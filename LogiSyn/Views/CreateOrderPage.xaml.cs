using SharedLibrary.Interface;
using AndersonsBakeryAPI.Services;

using Microsoft.Win32;
using System.IO;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LogiSyn.Views
{
    /// <summary>
    /// Interaction logic for AddOrder.xaml
    /// </summary>
    public partial class CreateOrderPage : Page
    {
        private readonly IOrderService _orderService;
        private string? _selectedFilePath;

        public CreateOrderPage()
        {
            InitializeComponent();
            _orderService = new OrderService();
            TxtDate.Text = DateTime.Now.ToString("dd/MM/yyyy");
        }

        private void Border_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.None;
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            e.Handled = true;
        }

        private void Border_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop) ?? Array.Empty<string>();
                if (files.Length > 0)
                {
                    _selectedFilePath = files[0];
                    TxtFileName.Text = Path.GetFileName(_selectedFilePath);
                }
            }
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "PDF Files|*.pdf|All files|*.*",
                Multiselect = false
            };

            if (dlg.ShowDialog() == true)
            {
                _selectedFilePath = dlg.FileName;
                TxtFileName.Text = Path.GetFileName(_selectedFilePath);
            }
        }

        private void BtnGo_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_selectedFilePath) || !File.Exists(_selectedFilePath))
            {
                MessageBox.Show("Please select an order PDF file first.", "No File Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // 1. Parse and scale from PDF
                var order = _orderService.ReadAndScaleOrder(_selectedFilePath!);

                // 2. Navigate straight to the Review page (do not save to history/list yet)
                NavigationService?.Navigate(new OrderReviewPage(order));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to parse order: {ex.Message}", "Processing Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
