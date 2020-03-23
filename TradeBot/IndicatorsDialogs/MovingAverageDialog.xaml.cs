using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TradeBot
{
    /// <summary>
    /// Логика взаимодействия для MovingAverageDialog.xaml
    /// </summary>
    public partial class MovingAverageDialog : Window
    {
        public enum CalculationMethod
        {
            Simple,
            Exponential,
        }

        public int Period { get; private set; }
        public int Offset { get; private set; }
        public CalculationMethod Type { get; private set; }

        public MovingAverageDialog()
        {
            InitializeComponent();

            typeComboBox.Items.Add(CalculationMethod.Simple.ToString());
            typeComboBox.Items.Add(CalculationMethod.Exponential.ToString());
            typeComboBox.SelectedIndex = 0;
        }

        private void addButton_Click(object sender, RoutedEventArgs e)
        {
            periodErrorTextBlock.Text = string.Empty;
            offsetErrorTextBlock.Text = string.Empty;
            int period;
            if (!int.TryParse(periodTextBox.Text.Trim(), out period))
            {
                periodErrorTextBlock.Text = "* Not a number";
                periodTextBox.Focus();
                return;
            }
            if (period < 1)
            {
                periodErrorTextBlock.Text = "* Value should be >= 1";
                periodTextBox.Focus();
                return;
            }

            int offset;
            if (!int.TryParse(offsetTextBox.Text.Trim(), out offset))
            {
                offsetErrorTextBlock.Text = "* Not a number";
                offsetTextBox.Focus();
                return;
            }
            if (offset < 0)
            {
                offsetErrorTextBlock.Text = "* Value should be positive";
                offsetTextBox.Focus();
                return;
            }

            Period = period;
            Offset = offset;
            Type = (CalculationMethod)typeComboBox.SelectedIndex;
            DialogResult = true;
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
