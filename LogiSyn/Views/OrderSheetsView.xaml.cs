using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SharedLibrary.Model;

namespace LogiSyn.Views
{
    public partial class OrderSheetsView : UserControl
    {
        public OrderSheetsView()
        {
            InitializeComponent();

            // TODO (backend): load the real order
            SummaryDoc.DataContext = SampleData.Summary(false);
            RawDoc.DataContext = SampleData.RawMaterials();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            ShellWindow.Current.Navigate("orders");
        }

        private void AddProductButton_Click(object sender, RoutedEventArgs e)
        {
            ShellWindow.Current.ShowModal(new AddProductModal(), (Brush)FindResource("ScrimDetail"));
        }

        // DONE opens the Print / Email choice
        private void DoneButton_Click(object sender, RoutedEventArgs e)
        {
            ShellWindow.Current.ShowModal(new PrintEmailModal(), (Brush)FindResource("ScrimPrint"));
        }
    }
}