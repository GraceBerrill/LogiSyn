using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using AndersonsBakeryAPI.Services;
using System;
using SharedLibrary.Model;
using LogiSyn.Views;

namespace LogiSyn
{
	public partial class App : Application
	{
		public static IServiceProvider ServiceProvider { get; private set; } = null!;

		protected override void OnStartup(StartupEventArgs e)
		{
			var services = new ServiceCollection();
			// Register ApiClient as a singleton for the WPF client to use
			services.AddSingleton<ApiClient>();

			// Register common backend services so views can resolve them from DI
			services.AddSingleton<AndersonsBakeryAPI.Services.ProductService>();
			services.AddSingleton<AndersonsBakeryAPI.Services.UserServiceRouter>();
			services.AddSingleton<AndersonsBakeryAPI.Services.LoginServiceRouter>();
			services.AddSingleton<AndersonsBakeryAPI.Services.OrderService>();

			ServiceProvider = services.BuildServiceProvider();

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