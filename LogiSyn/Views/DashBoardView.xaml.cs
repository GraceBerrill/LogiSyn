using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AndersonsBakeryAPI.Services;
using SharedLibrary.Model;

namespace LogiSyn.Views
{
	public partial class DashboardView : UserControl
	{
        private readonly AppRole _role;
        private readonly OrderService _orderService = new OrderService();
        private List<DashboardOrderItem> _orders = new();

        public DashboardView(AppRole role)
		{
			InitializeComponent();
			_role = role;
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
        private void LoadDashboardData()
        {
            try
            {
                _orders = GetDashboardOrders();

                NewOrdersValue.Text = _orders
                    .Count(o => string.Equals(o.Status, "Pending", StringComparison.OrdinalIgnoreCase))
                    .ToString();

                CompletedValue.Text = _orders
                    .Count(o => string.Equals(o.Status, "Complete", StringComparison.OrdinalIgnoreCase)
                             || string.Equals(o.Status, "Completed", StringComparison.OrdinalIgnoreCase))
                    .ToString();

                RecentList.ItemsSource = _orders;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load dashboard data: {ex.Message}",
                    "Dashboard Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private List<DashboardOrderItem> GetDashboardOrders()
        {
            var orders = _orderService.GetOrders() ?? Enumerable.Empty<OrderScaled>();

            return orders
                .OrderByDescending(o => o.OrderDate)
                .Take(10)
                .Select(o => new DashboardOrderItem
                {
                    Number = o.OrderId,
                    Customer = o.Customer,
                    Status = o.Status
                })
                .ToList();
        }

        //this is to export the dashboard data to a csv file
        private void ExcelButton_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				var orders = GetDashboardOrders();
				var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
				var outDir = System.IO.Path.Combine(docs, "LogiSyn_exports");
				System.IO.Directory.CreateDirectory(outDir);
				var file = System.IO.Path.Combine(outDir, $"orders_{DateTime.Now:yyyyMMdd_HHmmss}.csv");

				var sb = new System.Text.StringBuilder();
				sb.AppendLine("OrderNumber,Customer,Status");
				foreach (var o in orders)
				{
					string number = EscapeCsv(o.Number);
					string customer = EscapeCsv(o.Customer);
					string status = EscapeCsv(o.Status);
					sb.AppendLine($"{number},{customer},{status}");
				}

				System.IO.File.WriteAllText(file, sb.ToString());

				MessageBox.Show($"Orders exported to:\r\n{file}", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Export failed: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		//placeholder for email functinality
		private void EmailButton_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				LogiSyn.Views.ShellWindow.Current?.ShowModal(new PrintEmailModal(), (Brush)FindResource("ScrimPrint"));
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Email action failed: {ex.Message}", "Email", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private static string EscapeCsv(string input)
		{
			if (input == null) return string.Empty;
			if (input.Contains(',') || input.Contains('"') || input.Contains('\n') || input.Contains('\r'))
			{
				return '"' + input.Replace("\"", "\"\"") + '"';
			}
			return input;
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