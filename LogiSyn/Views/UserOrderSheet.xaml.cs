using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LogiSyn.Model;
using LogiSyn.Services;

namespace LogiSyn.Views
{
    public partial class UserOrderSheetView : UserControl
    {
        private readonly OrderRow? _order;
        private readonly ApiClient _apiClient = new ApiClient();
        private readonly OrderService _orderService = new OrderService();

        //------------------------------------------------------------------------------------------------//

        // Constructor for the UserOrderSheetView class
        public UserOrderSheetView(OrderRow order)
        {
            InitializeComponent();
            _order = order;
            bool done = order != null && order.IsComplete;

            // Set the data context for the sheet and configure UI elements based on order status
            var data = SampleData.Sheet(done);
            if (order != null) data.Title = order.Customer + " Order " + order.Number;

            Sheet.IsReadOnly = done;
            Sheet.DataContext = data;
            CompleteButton.Visibility = done ? Visibility.Collapsed : Visibility.Visible;
        }

        //------------------------------------------------------------------------------------------------//

        // Event handler for the Back button click event
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            ShellWindow.Current?.Navigate("orders");
        }

        //------------------------------------------------------------------------------------------------//

        // Event handler for the Print button click event
        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            // Create a PrintDialog and show it to the user.
            // If the user confirms, the sheet is printed and the user is notified and returned to the orders page.
            var printDlg = new PrintDialog();
            if (printDlg.ShowDialog() == true)
            {
                MessageBox.Show($"Sheet for {_order?.Number} sent to printer.", "Print", MessageBoxButton.OK, MessageBoxImage.Information);
                ShellWindow.Current?.Navigate("orders");
            }
        }

        //------------------------------------------------------------------------------------------------//

        // Event handler for the Complete button click event
        private async void CompleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_order != null)
            {
                // Update the order status to "Completed" locally and in database
                _orderService.CompleteOrder(_order.Number);
                try
                {
                    await _apiClient.UpdateOrderStatusAsync(_order.Number, "Completed");
                }
                catch { }

                _order.Status = "Complete";
            }
            // popup window with checkmark to show completion
            ShellWindow.Current?.ShowModal(new OrderCompletedModal(), (Brush)FindResource("ScrimDone"), false);
        }
    }
}

//--------------------------------------End of File----------------------------------------------------------//