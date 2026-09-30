using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LogiSyn.Views
{
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            InitializeComponent();
        }

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

        private void LoadDashboardData()
        {
            var orders = GetDashboardOrders();

            NewOrdersValue.Text = orders.Count(o => o.Status == "Pending").ToString();
            CompletedValue.Text = orders.Count(o => o.Status == "Complete" || o.Status == "Completed").ToString();

            RecentList.ItemsSource = orders;
        }

        private List<DashboardOrderItem> GetDashboardOrders()
        {
            return new List<DashboardOrderItem>
            {
                new DashboardOrderItem { Number = "#001", Customer = "Checkers", Status = "Pending", DashStatusBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C29D70")) },
                new DashboardOrderItem { Number = "#002", Customer = "Spar", Status = "Complete", DashStatusBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8DBE98")) }
            };
        }

        private void ExcelButton_Click(object sender, RoutedEventArgs e) { }
        private void EmailButton_Click(object sender, RoutedEventArgs e) { }
    }

    public class DashboardOrderItem
    {
        public string Number { get; set; } = string.Empty;
        public string Customer { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public Brush DashStatusBrush { get; set; } = Brushes.Gray;
    }
}