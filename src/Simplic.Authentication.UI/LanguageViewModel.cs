using System.Globalization;
using System.Windows.Media.Imaging;

namespace Simplic.Authentication.UI
{
    /// <summary>
    /// Language viewmodel
    /// </summary>
    public class LanguageViewModel : Simplic.UI.MVC.ViewModelBase
    {
        /// <summary>
        /// Gets or sets the language name
        /// </summary>
        public string Name
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the language image
        /// </summary>
        public BitmapImage Image
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the culture info
        /// </summary>
        public CultureInfo CultureInfo
        {
            get;
            set;
        }
    }
}
