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

            if (result == true)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    var main = new MainWindow();
                    Application.Current.MainWindow = main;
                    main.Show();
                }), DispatcherPriority.ApplicationIdle);
            }
            else
            {
                Shutdown();
            }
        }
    }
}