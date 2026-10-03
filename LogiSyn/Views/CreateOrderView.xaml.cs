using System.IO;
using System.Windows;
using System.Windows.Controls;
using SharedLibrary.Model;
using Microsoft.Win32;

namespace LogiSyn.Views
{
    public partial class CreateOrderView : UserControl
    {
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
            if (_file == null)
            {
                MessageBox.Show("Please choose an order file first.", "Create New Order",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // TODO (backend): read the file, create the order, then show the breakdown
            ShellWindow.Current.Navigate("ordersheets");
        }
    }
}