// Adriaan
using LogiSyn.Interface;
using LogiSyn.Model;
using LogiSyn.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LogiSyn.Views
{
    /// <summary>
    /// Interaction logic for UserOrderPageReview.xaml
    /// </summary>
    public partial class UserOrderPageReview : Page
    {
        private readonly OrderScaled _order;
        private readonly IOrderService _orderService;

        //------------------------------------------------------------------------------------------------//

        // Constructor for the UserOrderPageReview class
        public UserOrderPageReview(OrderScaled order)
        {
            InitializeComponent();
            _order = order ?? throw new ArgumentNullException(nameof(order));
            _orderService = new OrderService();

            PopulateUI();
        }

        //------------------------------------------------------------------------------------------------//

        private List<UserProductionItemViewModel> _presentationItems = new();

        private void PopulateUI()
        {
            TxtOrderTitle.Text = $"{_order.Customer} Order {_order.OrderId}".Trim();
            TxtOrderDate.Text = _order.OrderDate.ToString("d MMMM yyyy");

            // Wrap items with editable presentation bindings
            _presentationItems = _order.productionItems.Select(item => new UserProductionItemViewModel
            {
                ProductName = item.ProductName,
                Amount = item.Amount > 0 ? item.Amount.ToString() : "100",
                ProductionLine = item.ProductionLine,
                Packaging = item.packaging,
                Notes = item.Notes ?? string.Empty,
                Ingredients = item.ReqIngredients.Select(ing => new UserIngredientViewModel
                {
                    IngredientName = ing.IngredientName,
                    DisplayAmount = $"{ing.IngredientAmount} {ing.MeasuredIngredient}".Trim(),
                    DisplayAdditional = $"{ing.AdditionsAmount} {ing.MeasuredIngredient}".Trim(),
                    DisplayUsed = ing.AmountUsed > 0 ? ing.AmountUsed.ToString(System.Globalization.CultureInfo.InvariantCulture) : string.Empty
                }).ToList()
            }).ToList();

            ItemsProductionList.ItemsSource = _presentationItems;
        }

        //------------------------------------------------------------------------------------------------//

        private void SyncPresentationItemsToOrder()
        {
            if (_presentationItems == null || _order.productionItems == null) return;

            for (int i = 0; i < _presentationItems.Count && i < _order.productionItems.Count; i++)
            {
                var pvm = _presentationItems[i];
                var pItem = _order.productionItems[i];

                pItem.Notes = pvm.Notes ?? string.Empty;

                if (pvm.Ingredients != null && pItem.ReqIngredients != null)
                {
                    for (int j = 0; j < pvm.Ingredients.Count && j < pItem.ReqIngredients.Count; j++)
                    {
                        var ivm = pvm.Ingredients[j];
                        var ing = pItem.ReqIngredients[j];
                        if (double.TryParse(ivm.DisplayUsed, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double parsedUsed))
                        {
                            ing.AmountUsed = parsedUsed;
                        }
                        else if (string.IsNullOrWhiteSpace(ivm.DisplayUsed))
                        {
                            ing.AmountUsed = 0;
                        }
                    }
                }
            }
        }

        //------------------------------------------------------------------------------------------------//

        // Event handler for the Back button click event
        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            SyncPresentationItemsToOrder();
            _orderService.SaveOrder(_order);
            NavigationService?.GoBack();
        }

        //------------------------------------------------------------------------------------------------//

        // Event handler for the Print button click event
        private void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            SyncPresentationItemsToOrder();
            _orderService.SaveOrder(_order);

            var printDlg = new PrintDialog();
            if (printDlg.ShowDialog() == true)
            {
                MessageBox.Show($"Order sheet for {_order.OrderId} sent to printer.", "Print", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        //------------------------------------------------------------------------------------------------//

        // Event handler for the Mark Completed button click event
        private void BtnMarkCompleted_Click(object sender, RoutedEventArgs e)
        {
            SyncPresentationItemsToOrder();
            CompletedModalOverlay.Visibility = Visibility.Visible;
        }

        //------------------------------------------------------------------------------------------------//

        // Event handler for the Done button click event in the modal overlay
        private void BtnModalDone_Click(object sender, RoutedEventArgs e)
        {
            SyncPresentationItemsToOrder();

            // Mark status as completed
            _order.Status = "Completed";

            // If any inputs were left at zero, default them upon completion
            foreach (var item in _order.productionItems ?? new List<ProductionItem>())
            {
                if (item.packaging.PansUsed <= 0)
                {
                    item.packaging.PansUsed = item.packaging.Pans;
                }
                if (item.packaging.TrolleysUsed <= 0)
                {
                    item.packaging.TrolleysUsed = item.packaging.Trolleys;
                }
                foreach (var ing in item.ReqIngredients ?? new List<Ingredients>())
                {
                    if (ing.AmountUsed <= 0)
                    {
                        ing.AmountUsed = Math.Round(ing.IngredientAmount + ing.AdditionsAmount, 2);
                    }
                }
            }

            // Persist update through the shared OrderService
            _orderService.SaveOrder(_order);

            // Return to the user orders dashboard
            NavigationService?.Navigate(new UsersOrderPage());
        }
    }

    //------------------------------------------------------------------------------------------------//

    // View models supporting user data-entry
    public class UserProductionItemViewModel
    {
        public string ProductName { get; set; } = string.Empty;
        public string Amount { get; set; } = string.Empty;
        public string ProductionLine { get; set; } = string.Empty;
        public Packaging Packaging { get; set; } = new();
        public List<UserIngredientViewModel> Ingredients { get; set; } = new();
        public string Notes { get; set; } = string.Empty;

        private string _pansUsedStr = string.Empty;
        private bool _pansInitialized = false;

        public string PansUsed
        {
            get
            {
                if (!_pansInitialized)
                {
                    _pansInitialized = true;
                    _pansUsedStr = Packaging.PansUsed > 0 ? Packaging.PansUsed.ToString(System.Globalization.CultureInfo.InvariantCulture) : string.Empty;
                }
                return _pansUsedStr;
            }
            set
            {
                _pansInitialized = true;
                _pansUsedStr = value ?? string.Empty;
                if (double.TryParse(_pansUsedStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double d))
                {
                    Packaging.PansUsed = d;
                }
                else if (string.IsNullOrWhiteSpace(_pansUsedStr))
                {
                    Packaging.PansUsed = 0;
                }
            }
        }

        private string _trolleysUsedStr = string.Empty;
        private bool _trolleysInitialized = false;

        public string TrolleysUsed
        {
            get
            {
                if (!_trolleysInitialized)
                {
                    _trolleysInitialized = true;
                    _trolleysUsedStr = Packaging.TrolleysUsed > 0 ? Packaging.TrolleysUsed.ToString(System.Globalization.CultureInfo.InvariantCulture) : string.Empty;
                }
                return _trolleysUsedStr;
            }
            set
            {
                _trolleysInitialized = true;
                _trolleysUsedStr = value ?? string.Empty;
                if (double.TryParse(_trolleysUsedStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double d))
                {
                    Packaging.TrolleysUsed = d;
                }
                else if (string.IsNullOrWhiteSpace(_trolleysUsedStr))
                {
                    Packaging.TrolleysUsed = 0;
                }
            }
        }
    }

    //------------------------------------------------------------------------------------------------//

    public class UserIngredientViewModel
    {
        public string IngredientName { get; set; } = string.Empty;
        public string DisplayAmount { get; set; } = string.Empty;
        public string DisplayAdditional { get; set; } = string.Empty;
        public string DisplayUsed { get; set; } = string.Empty;
    }
}

//--------------------------------------End of File----------------------------------------------------------//
