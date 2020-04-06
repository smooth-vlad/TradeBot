using System.Windows;

namespace TradeBot
{
    /// <summary>
    ///     Логика взаимодействия для MovingAverageDialog.xaml
    /// </summary>
    public partial class MovingAverageDialog : Window
    {
        public enum CalculationMethod
        {
            Simple,
            Exponential
        }

        public MovingAverageDialog()
        {
            InitializeComponent();

            TypeComboBox.Items.Add(CalculationMethod.Simple.ToString());
            TypeComboBox.Items.Add(CalculationMethod.Exponential.ToString());
            TypeComboBox.SelectedIndex = 0;
        }

        public int Period { get; private set; }
        public CalculationMethod Type { get; private set; }
        public float Weight { get; private set; }

        void addButton_Click(object sender, RoutedEventArgs e)
        {
            PeriodErrorTextBlock.Text = string.Empty;
            WeightErrorTextBlock.Text = string.Empty;
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
            
            Period = period;
            Type = (CalculationMethod) TypeComboBox.SelectedIndex;
            DialogResult = true;
        }

        void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}