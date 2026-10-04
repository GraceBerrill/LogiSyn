// Adriaan
using System.Windows;
using System.Windows.Controls;

namespace LogiSyn.Views
{
    public partial class OrderCompletedModal : UserControl
    {
        public OrderCompletedModal()
        {
            InitializeComponent();
        }

        private void DoneButton_Click(object sender, RoutedEventArgs e)
        {
            // back to the user's order list (this also closes the pop-up)
            ShellWindow.Current?.Navigate("orders");
        }
    }
}