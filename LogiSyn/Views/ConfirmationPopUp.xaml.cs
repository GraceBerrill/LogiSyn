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
using System.Windows.Shapes;

namespace LogiSyn.Views
{
    /// <summary>
    /// Interaction logic for ConfirmationPopUp.xaml
    /// </summary>
    public partial class ConfirmationPopUp : Window
    {
        // Constructor for the ConfirmationPopUp class
        public ConfirmationPopUp()
        {
            InitializeComponent();

            this.PreviewKeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter || e.Key == Key.Escape)
                {
                    DialogResult = true;
                    Close();
                }
            };
        }

        //------------------------------------------------------------------------------------------------//

        // Event handler for the Done button click event
        private void BtnDone_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}

//--------------------------------------End of File----------------------------------------------------------//
