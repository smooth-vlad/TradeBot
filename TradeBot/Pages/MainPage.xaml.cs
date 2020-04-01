using System.Windows;
using System.Windows.Controls;
using Tinkoff.Trading.OpenApi.Network;

namespace TradeBot
{
    /// <summary>
    ///     Логика взаимодействия для MainPage.xaml
    /// </summary>
    public partial class MainPage : Page
    {
        readonly Context context;

        public MainPage(Context context)
        {
            InitializeComponent();
            this.context = context;

            AddStockSelectionTab();
        }

        void AddStockSelectionTab()
        {
            var newItem = new TabItem();
            newItem.Content = new InstrumentSelection(context, newItem);
            newItem.Header = "Instrument Selection";
            newItem.IsSelected = true;

            TabControl.Items.Insert(TabControl.Items.Count - 1, newItem);
        }

        void AddTabButton_Selected(object sender, RoutedEventArgs e)
        {
            AddStockSelectionTab();

            var s = sender as TabItem;
            s.IsSelected = false;

            e.Handled = true;
        }
    }
}