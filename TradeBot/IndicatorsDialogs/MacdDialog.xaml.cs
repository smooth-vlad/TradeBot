using System.Windows;

namespace TradeBot
{
    /// <summary>
    ///     Логика взаимодействия для MovingAverageDialog.xaml
    /// </summary>
    public partial class MacdDialog : Window
    {
        public enum CalculationMethod
        {
            Simple,
            Exponential
        }

        public MacdDialog()
        {
            InitializeComponent();

            TypeComboBox.Items.Add(CalculationMethod.Simple.ToString());
            TypeComboBox.Items.Add(CalculationMethod.Exponential.ToString());
            TypeComboBox.SelectedIndex = 1;
        }

        public int ShortPeriod { get; private set; }
        public int LongPeriod { get; private set; }
        public int HistogramPeriod { get; private set; }
        public float Weight { get; private set; }
        public CalculationMethod Type { get; private set; }

        private void addButton_Click(object sender, RoutedEventArgs e)
        {
            ShortPeriodErrorTextBlock.Text = string.Empty;
            LongPeriodErrorTextBlock.Text = string.Empty;
            HistogramPeriodErrorTextBlock.Text = string.Empty;
            WeightErrorTextBlock.Text = string.Empty;

            {
                if (!int.TryParse(ShortPeriodTextBox.Text.Trim(), out var shortPeriod))
                {
                    ShortPeriodErrorTextBlock.Text = "* Not a number";
                    ShortPeriodTextBox.Focus();
                    return;
                }

                if (shortPeriod < 1)
                {
                    ShortPeriodErrorTextBlock.Text = "* Value should be >= 1";
                    ShortPeriodTextBox.Focus();
                    return;
                }

                ShortPeriod = shortPeriod;
            }

            {
                if (!int.TryParse(LongPeriodTextBox.Text.Trim(), out var longPeriod))
                {
                    LongPeriodErrorTextBlock.Text = "* Not a number";
                    LongPeriodTextBox.Focus();
                    return;
                }

                if (longPeriod < 1)
                {
                    LongPeriodErrorTextBlock.Text = "* Value should be >= 1";
                    LongPeriodTextBox.Focus();
                    return;
                }

                if (longPeriod <= ShortPeriod)
                {
                    LongPeriodErrorTextBlock.Text = "* Value should be > 'Short Period'";
                    LongPeriodTextBox.Focus();
                    return;
                }

                LongPeriod = longPeriod;
            }

            {
                if (!int.TryParse(HistogramPeriodTextBox.Text.Trim(), out var histogramPeriod))
                {
                    HistogramPeriodErrorTextBlock.Text = "* Not a number";
                    HistogramPeriodTextBox.Focus();
                    return;
                }

                if (histogramPeriod < 1)
                {
                    HistogramPeriodErrorTextBlock.Text = "* Value should be >= 1";
                    HistogramPeriodTextBox.Focus();
                    return;
                }
                HistogramPeriod = histogramPeriod;
            }

            {
                if (!float.TryParse(WeightTextBox.Text.Trim().Replace('.', ','), out var weight))
                {
                    WeightErrorTextBlock.Text = "* Not a number";
                    WeightTextBox.Focus();
                    return;
                }

                if (weight < 0 || weight > 10)
                {
                    WeightErrorTextBlock.Text = "* Value should be >= 0 and <= 10";
                    WeightTextBox.Focus();
                    return;
                }
                Weight = weight;
            }

            Type = (CalculationMethod)TypeComboBox.SelectedIndex;
            DialogResult = true;
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}