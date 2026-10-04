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
    /// Interaction logic for UserWindow.xaml
    /// </summary>
    public partial class UserWindow : Window
    {
        public UserWindow()
        {
            InitializeComponent();

            // navigate shell window to dashboard by default for a regular user
            _ = new LogiSyn.Views.ShellWindow(LogiSyn.Model.AppRole.User);
        }

        public void SetupSidebarNavigation(string role)
        {

            //contrlls visibility based on role
            BtnOrders.Visibility = (role == "Admin" || role == "User") ? Visibility.Visible : Visibility.Collapsed;
            BtnHistory.Visibility = (role == "Admin" || role == "Manager") ? Visibility.Visible : Visibility.Collapsed;
            BtnManageProducts.Visibility = (role == "Admin") ? Visibility.Visible : Visibility.Collapsed;
            BtnManageUsers.Visibility = (role == "Manager") ? Visibility.Visible : Visibility.Collapsed;
        }

        //Set the profile name in the sidebar
        public void SetProfileName(string username)
        {
            if (string.IsNullOrEmpty(username)) return;
            try
            {
                var field = this.FindName("ProfileNameText") as System.Windows.Controls.TextBlock;
                if (field != null)
                    field.Text = username;
            }
            catch { }
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

        //makes the logout button close the main window and show the login window
        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LogiSyn.Views.ShellWindow.Current?.CloseAllModals();
            }
            catch { }

            //show login window and close main window
            var login = new LogiSyn.Views.LoginWindow();
            login.Show();

            try
            {
                if (LogiSyn.Views.ShellWindow.Current != null)
                {
                    // clear hosted content
                    var hostField = typeof(LogiSyn.Views.ShellWindow).GetField("_host", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (hostField != null)
                    {
                        var host = hostField.GetValue(LogiSyn.Views.ShellWindow.Current) as ContentControl;
                        if (host != null) host.Content = null;
                    }
                }
            }
            catch { }
            this.Close();
        }
    }
}
/*********************************************MAR26EOF***********************************************/