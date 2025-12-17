using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using TestTaska.Data;
using TestTaska.Models;
using System.Linq;

namespace TestTaska.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        private readonly SqlRepository _repository;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddProductExecuteCommand))]
        private bool _isBusy;

        [ObservableProperty]
        private ObservableCollection<ProductStockDisplay> _stockList = new ObservableCollection<ProductStockDisplay>();

        [ObservableProperty]
        private string _newProductName = string.Empty;

        private bool CanAddProductExecute() => !IsBusy;

        public MainViewModel(SqlRepository repository) : base()
        {
            _repository = repository;

            SqlRepository.StockMovementUpdated -= OnStockMovementChanged;
            SqlRepository.StockMovementUpdated += OnStockMovementChanged;

            SqlRepository.ProductsUpdated += async () =>
            {
                await LoadDataAsync();
            };

            _ = LoadDataAsync();
        }

        private void OnStockMovementChanged()
        {
            _ = LoadDataAsync();
        }

        public void Dispose()
        {
            SqlRepository.StockMovementUpdated -= OnStockMovementChanged;
            base.Dispose();
        }


        private async Task LoadDataAsync()
        {
            var result = await _repository.GetCurrentStockAsync();

            if (result.IsSuccess && result.Data != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    StockList.Clear();
                    foreach (var item in result.Data)
                    {
                        StockList.Add(item);
                    }
                });
            }
            else
            {
                await Task.Run(() =>
                    MessageBox.Show($"Ошибка загрузки: {result.ErrorMessage}", "Ошибка БД",
                        MessageBoxButton.OK, MessageBoxImage.Error)
                );
            }
        }

        //[AsyncRelayCommand(CanExecute = nameof(CanAddProductExecute))] кодген Toolkit отказывается генерировать инструкции для AsyncRelayCommand(WIP)
        [RelayCommand(CanExecute = nameof(CanAddProductExecute))]
        private Task AddProductExecuteAsync()
        {
            return Task.Run(async () =>
            {
                string nameToValidate = string.Empty;
                Application.Current.Dispatcher.Invoke(() => nameToValidate = NewProductName?.Trim());

                if (string.IsNullOrWhiteSpace(nameToValidate))
                {
                    await Task.Run(() => MessageBox.Show("Наименование товара не может быть пустым.", "Внимание"));
                    return;
                }

                Application.Current.Dispatcher.Invoke(() => IsBusy = true);

                try
                {
                    bool isDuplicate = await _repository.CheckDuplicateProductAsync(nameToValidate);

                    if (isDuplicate)
                    {
                        await Task.Run(() =>
                            MessageBox.Show($"Товар с именем '{nameToValidate}' уже существует!",
                            "Дубликат", MessageBoxButton.OK, MessageBoxImage.Warning));

                        return;
                    }

                    var addResult = await _repository.AddProductAsync(nameToValidate);

                    if (addResult.IsSuccess)
                    {
                        Application.Current.Dispatcher.Invoke(() => NewProductName = string.Empty);

                        var stockResult = await _repository.GetCurrentStockAsync();
                        if (stockResult.IsSuccess && stockResult.Data != null)
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                StockList.Clear();
                                foreach (var item in stockResult.Data) StockList.Add(item);
                            });
                        }
                    }
                    else
                    {
                        await Task.Run(() => MessageBox.Show(addResult.ErrorMessage, "Ошибка"));
                    }
                }
                catch (Exception ex)
                {
                    await Task.Run(() => MessageBox.Show($"Критическая ошибка: {ex.Message}"));
                }
                finally
                {
                    Application.Current.Dispatcher.Invoke(() => IsBusy = false);
                }
            });
        }
    }
}