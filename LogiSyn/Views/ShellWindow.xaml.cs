using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SharedLibrary.Model;

namespace LogiSyn.Views
{
    public partial class ShellWindow : Window
    {
        public static ShellWindow? Current { get; private set; }

        private readonly AppRole _role;

        public ShellWindow(AppRole role)
        {
            InitializeComponent();

            Current = this;
            _role = role;
            RoleText.Text = role.ToString();

            ConfigureNavigation();
            Navigate("dashboard");
        }

        private void ConfigureNavigation()
        {
            ProductsButton.Visibility = _role == AppRole.User
                ? Visibility.Collapsed
                : Visibility.Visible;

            UsersButton.Visibility = _role == AppRole.Admin
                ? Visibility.Visible
                : Visibility.Collapsed;

            HistoryButton.Visibility = _role == AppRole.User
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        public void Navigate(string page, object? parameter = null)
        {
            UserControl view;

            switch (page.ToLowerInvariant())
            {
                case "dashboard":
                    var dv = new DashboardView();
                    dv.ConfigureRole(_role.ToString());
                    view = dv;
                    break;

                case "orders":
                    view = _role == AppRole.Admin || _role == AppRole.Manager
                        ? new AdminOrdersView()
                        : new UserOrdersView();
                    break;

                case "history":
                    view = new HistoryView(_role);
                    break;

                case "products":
                    view = new ManageProductsView();
                    break;

                case "users":
                    view = new ManageUsersView();
                    break;

                case "createorder":
                    view = new CreateOrderView();
                    break;

                case "ordersheets":
                    view = new OrderSheetsView();
                    break;

                case "breakdown":
                    if (parameter is not OrderRow order)
                    {
                        Navigate("orders");
                        return;
                    }

                    view = new OrderBreakdownView(_role, order);
                    break;

                case "usersheet":
                    if (parameter is not OrderRow userOrder)
                    {
                        Navigate("orders");
                        return;
                    }

                    view = new UserOrderSheetView(userOrder);
                    break;

                default:
                    var dv2 = new DashboardView();
                    dv2.ConfigureRole(_role.ToString());
                    view = dv2;
                    page = "dashboard";
                    break;
            }

            MainContent.Content = view;
            UpdateActiveButton(page);
            CloseModal();
        }

        public void ShowModal(UserControl content, Brush? scrim = null, bool closeOnNavigate = true)
        {
            ModalScrim.Background = scrim ?? (Brush)FindResource("ScrimDetail");
            ModalContent.Content = content;
            ModalLayer.Visibility = Visibility.Visible;
        }

        public void CloseModal()
        {
            ModalContent.Content = null;
            ModalLayer.Visibility = Visibility.Collapsed;
        }

        public void CloseAllModals()
        {
            CloseModal();
        }

        private void UpdateActiveButton(string page)
        {
            Button[] buttons =
            {
                DashboardButton,
                OrdersButton,
                HistoryButton,
                ProductsButton,
                UsersButton
            };

            var inactiveBg = Brushes.Transparent;
            var inactiveFg = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0));

            var activeBg = Brushes.White;
            var activeFg = new SolidColorBrush(Color.FromRgb(0x1B, 0x21, 0x36));

            foreach (Button button in buttons)
            {
                button.Background = inactiveBg;
                button.Foreground = inactiveFg;
            }

            Button active = page.ToLowerInvariant() switch
            {
                "ordersheets" or "createorder" or "breakdown" or "usersheet" or "orders" => OrdersButton,
                "history" => HistoryButton,
                "products" => ProductsButton,
                "users" => UsersButton,
                _ => DashboardButton
            };

            active.Background = activeBg;
            active.Foreground = activeFg;
        }

        private void DashboardButton_Click(object sender, RoutedEventArgs e) => Navigate("dashboard");
        private void OrdersButton_Click(object sender, RoutedEventArgs e) => Navigate("orders");
        private void HistoryButton_Click(object sender, RoutedEventArgs e) => Navigate("history");
        private void ProductsButton_Click(object sender, RoutedEventArgs e) => Navigate("products");
        private void UsersButton_Click(object sender, RoutedEventArgs e) => Navigate("users");

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var app = Application.Current as App;

            Close();

            app?.SignOut();
        }

        protected override void OnClosed(EventArgs e)
        {
            if (ReferenceEquals(Current, this))
            {
                Current = null;
            }

            base.OnClosed(e);
        }
    }
}