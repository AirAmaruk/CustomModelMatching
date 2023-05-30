using System.Collections.Generic;

namespace CustomModelMatching
{
    public class ApplicationState
    {
        public string SelectedFolderPath { get; set; }
        public List<Aircraft> AircraftList { get; set; }
        // Include a new property for IsEdited state
        public List<bool> IsEditedList { get; set; }
    }
}
