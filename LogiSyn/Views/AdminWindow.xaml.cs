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
    /// Interaction logic for AdminWindow.xaml
    /// </summary>
    public partial class AdminWindow : Window
    {
        public AdminWindow(LogiSyn.Model.UserRow user)
        {
            InitializeComponent();

            if (!System.Enum.TryParse<LogiSyn.Model.AppRole>(user.Role, true, out var role))
                role = LogiSyn.Model.AppRole.User;

            // show dashboard by default
            MainContent.Content = new DashboardView(role);
        }

        // Adriaan - Navigate to Orders View
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new AdminOrdersView();
        }
    }
}