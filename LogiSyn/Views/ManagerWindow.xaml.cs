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
        public ManagerWindow(LogiSyn.Model.UserRow user)
        {
            InitializeComponent();

            // navigate to dashboard by default
            if (!System.Enum.TryParse<LogiSyn.Model.AppRole>(user.Role, true, out var role))
                role = LogiSyn.Model.AppRole.User;

            MainFrame.Navigate(new DashboardView(role));
        }

        // Adriaan - Navigate to Orders Page
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new OrdersPage());
        }

        private void NavDashboard_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new DashboardView(LogiSyn.Model.AppRole.Manager));
        }

        // Adriaan - Navigate to Order History
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

        // Adriaan - Navigate to Users Order Page
        private void UserOrderTemp_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new UsersOrderPage());
        }
    }
}