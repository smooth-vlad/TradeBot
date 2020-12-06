using System;
using System.Windows;

namespace TradeBot
{
    /// <summary>
    ///     Логика взаимодействия для MovingAverageDialog.xaml
    /// </summary>
    public partial class RsiDialog : Window
    {
        public RsiDialog()
        {
            InitializeComponent();
        }

        public int Period { get; private set; }
        public double OverboughtLine { get; private set; }
        public double OversoldLine { get; private set; }

        private void addButton_Click(object sender, RoutedEventArgs e)
        {
            PeriodErrorTextBlock.Text = string.Empty;
            OverboughtLineErrorTextBlock.Text = string.Empty;
            OversoldLineErrorTextBlock.Text = string.Empty;

            {
                if (!int.TryParse(PeriodTextBox.Text.Trim(), out var period))
                {
                    PeriodErrorTextBlock.Text = "* Not a number";
                    PeriodTextBox.Focus();
                    return;
                }

                if (period < 1)
                {
                    PeriodErrorTextBlock.Text = "* Value should be >= 1";
                    PeriodTextBox.Focus();
                    return;
                }

                Period = period;
            }

            {
                if (OverboughtLineSlider.Value <= OversoldLineSlider.Value)
                {
                    OverboughtLineErrorTextBlock.Text = "* should be higher than Oversold Line";
                    OversoldLineErrorTextBlock.Text = "* should be lower than Overbought Line";
                    OverboughtLineSlider.Focus();
                    return;
                }

                OverboughtLine = OverboughtLineSlider.Value;
                OversoldLine = OversoldLineSlider.Value;
            }

            DialogResult = true;
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void OverboughtLineSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            OverboughtLineSlider.Value = Math.Round(e.NewValue);

            if (OverboughtLineSlider == null || OversoldLineSlider == null)
                return;
            if (OverboughtLineSlider.Value <= OversoldLineSlider.Value)
            {
                OverboughtLineErrorTextBlock.Text = "* should be higher than Oversold Line";
                OversoldLineErrorTextBlock.Text = "* should be lower than Overbought Line";
            }
            else
            {
                OverboughtLineErrorTextBlock.Text = string.Empty;
                OversoldLineErrorTextBlock.Text = string.Empty;
            }
        }

        private void OversoldLineSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            OversoldLineSlider.Value = Math.Round(e.NewValue);

            if (OverboughtLineSlider == null || OversoldLineSlider == null)
                return;
            if (OverboughtLineSlider.Value <= OversoldLineSlider.Value)
            {
                OverboughtLineErrorTextBlock.Text = "* should be higher than Oversold Line";
                OversoldLineErrorTextBlock.Text = "* should be lower than Overbought Line";
            }
            else
            {
                OverboughtLineErrorTextBlock.Text = string.Empty;
                OversoldLineErrorTextBlock.Text = string.Empty;
            }
        }
    }
}