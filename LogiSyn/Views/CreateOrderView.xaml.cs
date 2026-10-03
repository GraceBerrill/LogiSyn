using System.IO;
using System.Windows;
using System.Windows.Controls;
using LogiSyn.Model;
using Microsoft.Win32;
using LogiSyn.Services;
using System.Diagnostics;

namespace LogiSyn.Views
{
    public partial class CreateOrderView : UserControl
    {
        private readonly OrderService _orderService = new OrderService();
        private readonly ApiClient _apiClient = new ApiClient();
        private string? _file;

        //------------------------------------------------------------------------------------------------//

        // Constructor for the CreateOrderView class
        public CreateOrderView()
        {
            InitializeComponent();
            DateText.Text = SampleData.Today();
        }

        //------------------------------------------------------------------------------------------------//

        // Method to set the file path and update the UI
        private void SetFile(string path)
        {
            _file = path;
            UploadText.Text = Path.GetFileName(path);
        }

        //------------------------------------------------------------------------------------------------//

        // Event handler for the Browse button click event
        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            // Limits the file types to Excel, CSV, and PDF files
            var dialog = new OpenFileDialog
            {
                Title = "Choose the order file",
                Filter = "Order files (*.xlsx;*.xls;*.csv;*.pdf)|*.xlsx;*.xls;*.csv;*.pdf|All files (*.*)|*.*"
            };
            // Show the file dialog and set the selected file if the user clicks OK
            if (dialog.ShowDialog() == true)
                SetFile(dialog.FileName);
        }

        //------------------------------------------------------------------------------------------------//

        // Drag and drop feature for the drop zone
        private void DropZone_Drop(object sender, DragEventArgs e)
        {
            // Check if the dropped data contains file paths
            var files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files != null && files.Length > 0)
                SetFile(files[0]);
        }

        //------------------------------------------------------------------------------------------------//

        // Event handler for the Go button click event
        private async void GoButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Check if a file has been selected; if not, show a message box and return
                if (string.IsNullOrWhiteSpace(_file))
                {
                    MessageBox.Show("Please choose an order file first.", "Create New Order",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Attempt to parse the order using the API client; if it fails, fall back to local parsing
                OrderScaled? order = null;
                try
                {
                    order = await _apiClient.ParsePdfOrderAsync(_file);
                }
                catch
                {
                    // TODO Add local parsing logic here
                }

                // If the order is still null after attempting to parse, read and scale the order using the OrderService
                if (order == null)
                {
                    order = _orderService.ReadAndScaleOrder(_file);
                }

                ShellWindow.Current?.Navigate("ordersheets", order);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"An error occurred while creating the order: {ex.Message}", "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

//--------------------------------------End of File----------------------------------------------------------//