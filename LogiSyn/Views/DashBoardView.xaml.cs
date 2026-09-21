using System.Windows;
using System.Windows.Controls;
using LogiSyn.Model;

namespace LogiSyn.Views
{
    public partial class DashboardView : UserControl
    {
        public DashboardView(AppRole role)
        {
            InitializeComponent();

            DateText.Text = SampleData.Today();

            // Only the Admin has the Excel / Email buttons
            ExportButtons.Visibility = role == AppRole.Admin ? Visibility.Visible : Visibility.Collapsed;

            // The Manager's first card is worded differently in the design
            NewOrdersLabel.Text = role == AppRole.Manager ? "On Going Orders:" : "New Orders:";

            // TODO (backend): replace the sample numbers and rows with real data
            NewOrdersValue.Text = "12";
            CompletedValue.Text = "6";
            RecentList.ItemsSource = SampleData.Orders();
        }

        private void ExcelButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO (backend): export the orders to Excel
            MessageBox.Show("Excel export will be connected later.", "Excel");
        }

        private void EmailButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO (backend): email the orders
            MessageBox.Show("Email will be connected later.", "Email");
        }
    }
}