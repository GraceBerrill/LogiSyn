using System;
using System.Windows;
using SharedLibrary.Model;
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

		private void ShowLogin()
		{
			var login = new LoginWindow();
			bool? result = login.ShowDialog();

			if (result != true || login.LoggedInUser == null)
			{
				Shutdown();
				return;
			}

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
}