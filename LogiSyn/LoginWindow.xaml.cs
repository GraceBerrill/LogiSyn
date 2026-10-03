using System.Windows;
using LogiSyn.Services;
using SharedLibrary.Model;

namespace LogiSyn
{
    public partial class LoginWindow : Window
    {
        private readonly LoginService _loginService = new LoginService();

        public LoginWindow()
        {
            InitializeComponent();
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string name = txtUsername.Text.Trim();
            string password = txtPassword.Password;

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Please enter both username and password.");
                return;
            }

            try
            {
                UserRow? loggedInUser = _loginService.Authenticate(name, password);

                if (loggedInUser != null)
                {
                    this.DialogResult = true;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Invalid username or password.", "Login Failed");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Database Error");
            }
        }
    }
}