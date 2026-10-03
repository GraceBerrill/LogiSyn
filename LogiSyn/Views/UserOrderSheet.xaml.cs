using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SharedLibrary.Model;

namespace LogiSyn.Views
{
    public partial class UserOrderSheetView : UserControl
    {
        public UserOrderSheetView(OrderRow order)
        {
            InitializeComponent();

            // A finished order is shown filled-in and locked; a pending one can be typed into.
            bool done = order != null && order.IsComplete;

            // TODO (backend): load the real sheet for this order
            var data = SampleData.Sheet(done);
            if (order != null) data.Title = order.Customer + " Order " + order.Number;

            Sheet.IsReadOnly = done;
            Sheet.DataContext = data;
            CompleteButton.Visibility = done ? Visibility.Collapsed : Visibility.Visible;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            ShellWindow.Current.Navigate("orders");
        }

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO (backend): print the sheet
            MessageBox.Show("Printing will be connected later.", "Print");
        }

        private void CompleteButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO (backend): save the "Used" amounts and mark the order as completed
            ShellWindow.Current.ShowModal(new OrderCompletedModal(), (Brush)FindResource("ScrimDone"), false);
        }
    }
}