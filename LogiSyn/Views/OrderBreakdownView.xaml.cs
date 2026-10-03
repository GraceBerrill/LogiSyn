using System.Windows;
using System.Windows.Controls;
using LogiSyn.Model;

namespace LogiSyn.Views
{
    public partial class OrderBreakdownView : UserControl
    {
        public OrderBreakdownView(AppRole role, OrderRow order)
        {
            InitializeComponent();

            // e.g. "Spar Order #002"
            string title = order == null ? "Spar Order #002" : order.Customer + " Order " + order.Number;

            // TODO (backend): load the real order breakdown for this order
            if (role == AppRole.Manager)
            {
                var data = SampleData.Sheet(true);
                data.Title = title;
                PaperHost.Content = new SheetPaper { IsReadOnly = true, DataContext = data };
            }
            else
            {
                var data = SampleData.Summary(true);
                data.Title = title;
                PaperHost.Content = new SummaryPaper { DataContext = data };
            }
        }

        private void DoneButton_Click(object sender, RoutedEventArgs e)
        {
            ShellWindow.Current?.Navigate("history");
        }
    }
}