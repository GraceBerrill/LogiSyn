// Adriaan
using System.Windows;
using System.Windows.Controls;

namespace LogiSyn.Views
{
    public partial class SheetPaper : UserControl
    {
        // Makes file readonly and ensures only user required fields are available
        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register("IsReadOnly", typeof(bool), typeof(SheetPaper),
                                        new PropertyMetadata(true));

        //------------------------------------------------------------------------------------------------//

        public bool IsReadOnly
        {
            get { return (bool)GetValue(IsReadOnlyProperty); }
            set { SetValue(IsReadOnlyProperty, value); }
        }

        //------------------------------------------------------------------------------------------------//

        public SheetPaper()
        {
            InitializeComponent();
        }
    }
}

//--------------------------------------End of File----------------------------------------------------------//