// Adriaan
using System.Windows;
using System.Windows.Controls;

namespace LogiSyn.Views
{
    public partial class PrintEmailModal : UserControl
    {
        public PrintEmailModal()
        {
            InitializeComponent();
        }

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO (backend): print the order sheets
            ShellWindow.Current?.CloseModal();
            MessageBox.Show("Printing will be connected later.", "Print");
        }

        private void EmailButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO (backend): email the order sheets
            ShellWindow.Current?.CloseModal();
            MessageBox.Show("Email will be connected later.", "Email");
        }
    }
}