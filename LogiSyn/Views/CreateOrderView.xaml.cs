using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using LogiSyn.ViewModels;

namespace LogiSyn.Views
{
    // Interaction logic for CreateOrderView.xaml.
    // Order parsing and data flow are managed by CreateOrderViewModel.
    public partial class CreateOrderView : UserControl
    {
        public CreateOrderViewModel ViewModel { get; }

        public CreateOrderView()
        {
            InitializeComponent();

            ViewModel = new CreateOrderViewModel();
            DataContext = ViewModel;

            DateText.Text = ViewModel.DateText;

            ViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(CreateOrderViewModel.FileName))
                {
                    UploadText.Text = ViewModel.FileName;
                }
                if (e.PropertyName == nameof(CreateOrderViewModel.IsProcessing))
                {
                    LoadingOverlay.Visibility = ViewModel.IsProcessing ? Visibility.Visible : Visibility.Collapsed;
                    GoButton.IsEnabled = !ViewModel.IsProcessing;
                    BrowseButton.IsEnabled = !ViewModel.IsProcessing;
                    DropZone.AllowDrop = !ViewModel.IsProcessing;
                }
            };
        }

        private void SetFile(string path)
        {
            ViewModel.SetSelectedFile(path);
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Choose the order file",
                Filter = "Order files (*.xlsx;*.xls;*.csv;*.pdf)|*.xlsx;*.xls;*.csv;*.pdf|All files (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                SetFile(dialog.FileName);
            }
        }

        private void DropZone_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
            {
                SetFile(files[0]);
            }
        }

        private async void GoButton_Click(object sender, RoutedEventArgs e)
        {
            var order = await ViewModel.ParseSelectedFileAsync();

            if (!string.IsNullOrEmpty(ViewModel.ErrorMessage))
            {
                MessageBox.Show(ViewModel.ErrorMessage, "Create New Order",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (order != null)
            {
                ShellWindow.Current?.Navigate("ordersheets", order);
            }
        }
    }
}
