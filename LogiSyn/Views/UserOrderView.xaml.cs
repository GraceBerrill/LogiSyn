using System.Windows;
using System.Windows.Controls;
using SharedLibrary.Model;

namespace LogiSyn.Views
{
    public partial class UserOrdersView : UserControl
    {
        public UserOrdersView()
        {
            InitializeComponent();

            DateText.Text = SampleData.Today();

            // TODO (backend): load this user's orders
            OrderList.ItemsSource = SampleData.UserOrders();
        }

        private void OrderRow_Click(object sender, RoutedEventArgs e)
        {
            var order = ((FrameworkElement)sender).DataContext as OrderRow;
            ShellWindow.Current.Navigate("usersheet", order);
        }
    }
}