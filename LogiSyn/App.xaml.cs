using System;
using System.Windows;
using System.Windows.Threading;
using LogiSyn.Views;

namespace LogiSyn
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var login = new LoginWindow();
            bool? result = login.ShowDialog();

            if (result == true && login.LoggedInUser != null)
            {
                var user = login.LoggedInUser;

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    LogiSyn.Model.AppRole parsedRole = LogiSyn.Model.AppRole.User;
                    if (!string.IsNullOrEmpty(user.Role))
                    {
                        Enum.TryParse<LogiSyn.Model.AppRole>(user.Role, true, out parsedRole);
                    }

                    var shell = new Views.ShellWindow(parsedRole);
                    Application.Current.MainWindow = shell;
                    shell.Show();
                }), DispatcherPriority.ApplicationIdle);
            }
            else
            {
                Shutdown();
            }
        }
    }
}