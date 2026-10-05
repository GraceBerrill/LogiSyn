using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using AndersonsBakeryAPI.Services;
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
			// Add logging for WPF app views and services
			services.AddLogging();
			// Register ApiClient as a singleton for the WPF client to use.
			// Uses the cloud API URL first; falls back to local API automatically via _baseUrls in ApiClient.
			services.AddSingleton<ApiClient>(sp => new ApiClient("https://andersons-bakery-api.onrender.com"));

			// Register common backend services so views can resolve them from DI
			services.AddSingleton<AndersonsBakeryAPI.Services.ProductService>();
			services.AddSingleton<AndersonsBakeryAPI.Services.UserServiceRouter>();
			services.AddSingleton<AndersonsBakeryAPI.Services.LoginServiceRouter>();
			services.AddSingleton<AndersonsBakeryAPI.Services.OrderService>();

			ServiceProvider = services.BuildServiceProvider();

			// Set ShutdownMode BEFORE base.OnStartup to prevent premature shutdown if a window closes during startup
			ShutdownMode = ShutdownMode.OnExplicitShutdown;
			base.OnStartup(e);

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