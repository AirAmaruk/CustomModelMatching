using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Xml.Linq;
using Newtonsoft.Json;

namespace CustomModelMatching
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string selectedFolderPath;
        private ObservableCollection<Aircraft> aircraftList;

        public MainWindow()
        {
            InitializeComponent();
            aircraftList = new ObservableCollection<Aircraft>();
            aircraftDataGrid.ItemsSource = aircraftList;
        }

        private void SaveState()
        {
            var state = new ApplicationState
            {
                SelectedFolderPath = selectedFolderPath,
                AircraftList = aircraftList.ToList(),
                IsEditedList = aircraftList.Select(a => a.IsEdited).ToList() // Save the IsEdited state
            };
            var json = JsonConvert.SerializeObject(state);
            File.WriteAllText("state.json", json);
        }

        private void LoadState()
        {
            if (File.Exists("state.json"))
            {
                var json = File.ReadAllText("state.json");
                var loadedState = JsonConvert.DeserializeObject<ApplicationState>(json);
                selectedFolderPath = loadedState.SelectedFolderPath;

                aircraftList.Clear(); // clear the current list

                // Initialize IsEditedList with false values if it's null
                if (loadedState.IsEditedList == null)
                {
                    loadedState.IsEditedList = new List<bool>(new bool[loadedState.AircraftList.Count]);
                }

                for (int i = 0; i < loadedState.AircraftList.Count; i++)
                {
                    var aircraft = loadedState.AircraftList[i];
                    aircraft.IsEdited = loadedState.IsEditedList[i]; // Load the IsEdited state
                    aircraftList.Add(aircraft);
                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveState();
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            using (var folderBrowserDialog = new FolderBrowserDialog())
            {
                if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    selectedFolderPath = folderBrowserDialog.SelectedPath;
                    folderPathTextBox.Text = selectedFolderPath;
                }
            }
        }

        private void SelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = aircraftDataGrid.SelectedItems.Cast<Aircraft>().ToList();

            if (selectedItems.All(i => i.IsSelected))
            {
                foreach (var item in selectedItems)
                {
                    item.IsSelected = false;
                }
            }
            else
            {
                foreach (var item in selectedItems)
                {
                    item.IsSelected = true;
                }
            }
        }


        private void FolderPathTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            selectedFolderPath = folderPathTextBox.Text;
        }

        private void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(selectedFolderPath))
            {
                System.Windows.MessageBox.Show("Please select a folder first.");
                return;
            }

            // Load the state before scanning
            LoadState();

            var scannedAircraftList = new ObservableCollection<Aircraft>();

            var excludedFolderName = "fsltl-traffic-base";

            foreach (var filePath in Directory.EnumerateFiles(selectedFolderPath, "aircraft.cfg", SearchOption.AllDirectories))
            {
                if (filePath.Contains("\\" + excludedFolderName + "\\")) // Checks if the current file is in the excluded folder
                {
                    continue; // Skip the current file and move to the next one
                }

                try
                {
                    var aircrafts = ExtractAircraftData(filePath);
                    foreach (var aircraft in aircrafts)
                    {
                        scannedAircraftList.Add(aircraft);
                    }
                }
                catch (Exception ex)
                {
                    // Log the error
                }
            }

            // Merge the scanned data with the loaded state
            foreach (var scannedAircraft in scannedAircraftList)
            {
                var existingAircraft = aircraftList.FirstOrDefault(a => a.Name == scannedAircraft.Name);

                if (existingAircraft != null)
                {
                    // Only update the properties if the aircraft hasn't been edited
                    if (!existingAircraft.IsEdited)
                    {
                        existingAircraft.IcaoAirline = scannedAircraft.IcaoAirline;
                        existingAircraft.TypeDesignator = scannedAircraft.TypeDesignator;
                    }
                }
                else
                {
                    // If the aircraft doesn't exist in the loaded state, add it
                    aircraftList.Add(scannedAircraft);
                }
            }

            SaveState();
            scanProgressBar.Value = 0;
        }

        private List<Aircraft> ExtractAircraftData(string filePath)
        {
            var aircraftList = new List<Aircraft>();
            var lines = File.ReadAllLines(filePath);
            Aircraft aircraft = null;
            string typeDesignator = null;

            foreach (var line in lines)
            {
                if (line.StartsWith("[fltsim", StringComparison.OrdinalIgnoreCase))
                {
                    if (aircraft != null)
                    {
                        // Add the previous aircraft to the list
                        aircraft.TypeDesignator = typeDesignator;
                        aircraftList.Add(aircraft);
                    }

                    // Start a new aircraft
                    aircraft = new Aircraft();
                    Debug.WriteLine($"Started new aircraft: {line}");
                }
                else if (Regex.IsMatch(line, @"^\s*title\s*=", RegexOptions.IgnoreCase))
                {
                    if (aircraft != null)
                    {
                        aircraft.Name = ExtractValue(line);

                        Debug.WriteLine($"Extracted title: {aircraft.Name}");
                    }
                }
                else if (Regex.IsMatch(line, @"^\s*icao_airline\s*=", RegexOptions.IgnoreCase))
                {
                    if (aircraft != null)
                    {
                        aircraft.IcaoAirline = ExtractValue(line);

                        Debug.WriteLine($"Extracted ICAO airline: {aircraft.IcaoAirline}");
                    }
                }
                else if (Regex.IsMatch(line, @"^\s*icao_type_designator\s*=", RegexOptions.IgnoreCase))
                {
                    typeDesignator = ExtractValue(line);

                    Debug.WriteLine($"Extracted ICAO type designator: {typeDesignator}");
                }
            }

            // Add the last aircraft to the list
            if (aircraft != null)
            {
                aircraft.TypeDesignator = typeDesignator;
                aircraftList.Add(aircraft);
            }

            return aircraftList;
        }


        private string ExtractValue(string line)
        {
            var equalsIndex = line.IndexOf('=');
            if (equalsIndex == -1)
            {
                return null;
            }

            var value = line.Substring(equalsIndex + 1).Trim();

            if (value.StartsWith("\""))
            {
                var closingQuoteIndex = value.IndexOf("\"", 1); // Look for the closing quote starting from the second character
                if (closingQuoteIndex != -1)
                {
                    // If a closing quote is found, take everything up to this quote
                    value = value.Substring(1, closingQuoteIndex - 1);
                }
                else
                {
                    // If no closing quote is found, remove the opening quote
                    value = value.Substring(1);
                }
            }

            return value.Trim();
        }

        private void CreateVMRButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(selectedFolderPath))
            {
                System.Windows.MessageBox.Show("Please select a folder first.");
                return;
            }

            var xml = new XDocument(new XDeclaration("1.0", "utf-8", null));
            var root = new XElement("ModelMatchRuleSet");

            foreach (var aircraft in aircraftList)
            {
                if (aircraft.IsSelected) // Assuming you have a property IsChecked in your Aircraft class
                {
                    var callsignPrefix = !string.IsNullOrEmpty(aircraft.IcaoAirline) ? aircraft.IcaoAirline.ToUpper() : "ZZZZ";
                    var typeCode = !string.IsNullOrEmpty(aircraft.TypeDesignator) ? aircraft.TypeDesignator.ToUpper() : "ZZZZ";

                    var rule = new XElement("ModelMatchRule",
                        new XAttribute("CallsignPrefix", callsignPrefix),
                        new XAttribute("TypeCode", typeCode),
                        new XAttribute("ModelName", aircraft.Name));

                    root.Add(rule);
                }
            }

            xml.Add(root);
            xml.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CustomModelMatching.xml"));


            System.Windows.MessageBox.Show("VMR file created successfully.");
            SaveState();
        }

        private void DataGrid_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Delete)
            {
                var grid = (System.Windows.Controls.DataGrid)sender;
                if (grid.SelectedItem is Aircraft aircraft)
                {
                    if (grid.CurrentCell.Column.Header.ToString() == "ICAO")
                    {
                        aircraft.IcaoAirline = string.Empty;
                        aircraft.IsEdited = true;
                    }
                    else if (grid.CurrentCell.Column.Header.ToString() == "Type of Aircraft")
                    {
                        aircraft.TypeDesignator = string.Empty;
                        aircraft.IsEdited = true;
                    }
                    e.Handled = true; // prevent the default Delete behavior
                    SaveState(); // Add this line to save the state after changes
                }
            }
        }


        private void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                var aircraft = e.Row.Item as Aircraft;
                if (aircraft != null)
                {
                    var textBox = e.EditingElement as System.Windows.Controls.TextBox;
                    if (textBox != null)
                    {
                        switch (e.Column.Header.ToString())
                        {
                            case "ICAO":
                                aircraft.IcaoAirline = textBox.Text;
                                break;
                            case "Type of Aircraft":
                                aircraft.TypeDesignator = textBox.Text;
                                break;
                        }
                    }

                    aircraft.IsEdited = true;
                    SaveState(); // Save the state whenever a cell is edited
                }
            }
        }
    }
}
