using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using TestTaska.Data;
using TestTaska.Models;

namespace TestTaska.ViewModels
{
    public partial class MainViewModel: ObservableObject, IDisposable
    {
        private readonly SqlRepository _repository;

        [ObservableProperty]
        private ObservableCollection<ProductStockDisplay> _stockList = new ObservableCollection<ProductStockDisplay>();

        [ObservableProperty]
        private string _newProductName = string.Empty;

        public MainViewModel()
        {
            _repository = new SqlRepository();
            LoadData();

            SqlRepository.StockMovementUpdated -= OnStockMovementChanged;
            SqlRepository.StockMovementUpdated += OnStockMovementChanged;
        }

        private void OnStockMovementChanged()
        {
            LoadData();
        }

        public void Dispose()
        {
            SqlRepository.StockMovementUpdated -= OnStockMovementChanged;
        }

        private void LoadData()
        {
            var stocks = _repository.GetCurrentStock();
            StockList.Clear();
            foreach (var stockItem in stocks)
            {
                StockList.Add(stockItem);
            }
        }

        [RelayCommand]
        private void AddProduct()
        {
            if (string.IsNullOrWhiteSpace(NewProductName))
            {
                MessageBox.Show("Наименование товара не может быть пустым.", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_repository.CheckDuplicateProduct(NewProductName))
            {
                MessageBox.Show($"Товар с наименованием '{NewProductName}' уже существует!", "Дублирование товара", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                _repository.AddProduct(NewProductName);

                NewProductName = string.Empty;
                LoadData();

                MessageBox.Show("Товар успешно добавлен.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении товара: {ex.Message}", "Ошибка БД", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
