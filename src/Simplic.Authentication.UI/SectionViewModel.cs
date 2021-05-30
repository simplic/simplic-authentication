using Simplic.UI.MVC;

namespace Simplic.Authentication.UI
{
    /// <summary>
    /// Section viewmodel
    /// </summary>
    public class SectionViewModel : ViewModelBase
    {
        /// <summary>
        /// Gets or sets the intern section name
        /// </summary>
        public string Section
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the section friendly name
        /// </summary>
        public string SectionFriendlyName
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the friendly name to be shown in the combobox
        /// </summary>
        public string DisplayName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(SectionFriendlyName))
                    return Section;
                else
                    return $"{SectionFriendlyName} [{Section}]";
            }
        }
    }
}
