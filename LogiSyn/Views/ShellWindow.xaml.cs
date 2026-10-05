using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using SharedLibrary.Model;
using AndersonsBakeryAPI.Services;

namespace LogiSyn.Views
{
    public partial class ShellWindow : Window
    {
        public static ShellWindow? Current { get; set; }

        private readonly AppRole _role;
        private bool _allowCloseWithoutShutdown;

        public ShellWindow(AppRole role)
        {
            InitializeComponent();
            _role = role;

            Current = this;
            RoleText.Text = role.ToString();

            // Subscribe to API status changes
            ApiClient.OnStatusChanged += UpdateApiStatus;
            UpdateApiStatus(ApiClient.CurrentState, ApiClient.CurrentMessage);

            // Initial API check in background
            Loaded += async (s, e) =>
            {
                try
                {
                    var client = new ApiClient();
                    await client.IsApiAvailableAsync();
                }
                catch { }
            };

            Closed += (s, e) =>
            {
                ApiClient.OnStatusChanged -= UpdateApiStatus;
            };

            ConfigureNavigation();
            Navigate("dashboard");
        }

        private void ConfigureNavigation()
        {
            ProductsButton.Visibility = _role == AppRole.User
                ? Visibility.Collapsed
                : Visibility.Visible;

            UsersButton.Visibility = (_role == AppRole.Admin || _role == AppRole.Manager)
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
                    var dv = new DashboardView(_role);
                    dv.ConfigureRole(_role.ToString());
                    view = dv;
                    break;

                // Adriaan - Order View Navigation
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

                // Adriaan - Order Creation, Sheets, and Breakdown Navigation
                case "createorder":
                    view = new CreateOrderView();
                    break;

                case "ordersheets":
                    view = parameter is OrderScaled scaledOrder
                        ? new OrderSheetsView(scaledOrder)
                        : new OrderSheetsView();
                    break;

                case "breakdown":
                    if (parameter is not OrderRow orderRow)
                    {
                        Navigate("orders");
                        return;
                    }

                    view = new OrderBreakdownView(_role, orderRow);
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
                    var dv2 = new DashboardView(_role);
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

        private DispatcherTimer? _toastTimer;

        public void ShowToast(string message, bool isWarning = false)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => ShowToast(message, isWarning));
                return;
            }

            if (ToastMessage == null || ToastNotification == null || ToastIcon == null) return;

            ToastMessage.Text = message;
            ToastIcon.Text = isWarning ? "\uE7BA" : "\uE73E";
            ToastIcon.Foreground = isWarning
                ? Brushes.Orange
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8DBE98"));

            ToastNotification.Visibility = Visibility.Visible;

            _toastTimer?.Stop();
            _toastTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3.5) };
            _toastTimer.Tick += (s, e) =>
            {
                ToastNotification.Visibility = Visibility.Collapsed;
                _toastTimer.Stop();
            };
            _toastTimer.Start();
        }

        public void UpdateApiStatus(ApiConnectionState state, string? message = null)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => UpdateApiStatus(state, message));
                return;
            }

            if (ApiStatusDot == null || ApiStatusText == null || ApiStatusBadge == null) return;

            switch (state)
            {
                case ApiConnectionState.CallingApi:
                    ApiStatusDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#61AFEF")); // Blue
                    ApiStatusText.Text = string.IsNullOrWhiteSpace(message) ? "Calling API…" : message;
                    ApiStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A8D1FF"));
                    ApiStatusBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1D2D44"));
                    ApiStatusBadge.ToolTip = "Connecting to Andersons Bakery API (in-flight request)...";
                    break;

                case ApiConnectionState.Online:
                    ApiStatusDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8DBE98")); // Green
                    ApiStatusText.Text = string.IsNullOrWhiteSpace(message) ? "API Connected" : message;
                    ApiStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C5E8CC"));
                    ApiStatusBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E3029"));
                    ApiStatusBadge.ToolTip = "Connected to Andersons Bakery API. Click to test connection.";
                    break;

                case ApiConnectionState.FallbackLocal:
                    ApiStatusDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFA726")); // Amber / Warm Orange
                    ApiStatusText.Text = string.IsNullOrWhiteSpace(message) ? "Local Storage (Fallback)" : message;
                    ApiStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD199"));
                    ApiStatusBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#362B1E"));
                    ApiStatusBadge.ToolTip = "Could not reach API. Operating in Local Storage fallback mode. Click to retry.";
                    break;
            }
        }

        private async void ApiStatusBadge_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ShowToast("Testing API connectivity…");
            var client = new ApiClient();
            bool isAvailable = await client.IsApiAvailableAsync();
            if (isAvailable)
            {
                ShowToast("API connection active!");
            }
            else
            {
                ShowToast("API unreachable. Using Local Storage fallback.", isWarning: true);
            }
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
        // Adriaan
        private void OrdersButton_Click(object sender, RoutedEventArgs e) => Navigate("orders");
        private void HistoryButton_Click(object sender, RoutedEventArgs e) => Navigate("history");
        private void ProductsButton_Click(object sender, RoutedEventArgs e) => Navigate("products");
        private void UsersButton_Click(object sender, RoutedEventArgs e) => Navigate("users");

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var app = Application.Current as App;

            _allowCloseWithoutShutdown = true;
            Close();

            app?.SignOut();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (!_allowCloseWithoutShutdown)
            {
                _allowCloseWithoutShutdown = true;
                Application.Current.Shutdown();
            }

            base.OnClosing(e);
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