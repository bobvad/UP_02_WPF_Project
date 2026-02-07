using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Newtonsoft.Json;
using UP_02.Models;
using UP_02.Pages.Carts;

namespace UP_02.Pages
{
    /// <summary>
    /// Логика взаимодействия для MainPage.xaml
    /// </summary>
    public partial class MainPage : Page
    {
        private List<Book> allBooks = new List<Book>();
        private string apiUrl = "https://localhost:7000/api/ParsingBooks/books";

        public MainPage()
        {
            InitializeComponent();
            LoadBooks();
        }

        private async void LoadBooks()
        {
            ShowLoading(true);
            ShowNoBooks(false);
            BooksPanel.Children.Clear();

            try
            {
                await LoadBooksFromApi();
                ShowFilteredBooks();
                UpdateBooksCount();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
            finally
            {
                ShowLoading(false);
            }
        }

        private async Task LoadBooksFromApi()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync(apiUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        allBooks = JsonConvert.DeserializeObject<List<Book>>(json);
                    }
                    else
                    {
                        allBooks = GetTestBooks();
                    }
                }
            }
            catch
            {
                allBooks = GetTestBooks();
            }
        }

        private void ShowFilteredBooks()
        {
            BooksPanel.Children.Clear();

            var filteredBooks = FilterBooks();

            if (filteredBooks.Count == 0)
            {
                ShowNoBooks(true);
                return;
            }

            ShowNoBooks(false);

            foreach (var book in filteredBooks)
            {
                var card = new BooksCarts();
                card.SetBookData(book);
                card.Margin = new Thickness(10);
                card.Width = 280;
                BooksPanel.Children.Add(card);
            }
        }

        private List<Book> FilterBooks()
        {
            var result = allBooks;

            if (!string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                string search = SearchBox.Text.ToLower();
                result = result.Where(b =>
                    (b.Title?.ToLower().Contains(search) ?? false) ||
                    (b.Author?.ToLower().Contains(search) ?? false)
                ).ToList();
            }

            if (StatusFilter.SelectedIndex == 1)
            {
                result = result.Where(b => b.IsCompleted).ToList();
            }
            else if (StatusFilter.SelectedIndex == 2)
            {
                result = result.Where(b => !b.IsCompleted).ToList();
            }

            if (LanguageFilter.SelectedIndex == 1)
            {
                result = result.Where(b => b.Language == "Русский").ToList();
            }
            else if (LanguageFilter.SelectedIndex == 2)
            {
                result = result.Where(b => b.Language == "Английский").ToList();
            }

            return result;
        }

        private List<Book> GetTestBooks()
        {
            return new List<Book>
            {
                new Book
                {
                    Title = "Моя сводная Тыковка",
                    Author = "Коротаева Ольга",
                    ImageUrl = "https://litmir.club/data/Book/0/960000/960417/BC4_1770025276.jpg",
                    Description = "Автор: Коротаева Ольга\nЖанр: Современные любовные романы",
                    Language = "Русский",
                    PageCount = 7,
                    IsCompleted = false
                },
                new Book
                {
                    Title = "Преступление и наказание",
                    Author = "Фёдор Достоевский",
                    ImageUrl = "https://example.com/book2.jpg",
                    Description = "Классический роман",
                    Language = "Русский",
                    PageCount = 672,
                    IsCompleted = true
                }
            };
        }

        private void UpdateBooksCount()
        {
            int total = allBooks.Count;
            int shown = FilterBooks().Count;
            BooksCountText.Text = $"Всего: {total}, Показано: {shown}";
        }

        private void ShowLoading(bool show)
        {
            LoadingGrid.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            BooksScrollViewer.Visibility = show ? Visibility.Collapsed : Visibility.Visible;
        }

        private void ShowNoBooks(bool show)
        {
            NoBooksGrid.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            BooksScrollViewer.Visibility = show ? Visibility.Collapsed : Visibility.Visible;
        }

        private void RefreshBtn_Click(object sender, RoutedEventArgs e)
        {
            LoadBooks();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ShowFilteredBooks();
            UpdateBooksCount();
        }

        private void StatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ShowFilteredBooks();
            UpdateBooksCount();
        }

        private void LanguageFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ShowFilteredBooks();
            UpdateBooksCount();
        }
    }
}
