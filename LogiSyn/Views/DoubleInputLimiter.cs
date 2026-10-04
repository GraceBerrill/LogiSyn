// Adriaan
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LogiSyn.Views
{
    /// <summary>
    /// Attached property and behavior that restricts a TextBox to only accept valid double numeric values.
    /// Prevents non-numeric characters, letters, symbols, spaces, multiple decimal points, and invalid pasted text.
    /// </summary>
    public static class DoubleInputLimiter
    {
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(DoubleInputLimiter),
                new UIPropertyMetadata(false, OnIsEnabledChanged));

        //------------------------------------------------------------------------------------------------//

        public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);
        public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

        //------------------------------------------------------------------------------------------------//

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not TextBox textBox) return;

            if ((bool)e.NewValue)
            {
                textBox.PreviewTextInput += TextBox_PreviewTextInput;
                textBox.PreviewKeyDown += TextBox_PreviewKeyDown;
                textBox.LostFocus += TextBox_LostFocus;
                DataObject.AddPastingHandler(textBox, TextBox_Pasting);
            }
            else
            {
                textBox.PreviewTextInput -= TextBox_PreviewTextInput;
                textBox.PreviewKeyDown -= TextBox_PreviewKeyDown;
                textBox.LostFocus -= TextBox_LostFocus;
                DataObject.RemovePastingHandler(textBox, TextBox_Pasting);
            }
        }

        //------------------------------------------------------------------------------------------------//

        private static void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Disallow spaces
            if (e.Key == Key.Space)
            {
                e.Handled = true;
            }
        }

        //------------------------------------------------------------------------------------------------//

        private static void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (sender is not TextBox textBox) return;

            int selStart = textBox.SelectionStart;
            int selLength = textBox.SelectionLength;
            string currentText = textBox.Text ?? string.Empty;

            // Treat comma as decimal separator, normalizing to dot
            string inputChar = e.Text;
            bool isComma = inputChar == ",";
            if (isComma)
            {
                inputChar = ".";
            }

            string proposed = currentText.Remove(selStart, selLength).Insert(selStart, inputChar);

            if (!IsValidDoublePartial(proposed))
            {
                e.Handled = true;
                return;
            }

            if (isComma)
            {
                e.Handled = true;
                textBox.Text = proposed;
                textBox.CaretIndex = selStart + 1;
            }
        }

        //------------------------------------------------------------------------------------------------//

        private static void TextBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (sender is not TextBox textBox) return;

            if (e.DataObject.GetDataPresent(DataFormats.Text))
            {
                // Get the pasted text, normalize commas to dots, and validate
                string rawPaste = (e.DataObject.GetData(DataFormats.Text) as string ?? string.Empty).Trim();
                string pasteNormalized = rawPaste.Replace(',', '.');

                // Get the current text and selection in the TextBox
                int selStart = textBox.SelectionStart;
                int selLength = textBox.SelectionLength;
                string currentText = textBox.Text ?? string.Empty;

                string proposed = currentText.Remove(selStart, selLength).Insert(selStart, pasteNormalized);

                if (IsValidDoubleFull(proposed))
                {
                    e.CancelCommand();
                    textBox.Text = proposed;
                    textBox.CaretIndex = selStart + pasteNormalized.Length;
                }
                else
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        //------------------------------------------------------------------------------------------------//

        // Event handler for when the TextBox loses focus, ensuring valid formatting
        private static void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is not TextBox textBox) return;

            string text = textBox.Text.Trim();
            if (text == "." || text == "-")
            {
                textBox.Text = "0";
            }
            else if (text.EndsWith("."))
            {
                textBox.Text = text.TrimEnd('.');
            }
        }

        //------------------------------------------------------------------------------------------------//

        // validates that the user input is a valid double number
        public static bool IsValidDoublePartial(string text)
        {
            if (string.IsNullOrEmpty(text)) return true;
            // Allows numbers with at most one decimal point: "", ".", ".5", "5", "5.", "5.25"
            return Regex.IsMatch(text, @"^([0-9]+(\.[0-9]*)?|\.[0-9]*)?$");
        }

        //------------------------------------------------------------------------------------------------//

        // validates that the user input is a valid double number and non-negative
        public static bool IsValidDoubleFull(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return true;
            return double.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double val) && val >= 0;
        }
    }
}

