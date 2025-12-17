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
    public partial class ReceiptsViewModel : ViewModelBase
    {
        private readonly SqlRepository _repository;

        [ObservableProperty]
        private DateTime _filterStartDate = DateTime.Now.AddMonths(-1).Date;

        [ObservableProperty]
        private DateTime _filterEndDate = DateTime.Now.Date;

        [ObservableProperty]
        private ObservableCollection<StockReceipt> _receiptsList = new();

        [ObservableProperty]
        private StockReceipt? _selectedReceipt;

        [ObservableProperty]
        private ObservableCollection<Product> _availableProducts = new();

        [ObservableProperty]
        private StockReceipt _editingReceipt = new() { ReceiptDate = DateTime.Now.Date, Quantity = 1 };

        [ObservableProperty]
        private Product? _selectedProductForEdit;

        [ObservableProperty]
        private bool _isBusy;

        private bool CanExecuteAction() => !IsBusy;

        public ReceiptsViewModel(SqlRepository repository) : base()
        {
            _repository = repository;

            SqlRepository.ProductsUpdated -= OnProductsListChanged;
            SqlRepository.ProductsUpdated += OnProductsListChanged;

            LoadInitialData();
        }

        private void OnProductsListChanged()
        {
            _ = LoadProductsForComboBox();
        }

        private async void LoadInitialData()
        {
            await LoadProductsForComboBox();
            await FilterReceipts();
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
        public async Task FilterReceipts()
        {
            if (IsBusy) return;
            IsBusy = true;

            var result = await _repository.GetFilteredReceiptsAsync(FilterStartDate, FilterEndDate);

            if (result.IsSuccess && result.Data != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ReceiptsList.Clear();
                    foreach (var receipt in result.Data)
                    {
                        ReceiptsList.Add(receipt);
                    }
                });
            }
            else
            {
                await ShowErrorMessage($"Ошибка при загрузке приходов: {result.ErrorMessage}");
            }

            IsBusy = false;
        }

        [RelayCommand]
        public void NewReceipt()
        {
            EditingReceipt = new StockReceipt { ReceiptDate = DateTime.Now.Date, Quantity = 1 };
            SelectedProductForEdit = null;
            SelectedReceipt = null;
        }

        [RelayCommand]
        public void EditSelectedReceipt()
        {
            if (SelectedReceipt == null)
            {
                MessageBox.Show("Выберите запись для редактирования.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            EditingReceipt = new StockReceipt
            {
                ReceiptId = SelectedReceipt.ReceiptId,
                ProductId = SelectedReceipt.ProductId,
                ReceiptDate = SelectedReceipt.ReceiptDate,
                Quantity = SelectedReceipt.Quantity
            };

            SelectedProductForEdit = AvailableProducts.FirstOrDefault(p => p.ProductId == SelectedReceipt.ProductId);
        }

        [RelayCommand]
        public async Task SaveReceipt()
        {
            if (SelectedProductForEdit == null || EditingReceipt.Quantity <= 0)
            {
                MessageBox.Show("Пожалуйста, выберите товар и введите корректное количество (> 0).", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsBusy = true;
            EditingReceipt.ProductId = SelectedProductForEdit.ProductId;

            OperationResult result;
            if (EditingReceipt.ReceiptId == 0)
            {
                result = await _repository.AddReceiptAsync(EditingReceipt);
            }
            else
            {
                result = await _repository.UpdateReceiptAsync(EditingReceipt);
            }

            if (result.IsSuccess)
            {
                await FilterReceipts();
                NewReceipt();
                MessageBox.Show("Запись успешно сохранена.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                await ShowErrorMessage($"Ошибка сохранения: {result.ErrorMessage}");
            }

            IsBusy = false;
        }

        [RelayCommand(CanExecute = nameof(CanExecuteAction))]
        public Task DeleteReceipt()
        {
            return Task.Run(async () =>
            {
                if (SelectedReceipt == null)
                {
                    await Task.Run(() => MessageBox.Show("Выберите запись для удаления."));
                    return;
                }

                Application.Current.Dispatcher.Invoke(() => IsBusy = true);

                try
                {
                    int currentStock = await _repository.GetProductStockCountAsync(SelectedReceipt.ProductId);

                    if (currentStock - SelectedReceipt.Quantity < 0)
                    {
                        await Task.Run(() => MessageBox.Show(
                            $"Невозможно удалить приход!{Environment.NewLine}{Environment.NewLine}" +
                            $"Этот товар уже частично или полностью списан (продан).{Environment.NewLine}" +
                            $"Текущий остаток: {currentStock}{Environment.NewLine}" +
                            $"В этом приходе: {SelectedReceipt.Quantity}{Environment.NewLine}{Environment.NewLine}" +
                            $"Сначала удалите расходы, связанные с этим товаром.",
                            "Ошибка целостности склада", MessageBoxButton.OK, MessageBoxImage.Stop));
                        return;
                    }

                    var confirm = MessageBoxResult.No;
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        confirm = MessageBox.Show("Вы уверены, что хотите удалить запись о приходе?",
                            "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    });

                    if (confirm == MessageBoxResult.Yes)
                    {
                        var result = await _repository.DeleteReceiptAsync(SelectedReceipt.ReceiptId);

                        if (result.IsSuccess)
                        {
                            await FilterReceipts();
                            await Task.Run(() => MessageBox.Show("Запись удалена."));
                        }
                        else
                        {
                            await Task.Run(() => MessageBox.Show(result.ErrorMessage));
                        }
                    }
                }
                catch (Exception ex)
                {
                    await Task.Run(() => MessageBox.Show($"Ошибка: {ex.Message}"));
                }
                finally
                {
                    Application.Current.Dispatcher.Invoke(() => IsBusy = false);
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