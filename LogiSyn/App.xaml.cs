using System;
using System.Windows;
using System.Windows.Threading;
using LogiSyn.Model;
using LogiSyn.Views;

namespace LogiSyn
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var login = new LogiSyn.Views.LoginWindow();
            bool? result = login.ShowDialog();

            if (result == true && login.LoggedInUser != null)
            {
                var user = login.LoggedInUser;

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    // Map the user role string to the AppRole enum (Admin, Manager, User)
                    if (!Enum.TryParse(user.Role, true, out AppRole role))
                    {
                        role = AppRole.User;
                    }

                    var shell = new ShellWindow(role);
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