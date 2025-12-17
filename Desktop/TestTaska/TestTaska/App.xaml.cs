using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Configuration;
using System.Data;
using System.Runtime.InteropServices.JavaScript;
using System.Windows;
using TestTaska.Data;
using TestTaska.ViewModels;

namespace TestTaska
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly IHost _host;

        public App()
        {
            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    ConfigureServices(services);
                })
                .Build();
        }
        private void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<SqlRepository>();

            services.AddSingleton<MainViewModel>();

            services.AddTransient<ReceiptsViewModel>();
            services.AddTransient<StockOutsViewModel>();

            services.AddSingleton<MainWindow>();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            await _host.StartAsync();

            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();

            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            if (_host.Services.GetService<MainViewModel>() is ViewModelBase mainVm)
            {
                mainVm.Dispose();
            }
            if (_host.Services.GetService<ReceiptsViewModel>() is ViewModelBase receiptsVm)
            {
                receiptsVm.Dispose();
            }
            if (_host.Services.GetService<StockOutsViewModel>() is ViewModelBase stockOutsVm)
            {
                stockOutsVm.Dispose();
            }

            using (_host)
            {
                await _host.StopAsync(TimeSpan.FromSeconds(5));
            }

            base.OnExit(e);
        }
    }
}
