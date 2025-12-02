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

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            if (this.DataContext is IDisposable mainVm)
            {
                mainVm.Dispose();
            }

            if (FindName("ReceiptsTab") is TabItem receiptsTab
                && receiptsTab.DataContext is IDisposable receiptsVm)
            {
                receiptsVm.Dispose();
            }

            if (FindName("StockOutsTab") is TabItem stockOutsTab
                && stockOutsTab.DataContext is IDisposable stockOutsVm)
            {
                stockOutsVm.Dispose();
            }

            Application.Current.Shutdown();
        }
    }
}