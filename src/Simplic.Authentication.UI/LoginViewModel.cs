using Simplic.Configuration.Ini;
using Simplic.Localization;
using Simplic.UI.MVC;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media.Imaging;

namespace Simplic.Authentication.UI
{
    /// <summary>
    /// Login viewmodel
    /// </summary>
    public class LoginViewModel
        // This window should not inherit from the ExtendableViewModel
        : ViewModelBase
    {
        private string domain;
        private string userName;
        private bool rememberMe;
        private ILocalizationService localizationService;
        private LanguageViewModel selectedLanguage;
        private ObservableCollection<LanguageViewModel> languages;
        private ObservableCollection<SectionViewModel> sections;
        private SectionViewModel selectedSection;

        /// <summary>
        /// Initializew view model
        /// </summary>
        public LoginViewModel()
        {
            Domain = Environment.UserDomainName;
            languages = new ObservableCollection<LanguageViewModel>();

            sections = new ObservableCollection<SectionViewModel>();

            // Read sections
            var iniFile = new IniFileSettings(Base.GlobalSettings.StudioPath + "\\Configuration\\Studio.ini");
            foreach (var section in iniFile.ReadSections())
            {
                if (iniFile.ReadValue<int>("IsHiddenSection", section, 0) == 1)
                    continue;

                sections.Add(new SectionViewModel
                {
                    Section = section,
                    SectionFriendlyName = iniFile.ReadValue<string>("SectionFriendlyName", section)
                });
            }

            // Use field here and call raise property changed manually, to avoid exit and reopen
            selectedSection = sections.FirstOrDefault(x => x.Section == Base.GlobalSettings.IniSection);
            RaisePropertyChanged(nameof(SelectedSection));

            localizationService = CommonServiceLocator.ServiceLocator.Current.GetInstance<ILocalizationService>();

            foreach (var language in localizationService.GetAvailableLanguages())
            {
                var languageVM = new LanguageViewModel();

                languageVM.Name = language.DisplayName;

                // We use embedded images, because we have no database connection yet
                var image = new BitmapImage();
                image.BeginInit();
                image.UriSource = new Uri($"pack://application:,,/Simplic.Authentication.UI;component/Resources/Images/flag_{language.Name}_32x.png");
                image.EndInit();

                languageVM.Image = image;
                languageVM.CultureInfo = language;

                languages.Add(languageVM);
            }

            selectedLanguage = languages.FirstOrDefault();

            userName = Environment.UserName;

#if DEBUG
            userName = "SuperUser";
#endif
        }

        /// <summary>
        /// Gets or sets the current domain
        /// </summary>
        public string Domain
        {
            get
            {
                return domain;
            }

            set
            {
                domain = value;
            }
        }

        /// <summary>
        /// Gets or sets the current username
        /// </summary>
        public string UserName
        {
            get
            {
                return userName;
            }

            set
            {
                userName = value;
                RaisePropertyChanged(nameof(UserName));
            }
        }

        /// <summary>
        /// Gets or sets whether to keep logged in
        /// </summary>
        public bool RememberMe
        {
            get
            {
                return rememberMe;
            }

            set
            {
                rememberMe = value;
            }
        }

        /// <summary>
        /// Gets or sets the selected language
        /// </summary>
        public LanguageViewModel SelectedLanguage
        {
            get
            {
                return selectedLanguage;
            }

            set
            {
                selectedLanguage = value;
                RaisePropertyChanged(nameof(SelectedLanguage));

                // Change language
                localizationService.ChangeLanguage(value.CultureInfo);

                RaisePropertyChanged(nameof(UserNameLabel));
                RaisePropertyChanged(nameof(PasswordLabel));
                RaisePropertyChanged(nameof(DomainLabel));
                RaisePropertyChanged(nameof(LanguageLabel));
                RaisePropertyChanged(nameof(RememberMeLabel));
                RaisePropertyChanged(nameof(LoginLabel));
                RaisePropertyChanged(nameof(CancelLabel));
                RaisePropertyChanged(nameof(DatabaseLabel));
            }
        }

        /// <summary>
        /// Gets or sets all selected languages
        /// </summary>
        public ObservableCollection<LanguageViewModel> Languages
        {
            get
            {
                return languages;
            }

            set
            {
                languages = value;
            }
        }

        public string UserNameLabel
        {
            get
            {
                return localizationService.Translate("user_name");
            }
        }

        public string PasswordLabel
        {
            get
            {
                return localizationService.Translate("password");
            }
        }
        public string DomainLabel
        {
            get
            {
                return localizationService.Translate("domain");
            }
        }
        public string LanguageLabel
        {
            get
            {
                return localizationService.Translate("language");
            }
        }
        public string RememberMeLabel
        {
            get
            {
                return localizationService.Translate("remember_me");
            }
        }
        public string LoginLabel
        {
            get
            {
                return localizationService.Translate("login");
            }
        }
        public string CancelLabel
        {
            get
            {
                return localizationService.Translate("cancel");
            }
        }

        /// <summary>
        /// Gets the database label content
        /// </summary>
        public string DatabaseLabel
        {
            get
            {
                return localizationService.Translate("database");
            }
        }

        /// <summary>
        /// Gets the current simplic login title including the version
        /// </summary>
        public string LoginFooter
        {
            get
            {
                var assembly = this.GetType().Assembly;

                // Try to find simplic studio version

                try
                {
                    assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(x => x.FullName.Contains("Simplic Studio")) ?? assembly;
                }
                catch
                {
                    /* swallow */
                }

                return $"Simplic Studio {DateTime.Now.Year} | {assembly.GetName().Version} | Copyright by SIMPLIC GmbH {DateTime.Now.Year}";
            }
        }

        /// <summary>
        /// Gets or sets a list of available sections
        /// </summary>
        public ObservableCollection<SectionViewModel> Sections
        {
            get
            {
                return sections;
            }

            set
            {
                sections = value;
            }
        }

        /// <summary>
        /// Gets or sets the selected section. If a new section is selected, simplic will be restarted.
        /// </summary>
        public SectionViewModel SelectedSection
        {
            get
            {
                return selectedSection;
            }

            set
            {
                selectedSection = value;
                System.Diagnostics.Process.Start("Simplic Studio.exe", $"--section {value.Section}");
                Environment.Exit(0);
            }
        }
    }
}
