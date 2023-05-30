using System.ComponentModel;

namespace CustomModelMatching
{
    public class Aircraft : INotifyPropertyChanged
    {
        private string name;
        private string icaoAirline;
        private string typeDesignator;
        private bool isSelected;
        private bool isEdited;

        public string Name
        {
            get { return name; }
            set
            {
                if (value != name)
                {
                    name = value;
                    OnPropertyChanged("Name");
                }
            }
        }

        public string IcaoAirline
        {
            get { return icaoAirline; }
            set
            {
                if (value != icaoAirline)
                {
                    icaoAirline = value;
                    OnPropertyChanged("IcaoAirline");
                }
            }
        }

        public string TypeDesignator
        {
            get { return typeDesignator; }
            set
            {
                if (value != typeDesignator)
                {
                    typeDesignator = value;
                    OnPropertyChanged("TypeDesignator");
                }
            }
        }

        public bool IsSelected
        {
            get { return isSelected; }
            set
            {
                if (value != isSelected)
                {
                    isSelected = value;
                    OnPropertyChanged("IsSelected");
                }
            }
        }

        public bool IsEdited
        {
            get { return isEdited; }
            set
            {
                if (value != isEdited)
                {
                    isEdited = value;
                    OnPropertyChanged("IsEdited");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
