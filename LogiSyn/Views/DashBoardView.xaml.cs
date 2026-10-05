using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AndersonsBakeryAPI.Services;
using SharedLibrary.Model;

namespace LogiSyn.Views
{
	public partial class DashboardView : UserControl
	{
		private readonly ApiClient _apiClient = new ApiClient();
		private readonly OrderService _orderService = new OrderService();
		private List<OrderScaled> _currentOrdersScaled = new();
		private List<DashboardOrderItem> _currentDashboardOrders = new();

		public DashboardView(SharedLibrary.Model.AppRole role)
		{
			InitializeComponent();
			ConfigureRole(role.ToString());
		}

		/********************************************************************************************/
		// this makes it so that the dashboard can be configured based on the role of the user
		public void ConfigureRole(string role)
		{
			DateText.Text = DateTime.Now.ToString("dd/MM/yyyy");

			switch (role?.Trim().ToLower())
			{
				case "admin":
					ExportButtons.Visibility = Visibility.Visible;
					NewOrdersLabel.Text = "New Orders:";
					break;

				case "manager":
					ExportButtons.Visibility = Visibility.Collapsed;
					NewOrdersLabel.Text = "On Going Orders:";
					break;

				case "user":
				default:
					ExportButtons.Visibility = Visibility.Collapsed;
					NewOrdersLabel.Text = "New Orders:";
					break;
			}

			LoadDashboardData();
		}

		// Adriaan - Dashboard Orders Section
		private async void LoadDashboardData()
		{
			_currentOrdersScaled = await GetOrdersScaledAsync();

			NewOrdersValue.Text = _currentOrdersScaled.Count(o =>
				string.Equals(o.Status, "Pending", StringComparison.OrdinalIgnoreCase) ||
				string.Equals(o.Status, "In Production", StringComparison.OrdinalIgnoreCase)).ToString();

			CompletedValue.Text = _currentOrdersScaled.Count(o =>
				string.Equals(o.Status, "Complete", StringComparison.OrdinalIgnoreCase) ||
				string.Equals(o.Status, "Completed", StringComparison.OrdinalIgnoreCase)).ToString();

			_currentDashboardOrders = _currentOrdersScaled.Select(o =>
			{
				bool isComp = string.Equals(o.Status, "Complete", StringComparison.OrdinalIgnoreCase) ||
				              string.Equals(o.Status, "Completed", StringComparison.OrdinalIgnoreCase);

				return new DashboardOrderItem
				{
					Number = o.OrderId,
					Customer = o.Customer,
					Status = o.Status,
					DashStatusBrush = isComp
						? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8DBE98"))
						: new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C29D70"))
				};
			}).ToList();

			RecentList.ItemsSource = _currentDashboardOrders;
		}

		private async Task<List<OrderScaled>> GetOrdersScaledAsync()
		{
			List<OrderScaled> sourceOrders = new();
			try
			{
				sourceOrders = await _apiClient.GetOrdersAsync();
			}
			catch { }

			if (sourceOrders == null || sourceOrders.Count == 0)
			{
				try
				{
					sourceOrders = (await _orderService.GetOrdersAsync()).ToList();
				}
				catch { }
			}

			if (sourceOrders == null || sourceOrders.Count == 0)
			{
				try
				{
					sourceOrders = _orderService.GetOrders().ToList();
				}
				catch { }
			}

			if (sourceOrders != null && sourceOrders.Count > 0)
			{
				return sourceOrders;
			}

			return SampleData.SampleOrdersScaled();
		}

		// Opens the Excel export modal allowing the user to select one or multiple orders with complete details
		private void ExcelButton_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				var orders = _currentOrdersScaled.Count > 0 ? _currentOrdersScaled : SampleData.SampleOrdersScaled();
				var modal = new ExportOrdersModal(orders);

				if (ShellWindow.Current != null)
				{
					ShellWindow.Current.ShowModal(modal, (Brush)FindResource("ScrimDetail"));
				}
				else
				{
					var win = new Window
					{
						Content = modal,
						SizeToContent = SizeToContent.WidthAndHeight,
						WindowStartupLocation = WindowStartupLocation.CenterScreen,
						ResizeMode = ResizeMode.NoResize,
						Title = "Export Orders to Excel"
					};
					win.ShowDialog();
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Failed to open Excel export: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		// Opens the Email modal allowing the user to select one or multiple orders to email with full details and attachments
		private void EmailButton_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				var orders = _currentOrdersScaled.Count > 0 ? _currentOrdersScaled : SampleData.SampleOrdersScaled();
				var modal = new EmailOrdersModal(orders);

				if (ShellWindow.Current != null)
				{
					ShellWindow.Current.ShowModal(modal, (Brush)FindResource("ScrimDetail"));
				}
				else
				{
					var win = new Window
					{
						Content = modal,
						SizeToContent = SizeToContent.WidthAndHeight,
						WindowStartupLocation = WindowStartupLocation.CenterScreen,
						ResizeMode = ResizeMode.NoResize,
						Title = "Email Orders"
					};
					win.ShowDialog();
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Failed to open Email modal: {ex.Message}", "Email Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		// Clicking a recent order navigates to its detailed breakdown
		private void RecentOrderRow_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			if (sender is FrameworkElement elem && elem.DataContext is DashboardOrderItem item)
			{
				var orderScaled = _currentOrdersScaled.FirstOrDefault(o =>
					o.OrderId.Equals(item.Number, StringComparison.OrdinalIgnoreCase));

				var row = orderScaled != null
					? OrderRow.FromOrderScaled(orderScaled)
					: new OrderRow
					{
						Number = item.Number,
						Customer = item.Customer,
						Status = item.Status,
						Date = DateTime.Now
					};

				ShellWindow.Current?.Navigate("breakdown", row);
			}
		}
	}

	// Adriaan - Dashboard Order Item Model
	//order item class for the dashboard
	public class DashboardOrderItem
	{
		public string Number { get; set; } = string.Empty;
		public string Customer { get; set; } = string.Empty;
		public string Status { get; set; } = string.Empty;
		public Brush DashStatusBrush { get; set; } = Brushes.Gray;
	}
}
/*********************************************MAR26EOF***********************************************/