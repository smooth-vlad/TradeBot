using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Tinkoff.Trading.OpenApi.Network;

namespace TradeBot
{
    /// <summary>
    /// Логика взаимодействия для MainPage.xaml
    /// </summary>
    public partial class MainPage : Page
    {
        private Context context;

        public MainPage(Context context)
        {
            InitializeComponent();
            this.context = context;

            AddStockSelectionTab();
        }

        private void AddStockSelectionTab()
        {
            var newItem = new TabItem();
            newItem.Content = new StockSelection(context, newItem);
            newItem.Header = "Instrument Selection";
            newItem.IsSelected = true;

            tabControl.Items.Insert(tabControl.Items.Count - 1, newItem);
        }

        private void AddTabButton_Selected(object sender, RoutedEventArgs e)
        {
            AddStockSelectionTab();

            var s = sender as TabItem;
            s.IsSelected = false;

            e.Handled = true;
        }

        private void MovingAverage_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new MovingAverageDialog();
            if (dialog.ShowDialog() == true)
            {
                var tab = tabControl.SelectedContent;
                if (tab.GetType() == typeof(RealTimeTrading))
                    (tab as RealTimeTrading).tradingChart.AddIndicator(new MovingAverage(dialog.Period, dialog.Offset, dialog.Type));
                else if (tab.GetType() == typeof(TestingTrading))
                    (tab as TestingTrading).tradingChart.AddIndicator(new MovingAverage(dialog.Period, dialog.Offset, dialog.Type));
            }
        }

        private void RemoveIndicators_Click(object sender, RoutedEventArgs e)
        {
            var tab = tabControl.SelectedContent;
            if (tab.GetType() == typeof(RealTimeTrading))
                (tab as RealTimeTrading).tradingChart.RemoveIndicators();
            else if (tab.GetType() == typeof(TestingTrading))
                (tab as TestingTrading).tradingChart.RemoveIndicators();
        }
    }
}
