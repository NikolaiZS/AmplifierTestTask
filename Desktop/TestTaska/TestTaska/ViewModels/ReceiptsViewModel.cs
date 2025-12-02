using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using TestTaska.Data;
using TestTaska.Models;


namespace TestTaska.ViewModels
{
    public partial class ReceiptsViewModel : ObservableObject, IDisposable
    {
        private readonly SqlRepository _repository;

        [ObservableProperty]
        private DateTime _filterStartDate = DateTime.Now.AddMonths(-1).Date;

        [ObservableProperty]
        private DateTime _filterEndDate = DateTime.Now.Date;

        [ObservableProperty]
        private ObservableCollection<StockReceipt> _receiptsList;

        [ObservableProperty]
        private StockReceipt _selectedReceipt;

        [ObservableProperty]
        private ObservableCollection<Product> _availableProducts;

        [ObservableProperty]
        private StockReceipt _editingReceipt = new StockReceipt() { ReceiptDate = DateTime.Now.Date };

        [ObservableProperty]
        private Product _selectedProductForEdit;

        public ReceiptsViewModel()
        {
            _repository = new SqlRepository();

            SqlRepository.ProductsUpdated -= OnProductsListChanged;
            SqlRepository.ProductsUpdated += OnProductsListChanged;
            LoadInitialData();
        }

        private void OnProductsListChanged()
        {
            LoadProductsForComboBox();
        }

        private void LoadProductsForComboBox()
        {
            AvailableProducts = new ObservableCollection<Product>(_repository.GetAllProducts());
        }

        private void LoadInitialData()
        {
            LoadProductsForComboBox();
            FilterReceipts();
        }

        public void Dispose()
        {
            SqlRepository.ProductsUpdated -= OnProductsListChanged;
        }

        [RelayCommand]
        public void FilterReceipts()
        {
            try
            {
                var filtered = _repository.GetFilteredReceipts(FilterStartDate, FilterEndDate);
                ReceiptsList = new ObservableCollection<StockReceipt>(filtered);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка фильтрации данных: {ex.Message}", "Ошибка БД", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
        public void SaveReceipt()
        {
            if (SelectedProductForEdit == null || EditingReceipt.Quantity <= 0)
            {
                MessageBox.Show("Пожалуйста, выберите товар и введите корректное количество (> 0).", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            EditingReceipt.ProductId = SelectedProductForEdit.ProductId;

            try
            {
                if (EditingReceipt.ReceiptId == 0)
                {
                    _repository.AddReceipt(EditingReceipt);
                }
                else
                {
                    _repository.UpdateReceipt(EditingReceipt);
                }

                FilterReceipts();
                NewReceipt();
                MessageBox.Show("Запись успешно сохранена.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка БД", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        public void DeleteReceipt()
        {
            if (SelectedReceipt == null)
            {
                MessageBox.Show("Выберите запись для удаления.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show("Вы уверены, что хотите удалить эту запись?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _repository.DeleteReceipt(SelectedReceipt.ReceiptId);
                    FilterReceipts();
                    MessageBox.Show("Запись удалена.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка БД", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}