using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LogiSyn.Views
{
    /// <summary>
    /// Attached property and behavior that restricts a TextBox to only accept valid integer digits (0-9).
    /// Prevents non-numeric characters, letters, symbols, spaces, decimals, and invalid pasted text.
    /// </summary>
    public static class IntegerInputLimiter
    {
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(IntegerInputLimiter),
                new UIPropertyMetadata(false, OnIsEnabledChanged));

        public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);
        public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not TextBox textBox) return;

            if ((bool)e.NewValue)
            {
                textBox.PreviewTextInput += TextBox_PreviewTextInput;
                textBox.PreviewKeyDown += TextBox_PreviewKeyDown;
                DataObject.AddPastingHandler(textBox, TextBox_Pasting);
            }
            else
            {
                textBox.PreviewTextInput -= TextBox_PreviewTextInput;
                textBox.PreviewKeyDown -= TextBox_PreviewKeyDown;
                DataObject.RemovePastingHandler(textBox, TextBox_Pasting);
            }
        }

        private static void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                e.Handled = true;
            }
        }

        private static void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            foreach (char c in e.Text)
            {
                if (!char.IsDigit(c))
                {
                    e.Handled = true;
                    return;
                }
            }
        }

        private static void TextBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (sender is not TextBox textBox) return;

            if (e.DataObject.GetDataPresent(DataFormats.Text))
            {
                string rawPaste = (e.DataObject.GetData(DataFormats.Text) as string ?? string.Empty).Trim();
                foreach (char c in rawPaste)
                {
                    if (!char.IsDigit(c))
                    {
                        e.CancelCommand();
                        return;
                    }
                }
            }
            else
            {
                e.CancelCommand();
            }
        }
    }
}

