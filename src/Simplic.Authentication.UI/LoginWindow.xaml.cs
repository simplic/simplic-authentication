using Simplic.Base;
using Simplic.Configuration;
using Simplic.Framework.Base;
using Simplic.Framework.DAL;
using Simplic.Localization;
using Simplic.Log;
using Simplic.Session;
using Simplic.TenantSystem;
using Simplic.WebApi.Client;
using Simplic.WebApi.Studio.Client;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Telerik.Windows.Automation.Peers;
using Telerik.Windows.Controls;

namespace Simplic.Authentication.UI
{
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        private readonly IAuthenticationService authenticationService;
        private readonly ILocalizationService localizationService;
        private readonly ISessionService sessionService;
        private readonly IOrganizationService organizationService;

        private readonly IClient client;
        private readonly IConnectionConfigurationService connectionConfigurationService;

        /// <summary>
        /// Initializew login window
        /// </summary>
        public LoginWindow()
        {
            if (!(StyleManager.ApplicationTheme is Telerik.Windows.Controls.VisualStudio2019Theme))
            {
                StyleManager.ApplicationTheme = new VisualStudio2019Theme();
            }

            AutomationManager.AutomationMode = AutomationMode.Disabled;

            InitializeComponent();

            authenticationService = CommonServiceLocator.ServiceLocator.Current.GetInstance<IAuthenticationService>();
            localizationService = CommonServiceLocator.ServiceLocator.Current.GetInstance<ILocalizationService>();
            sessionService = CommonServiceLocator.ServiceLocator.Current.GetInstance<ISessionService>();
            organizationService = CommonServiceLocator.ServiceLocator.Current.GetInstance<IOrganizationService>();
            client = CommonServiceLocator.ServiceLocator.Current.GetInstance<IClient>();
            connectionConfigurationService = CommonServiceLocator.ServiceLocator.Current.GetInstance<IConnectionConfigurationService>();

            DataContext = new LoginViewModel();

            this.Loaded += (s, e) =>
            {
                passwordBox.Focus(); Keyboard.Focus(passwordBox);

                try
                {
                    // Set default values
                    if (GlobalSettings.Arguments.ContainsKey("username"))
                        ViewModel.UserName = GlobalSettings.Arguments["username"];

                    if (GlobalSettings.Arguments.ContainsKey("password"))
                    {
                        passwordBox.Password = GlobalSettings.Arguments["password"];

                        // Remove password from ram
                        GlobalSettings.Arguments["password"] = "";
                    }

                    if (GlobalSettings.Arguments.ContainsKey("passthrough"))
                        Login();

                    // Try autologin
                    var session = authenticationService.TryAutologin();
                    if (session != null)
                        Login(session);
                }
                catch
                {
                    /* swallow */
                }
            };

            PreviewKeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    Login();
                    e.Handled = true;
                }
                if (e.Key == Key.Escape)
                {
                    DialogResult = false;
                    this.Close();
                    e.Handled = true;
                }
            };

#if DEBUG            
            passwordBox.Password = "capreolus";
#endif
        }

        /// <summary>
        /// Execute login process
        /// </summary>
        private void Login(Simplic.Session.Session autologinSession = null)
        {
            try
            {
                GlobalSettings.CurrentUserSession = autologinSession ?? authenticationService.Login(ViewModel.Domain, ViewModel.UserName, passwordBox.Password);

                // Workaround to be backword compatible
                var dbUserName = "";

                if (GlobalSettings.CurrentUserSession.IsADUser)
                    dbUserName = $"su.{ViewModel.Domain}.{ViewModel.UserName}";
                else
                    dbUserName = $"su.{ViewModel.UserName}";

                var pwd = "integratedpw";

                // Connection-String setzten
                GlobalSettings.UserName = GlobalSettings.CurrentUserSession.UserName;
                GlobalSettings.UserId = GlobalSettings.CurrentUserSession.UserId;
                GlobalSettings.SetUserConnectionString(dbUserName, pwd);
                DALManager.Init(GlobalSettings.ConnectionString);

                GlobalSettings.UserIsLoggedIn = true;
                LogManagerInstance.Instance.SetMessageConstant("UserName", GlobalSettings.UserName);
                LogManagerInstance.Instance.SetMessageConstant("UserId", GlobalSettings.UserId.ToString());
                LogManagerInstance.Instance.Info(string.Format("User logged in successful: {0}", ViewModel.UserName));

                UserManager.Singleton.RefreshSubstitution(GlobalSettings.CurrentUserSession.UserId);

                // Set current user session
                sessionService.CurrentSession = GlobalSettings.CurrentUserSession;

                if (ViewModel.RememberMe)
                    authenticationService.SetAutologin(ViewModel.Domain, ViewModel.UserName, passwordBox.Password);

                if (organizationService.Mode == OrganizationMode.Strict)
                {
                    GlobalSettings.CurrentUserSession.Organizations = new List<Organization>
                    {
                        organizationService.GetByUserId(GlobalSettings.UserId).OrderBy(x => x.Name).FirstOrDefault()
                    };
                }
                else
                {
                    GlobalSettings.CurrentUserSession.Organizations = new List<Organization>(organizationService.GetByUserId(GlobalSettings.UserId).OrderBy(x => x.Name));
                }

                // Try to login to web-api
                try
                {
                    var connection = connectionConfigurationService.GetByName("SimplicWebApi");

                    if (connection != null)
                    {
                        client.Url = connection.ConnectionString;

                        var authClient = CommonServiceLocator.ServiceLocator.Current.GetInstance<IAuthClient>();

                        var userName = ViewModel.UserName;
                        var userPwd = passwordBox.Password;

                        var result = Task.Run(async () =>
                        {
                            try
                            {
                                client.User = await authClient.LoginAsync(userName, userPwd);
                            }
                            catch (System.Exception ex)
                            {
                                Log.LogManagerInstance.Instance.Warning($"Failed to login user: {ViewModel.UserName}", ex);
                            }
                        });
                    }
                    else
                    {
                        Log.LogManagerInstance.Instance.Warning($"Could not find SimplicWebApi-connection string. {ViewModel.UserName}");
                    }
                }
                catch (System.Exception ex)
                {
                    Log.LogManagerInstance.Instance.Warning($"Error when trying to login into web-api. {ViewModel.UserName}", ex);
                }

                // If login failed, an exception will be thrown
                DialogResult = true;
                this.Close();
            }
            catch (LoginFailedException ex)
            {
                MessageBox.Show(localizationService.Translate($"login_{ex.Type.ToString().ToLower()}"), localizationService.Translate($"login_{ex.Type.ToString().ToLower()}_title"), MessageBoxButton.OK, MessageBoxImage.Warning);
                Log.LogManagerInstance.Instance.Error("Login failed (Exception)", ex);
            }
        }

        /// <summary>
        /// Check login
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LoginClickEventHandler(object sender, RoutedEventArgs e)
        {
            Login();
        }

        /// <summary>
        /// Cancel login
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CancelClickEventHandler(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// Get the current DataContext as <see cref="LoginViewModel"/>
        /// </summary>
        public LoginViewModel ViewModel
        {
            get { return DataContext as LoginViewModel; }
        }
    }
}
