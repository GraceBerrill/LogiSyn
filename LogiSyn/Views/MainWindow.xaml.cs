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

            MainFrame.Navigate(new OrdersPage());
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new OrdersPage());
        }

        private void NavDashboard_Click(object sender, RoutedEventArgs e)
        {
            // If you have a Dashboard page created:
            //MainFrame.Navigate(new DashboardPage());
        }

        private void NavHistory_Click(object sender, RoutedEventArgs e)
        {
            // MainFrame.Navigate(new HistoryPage());
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
    }
}