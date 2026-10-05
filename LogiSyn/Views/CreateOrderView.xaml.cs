// Adriaan
using System.IO;
using System.Windows;
using System.Windows.Controls;
using SharedLibrary.Model;
using Microsoft.Win32;
using AndersonsBakeryAPI.Services;
using System.Diagnostics;

namespace LogiSyn.Views
{
    public partial class CreateOrderView : UserControl
    {
        private readonly OrderService _orderService = new OrderService();
        private readonly ExcelOrderService _excelService = new ExcelOrderService();
        private readonly ApiClient _apiClient = new ApiClient();
        private string? _file;

        //------------------------------------------------------------------------------------------------//

        // Constructor for the CreateOrderView class
        public CreateOrderView()
        {
            InitializeComponent();
            DateText.Text = DateTime.Now.ToString("dd MMMM yyyy");
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
            // Check if a file has been selected; if not, show a message box and return
            if (string.IsNullOrWhiteSpace(_file))
            {
                MessageBox.Show("Please choose an order file first.", "Create New Order",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Display loading spinner overlay and disable interaction while parsing
            LoadingOverlay.Visibility = Visibility.Visible;
            GoButton.IsEnabled = false;
            BrowseButton.IsEnabled = false;
            DropZone.AllowDrop = false;

            try
            {
                OrderScaled? order = null;
                string ext = Path.GetExtension(_file).ToLowerInvariant();

                if (ext == ".xlsx" || ext == ".xls" || ext == ".csv")
                {
                    order = await Task.Run(() => _excelService.ReadOrderFromExcel(_file));
                }
                else if (ext == ".pdf")
                {
                    // Attempt to parse the order using the API client first
                    try
                    {
                        order = await _apiClient.ParsePdfOrderAsync(_file);
                    }
                    catch (Exception ex)
                    {
                        // API parsing offline or unreachable; log diagnostic and seamlessly fallback to local OrderService
                        Debug.WriteLine($"[CreateOrder] Cloud API PDF parse failed ({ex.Message}). Engaging local offline parser.");
                    }

                    // Fall back to local parsing on background thread so the UI spinner animates smoothly
                    if (order == null)
                    {
                        order = await Task.Run(() => _orderService.ReadAndScaleOrder(_file));
                    }
                }
                else
                {
                    MessageBox.Show("Unsupported file type. Please select an Excel (.xlsx, .xls, .csv) or PDF (.pdf) file.",
                                    "Invalid File", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (order != null)
                {
                    ShellWindow.Current?.Navigate("ordersheets", order);
                }
                else
                {
                    MessageBox.Show("Unable to parse the order file. Please verify that the selected file is a valid order file.",
                                    "Parse Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"An error occurred while creating the order: {ex.Message}", "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Reset loading overlay and controls
                LoadingOverlay.Visibility = Visibility.Collapsed;
                GoButton.IsEnabled = true;
                BrowseButton.IsEnabled = true;
                DropZone.AllowDrop = true;
            }
        }
    }
}

//--------------------------------------End of File----------------------------------------------------------//