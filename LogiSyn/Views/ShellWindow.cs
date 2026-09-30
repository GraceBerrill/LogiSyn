using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LogiSyn.Model;

namespace LogiSyn.Views
{
    // Lightweight controller that exposes navigation and modal helpers to views.
    public class ShellWindow
    {
        public static ShellWindow Current { get; set; }

        public AppRole Role { get; set; } = AppRole.Admin;

        private readonly Window _owner;
        private readonly ContentControl _host;
        private readonly Stack<Window> _modals = new Stack<Window>();

        public ShellWindow(Window owner)
        {
            _owner = owner;
            // expect MainWindow to contain a ContentControl named MainContent
            _host = (ContentControl)owner.FindName("MainContent");
        }

        // Navigate to a named page. Parameter is optional and passed to view if supported.
        public void Navigate(string page, object parameter = null)
        {
            UserControl view = null;
            switch (page)
            {
                case "ordersheets":
                    view = new OrderSheetsView();
                    break;
                case "createorder":
                    view = new CreateOrderView();
                    break;
                case "orders":
                    view = new AdminOrdersView();
                    break;
                case "breakdown":
                    view = new OrderBreakdownView(Role, parameter as OrderRow);
                    break;
                case "history":
                    view = new HistoryView(Role);
                    break;
                case "usersheet":
                    view = new UserOrderSheetView(parameter as OrderRow);
                    break;
                case "products":
                    view = new ManageProductsView();
                    break;
                case "users":
                    view = new ManageUsersView();
                    break;
                default:
                    var dv = new DashboardView();
                    dv.ConfigureRole(Role.ToString());
                    view = dv;
                    break;
            }

            // if view has a method to accept parameter, attempt to set DataContext
            if (parameter != null && view != null && view.DataContext == null)
                view.DataContext = parameter;

            if (_host != null)
                _host.Content = view;
        }

        // Shows a modal usercontrol inside a simple window using the provided scrim brush.
        public void ShowModal(UserControl modalContent, Brush scrim, bool closeOnBackgroundClick = true)
        {
            var w = new Window
            {
                Owner = _owner,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = scrim ?? Brushes.Transparent,
                ShowInTaskbar = false,
                SizeToContent = SizeToContent.WidthAndHeight,
                Content = modalContent,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            _modals.Push(w);
            w.ShowDialog();
        }

        public void CloseModal()
        {
            if (_modals.Count == 0) return;
            var w = _modals.Pop();
            if (w != null && w.IsVisible)
                w.Close();
        }
    }
}
