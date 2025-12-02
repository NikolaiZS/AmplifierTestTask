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
    public partial class StockOutsViewModel : ObservableObject, IDisposable
    {
        private readonly SqlRepository _repository;

        [ObservableProperty]
        private DateTime _filterStartDate = DateTime.Now.AddMonths(-1).Date;

        [ObservableProperty]
        private DateTime _filterEndDate = DateTime.Now.Date;

        [ObservableProperty]
        private ObservableCollection<StockOut> _stockOutsList;

        [ObservableProperty]
        private StockOut _selectedStockOut; 

        [ObservableProperty]
        private ObservableCollection<Product> _availableProducts; 

        [ObservableProperty]
        private StockOut _editingStockOut = new StockOut() { OutDate = DateTime.Now.Date }; 

        [ObservableProperty]
        private Product _selectedProductForEdit; 

        public StockOutsViewModel()
        {
            _repository = new SqlRepository();
            LoadInitialData();

            SqlRepository.ProductsUpdated -= OnProductsListChanged;
            SqlRepository.ProductsUpdated += OnProductsListChanged;
        }

        private void OnProductsListChanged()
        {
            LoadProductsForComboBox();
        }

        private void LoadProductsForComboBox()
        {
            AvailableProducts = new ObservableCollection<Product>(_repository.GetAllProducts());
        }
        public void Dispose()
        {
            SqlRepository.ProductsUpdated -= OnProductsListChanged;
        }

        private void LoadInitialData()
        {
            LoadProductsForComboBox();

            FilterStockOuts();
        }

        [RelayCommand]
        public void FilterStockOuts()
        {
            try
            {
                var filtered = _repository.GetFilteredStockOuts(FilterStartDate, FilterEndDate);
                StockOutsList = new ObservableCollection<StockOut>(filtered);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка фильтрации данных: {ex.Message}", "Ошибка БД", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

        [RelayCommand]
        public void SaveStockOut()
        {
            if (SelectedProductForEdit == null || EditingStockOut.Quantity <= 0)
            {
                MessageBox.Show("Пожалуйста, выберите товар и введите корректное количество (> 0).", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            EditingStockOut.ProductId = SelectedProductForEdit.ProductId;

            try
            {
                if (EditingStockOut.OutId == 0)
                {
                    _repository.AddStockOut(EditingStockOut);
                }
                else
                {
                    _repository.UpdateStockOut(EditingStockOut);
                }

                FilterStockOuts();
                NewStockOut();
                MessageBox.Show("Запись расхода успешно сохранена.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения расхода: {ex.Message}", "Ошибка БД", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        public void DeleteStockOut()
        {
            if (SelectedStockOut == null)
            {
                MessageBox.Show("Выберите запись для удаления.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show("Вы уверены, что хотите удалить эту запись расхода?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _repository.DeleteStockOut(SelectedStockOut.OutId);
                    FilterStockOuts();
                    MessageBox.Show("Запись расхода удалена.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления расхода: {ex.Message}", "Ошибка БД", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}