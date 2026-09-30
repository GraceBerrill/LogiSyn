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

namespace LogiSyn
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // register shell controller so views can navigate and show modals
            // ShellWindow is in LogiSyn.Views namespace
            LogiSyn.Views.ShellWindow.Current = new LogiSyn.Views.ShellWindow(this);
        }

        public void SetupSidebarNavigation(string role)
        {
            // Toggle sidebar visibility based on design mockups
            BtnOrders.Visibility = (role == "Admin" || role == "User") ? Visibility.Visible : Visibility.Collapsed;
            BtnHistory.Visibility = (role == "Admin" || role == "Manager") ? Visibility.Visible : Visibility.Collapsed;
            BtnManageProducts.Visibility = (role == "Admin") ? Visibility.Visible : Visibility.Collapsed;
            BtnManageUsers.Visibility = (role == "Manager") ? Visibility.Visible : Visibility.Collapsed;
        }

        private void BtnDashboard_Click(object sender, RoutedEventArgs e)
        {
            LogiSyn.Views.ShellWindow.Current?.Navigate("dashboard");
        }

        private void BtnOrders_Click(object sender, RoutedEventArgs e)
        {
            LogiSyn.Views.ShellWindow.Current?.Navigate("orders");
        }

        private void BtnHistory_Click(object sender, RoutedEventArgs e)
        {
            LogiSyn.Views.ShellWindow.Current?.Navigate("history");
        }

        private void BtnManageProducts_Click(object sender, RoutedEventArgs e)
        {
            LogiSyn.Views.ShellWindow.Current?.Navigate("products");
        }

        private void BtnManageUsers_Click(object sender, RoutedEventArgs e)
        {
            LogiSyn.Views.ShellWindow.Current?.Navigate("users");
        }
    }
}