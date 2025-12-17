using Microsoft.Extensions.DependencyInjection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TestTaska.ViewModels;

namespace TestTaska
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public MainWindow(MainViewModel mainViewModel, IServiceProvider serviceProvider) : this()
        {

            this.DataContext = mainViewModel;

            ReceiptsTab.DataContext = serviceProvider.GetRequiredService<ReceiptsViewModel>();
            StockOutsTab.DataContext = serviceProvider.GetRequiredService<StockOutsViewModel>();
        }
    }
}