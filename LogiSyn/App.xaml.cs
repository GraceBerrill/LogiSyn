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
                    Window dashboard = user.Role?.ToLowerInvariant() switch
                    {
                        "admin" => new AdminWindow(),
                        "manager" => new ManagerWindow(),
                        _ => new UserWindow(),
                    };

                    Application.Current.MainWindow = dashboard;
                    dashboard.Show();
                }), DispatcherPriority.ApplicationIdle);
            }
            else
            {
                Shutdown();
            }
        }
    }
}