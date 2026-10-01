using System.Windows;
using System.Windows.Controls;
using LogiSyn.Model;

namespace LogiSyn.Views
{
    public partial class ProductDetailModal : UserControl
    {
        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register("IsReadOnly", typeof(bool), typeof(ProductDetailModal),
                                        new PropertyMetadata(true));

        public bool IsReadOnly
        {
            get { return (bool)GetValue(IsReadOnlyProperty); }
            set { SetValue(IsReadOnlyProperty, value); }
        }

        public ProductDetailModal(ProductDetail detail, bool editable)
        {
            InitializeComponent();

            DataContext = detail;
            IsReadOnly = !editable;
            SaveButton.Visibility = editable ? Visibility.Visible : Visibility.Collapsed;
            DateAddedText.Text = "Date Added: " + detail.DateAdded;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            ShellWindow.Current.CloseModal();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO (backend): save the edited product (the DataContext holds the new values)
            ShellWindow.Current.CloseModal();
        }
    }
}