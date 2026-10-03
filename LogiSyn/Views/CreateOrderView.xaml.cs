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

        private string _file;

        public CreateOrderView()
        {
            InitializeComponent();
            DateText.Text = SampleData.Today();
        }

        private void SetFile(string path)
        {
            _file = path;
            UploadText.Text = Path.GetFileName(path);
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Choose the order file",
                Filter = "Order files (*.xlsx;*.xls;*.csv;*.pdf)|*.xlsx;*.xls;*.csv;*.pdf|All files (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
                SetFile(dialog.FileName);
        }

        // drag a file onto the dashed box
        private void DropZone_Drop(object sender, DragEventArgs e)
        {
            var files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files != null && files.Length > 0)
                SetFile(files[0]);
        }

        private void GoButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_file == null)
                {
                    MessageBox.Show("Please choose an order file first.", "Create New Order",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var order = _orderService.ReadAndScaleOrder(_file);

                ShellWindow.Current.Navigate("ordersheets", order);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"An error occurred while creating the order: {ex.Message}", "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}