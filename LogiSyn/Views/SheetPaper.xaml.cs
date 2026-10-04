// Adriaan
using System.Windows;
using System.Windows.Controls;

namespace LogiSyn.Views
{
    public partial class SheetPaper : UserControl
    {
        /// <summary>true = look only (finished sheet); false = the baker can type into "Used" and "Notes".</summary>
        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register("IsReadOnly", typeof(bool), typeof(SheetPaper),
                                        new PropertyMetadata(true));

        public bool IsReadOnly
        {
            get { return (bool)GetValue(IsReadOnlyProperty); }
            set { SetValue(IsReadOnlyProperty, value); }
        }

        public SheetPaper()
        {
            InitializeComponent();
        }
    }
}