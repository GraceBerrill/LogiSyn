using LogiSyn.Views;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LogiSyn.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class ManagerWindow : Window
    {
        public ManagerWindow()
        {
            InitializeComponent();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new OrdersPage());
        }

        private void NavDashboard_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new MainWindow());
        }

        private void NavHistory_Click(object sender, RoutedEventArgs e)
        {
             MainFrame.Navigate(new AdminOrderHistory());
        }

        private void NavProducts_Click(object sender, RoutedEventArgs e)
        {
            // MainFrame.Navigate(new ManageProductsPage());
        }

        private void NavLogout_Click(object sender, RoutedEventArgs e)
        {
            var loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close();
        }

        private void UserOrderTemp_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new UsersOrderPage());
        }
    }
}