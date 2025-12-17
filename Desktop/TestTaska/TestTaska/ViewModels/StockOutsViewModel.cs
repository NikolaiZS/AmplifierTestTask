using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using TestTaska.Data;
using TestTaska.Models;

namespace TestTaska.ViewModels
{
    public partial class StockOutsViewModel : ViewModelBase
    {
        private readonly SqlRepository _repository;

        [ObservableProperty]
        private DateTime _filterStartDate = DateTime.Now.AddMonths(-1).Date;

        [ObservableProperty]
        private DateTime _filterEndDate = DateTime.Now.Date;

        [ObservableProperty]
        private ObservableCollection<StockOut> _stockOutsList = new();

        [ObservableProperty]
        private StockOut? _selectedStockOut;

        [ObservableProperty]
        private ObservableCollection<Product> _availableProducts = new();

        [ObservableProperty]
        private StockOut _editingStockOut = new() { OutDate = DateTime.Now.Date, Quantity = 1 };

        [ObservableProperty]
        private Product? _selectedProductForEdit;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveStockOutCommand))]
        [NotifyCanExecuteChangedFor(nameof(DeleteStockOutCommand))]
        private bool _isBusy;

        private bool CanExecuteAction() => !IsBusy;

        public StockOutsViewModel(SqlRepository repository) : base()
        {
            _repository = repository;

            SqlRepository.ProductsUpdated -= OnProductsListChanged;
            SqlRepository.ProductsUpdated += OnProductsListChanged;

            _ = LoadInitialData();
        }

        private void OnProductsListChanged()
        {
            _ = LoadProductsForComboBox();
        }

        private async Task LoadInitialData()
        {
            await LoadProductsForComboBox();
            await FilterStockOuts();
        }

        private async Task LoadProductsForComboBox()
        {
            var result = await _repository.GetAllProductsAsync();

            if (result.IsSuccess && result.Data != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    AvailableProducts = new ObservableCollection<Product>(result.Data);
                });
            }
            else
            {
                await ShowErrorMessage($"Ошибка при загрузке товаров: {result.ErrorMessage}");
            }
        }

        [RelayCommand]
        public async Task FilterStockOuts()
        {
            if (IsBusy) return;
            IsBusy = true;

            var result = await Task.Run(() => _repository.GetFilteredStockOutsAsync(FilterStartDate, FilterEndDate));

            if (result.IsSuccess && result.Data != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    StockOutsList.Clear();
                    foreach (var stockOut in result.Data)
                    {
                        StockOutsList.Add(stockOut);
                    }
                });
            }
            IsBusy = false;
        }

        [RelayCommand]
        public void NewStockOut()
        {
            EditingStockOut = new StockOut { OutDate = DateTime.Now.Date, Quantity = 1 };
            SelectedProductForEdit = null;
            SelectedStockOut = null;
        }

        [RelayCommand]
        public void EditSelectedStockOut()
        {
            if (SelectedStockOut == null)
            {
                MessageBox.Show("Выберите запись для редактирования.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            EditingStockOut = new StockOut
            {
                OutId = SelectedStockOut.OutId,
                ProductId = SelectedStockOut.ProductId,
                OutDate = SelectedStockOut.OutDate,
                Quantity = SelectedStockOut.Quantity
            };

            SelectedProductForEdit = AvailableProducts.FirstOrDefault(p => p.ProductId == SelectedStockOut.ProductId);
        }

        [RelayCommand(CanExecute = nameof(CanExecuteAction))]
        public Task SaveStockOut()
        {
            return Task.Run(async () =>
            {
                if (SelectedProductForEdit == null || EditingStockOut.Quantity <= 0)
                {
                    await Task.Run(() => MessageBox.Show("Укажите товар и количество."));
                    return;
                }

                Application.Current.Dispatcher.Invoke(() => IsBusy = true);

                try
                {
                    int stockInDb = await _repository.GetProductStockCountAsync(SelectedProductForEdit.ProductId);

                    int finalAvailableStock = stockInDb;

                    if (EditingStockOut.OutId != 0)
                    {
                        var originalRecord = StockOutsList.FirstOrDefault(x => x.OutId == EditingStockOut.OutId);
                        if (originalRecord != null)
                        {
                            finalAvailableStock += originalRecord.Quantity;
                        }
                    }

                    if (EditingStockOut.Quantity > finalAvailableStock)
                    {
                        await Task.Run(() => MessageBox.Show(
                            $"Недостаточно товара!{Environment.NewLine}" +
                            $"На складе фактически: {stockInDb}{Environment.NewLine}" +
                            $"Доступно для этой операции: {finalAvailableStock}{Environment.NewLine}" +
                            $"Вы пытаетесь списать: {EditingStockOut.Quantity}",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning));
                        return;
                    }

                    EditingStockOut.ProductId = SelectedProductForEdit.ProductId;

                    var result = EditingStockOut.OutId == 0
                        ? await _repository.AddStockOutAsync(EditingStockOut)
                        : await _repository.UpdateStockOutAsync(EditingStockOut);

                    if (result.IsSuccess)
                    {
                        await FilterStockOuts();
                        Application.Current.Dispatcher.Invoke(() => NewStockOut());
                        await Task.Run(() => MessageBox.Show("Готово!"));
                    }
                }
                finally
                {
                    Application.Current.Dispatcher.Invoke(() => IsBusy = false);
                }
            });
        }

        [RelayCommand(CanExecute = nameof(CanExecuteAction))]
        public Task DeleteStockOut()
        {
            return Task.Run(async () =>
            {
                if (SelectedStockOut == null)
                {
                    await Task.Run(() => MessageBox.Show("Выберите запись для удаления.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning));
                    return;
                }

                var confirm = MessageBoxResult.No;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    confirm = MessageBox.Show("Вы уверены, что хотите удалить эту запись расхода?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
                });

                if (confirm == MessageBoxResult.Yes)
                {
                    Application.Current.Dispatcher.Invoke(() => IsBusy = true);

                    try
                    {
                        var result = await _repository.DeleteStockOutAsync(SelectedStockOut.OutId);

                        if (result.IsSuccess)
                        {
                            await FilterStockOuts();

                            await Task.Run(() => MessageBox.Show("Запись расхода удалена.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information));
                        }
                        else
                        {
                            await ShowErrorMessage($"Ошибка удаления: {result.ErrorMessage}");
                        }
                    }
                    catch (Exception ex)
                    {
                        await ShowErrorMessage($"Критическая ошибка при удалении: {ex.Message}");
                    }
                    finally
                    {
                        Application.Current.Dispatcher.Invoke(() => IsBusy = false);
                    }
                }
            });
        }


        private async Task ShowErrorMessage(string message)
        {
            await Task.Run(() =>
                MessageBox.Show(message, "Ошибка БД", MessageBoxButton.OK, MessageBoxImage.Error)
            );
        }

        public void Dispose()
        {
            SqlRepository.ProductsUpdated -= OnProductsListChanged;
            base.Dispose();
        }
    }
}