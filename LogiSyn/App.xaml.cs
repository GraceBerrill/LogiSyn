using System;
using System.Windows;
<<<<<<< HEAD
using SharedLibrary.Model;
=======
using System.Windows.Threading;
using LogiSyn.Model;
>>>>>>> Adriaan
using LogiSyn.Views;

namespace LogiSyn
{
	public partial class App : Application
	{
		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);
			ShutdownMode = ShutdownMode.OnExplicitShutdown;

			ShowLogin();
		}

<<<<<<< HEAD
		private void ShowLogin()
		{
			var login = new LoginWindow();
			bool? result = login.ShowDialog();
=======
            var login = new LogiSyn.Views.LoginWindow();
            bool? result = login.ShowDialog();
>>>>>>> Adriaan

			if (result != true || login.LoggedInUser == null)
			{
				Shutdown();
				return;
			}

<<<<<<< HEAD
			var user = login.LoggedInUser;

			AppRole parsedRole = AppRole.User;
			if (!string.IsNullOrEmpty(user.Role))
			{
				Enum.TryParse(user.Role, true, out parsedRole);
			}

			var shell = new ShellWindow(parsedRole);
			MainWindow = shell;
			shell.Show();
		}

		public void SignOut()
		{
			MainWindow = null;
			ShowLogin();
		}
	}
=======
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
>>>>>>> Adriaan
}