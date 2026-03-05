using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
        private string apiUrl = "https://localhost:7000/api/ParsingBooks/database/books"; 

        public MainPage()
        {
            InitializeComponent();

            Loaded += (s, e) => LoadBooks();
            RefreshBtn.Click += RefreshBtn_Click;
            SearchBox.TextChanged += SearchBox_TextChanged;
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
                MessageBox.Show($"Ошибка загрузки книг: {ex.Message}\n\nПроверьте подключение к серверу по адресу {apiUrl}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    client.Timeout = TimeSpan.FromSeconds(30); 

                    var response = await client.GetAsync(apiUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();

                        allBooks = JsonConvert.DeserializeObject<List<Book>>(json) ?? new List<Book>();

                        Console.WriteLine($"Загружено книг: {allBooks.Count}");
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        throw new Exception($"Сервер вернул ошибку: {response.StatusCode}\n{errorContent}");
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Не удалось подключиться к серверу. Убедитесь, что API запущен по адресу {apiUrl}", ex);
            }
            catch (JsonException ex)
            {
                throw new Exception($"Ошибка при обработке данных от сервера", ex);
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
                try
                {
                    var card = new BooksCarts();

                    if (string.IsNullOrEmpty(book.Title))
                    {
                        book.Title = "Без названия";
                    }

                    if (string.IsNullOrEmpty(book.Author))
                    {
                        book.Author = "Неизвестен";
                    }

                    card.SetBookData(book);
                    card.Margin = new Thickness(10);
                    card.Width = 280;

                    BooksPanel.Children.Add(card);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при создании карточки книги: {ex.Message}");
                }
            }
        }

        private List<Book> FilterBooks()
        {
            if (allBooks == null || allBooks.Count == 0)
                return new List<Book>();

            var result = allBooks.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                string search = SearchBox.Text.ToLower().Trim();
                result = result.Where(b =>
                    (b.Title?.ToLower().Contains(search) ?? false) ||
                    (b.Author?.ToLower().Contains(search) ?? false) ||
                    (b.Genre?.ToLower().Contains(search) ?? false)
                );
            }

            return result.ToList();
        }

        private void UpdateBooksCount()
        {
            int total = allBooks?.Count ?? 0;
            int shown = FilterBooks().Count;
            BooksCountText.Text = $"Всего книг: {total} (показано: {shown})";
        }

        private void ShowLoading(bool show)
        {
            if (LoadingGrid != null)
                LoadingGrid.Visibility = show ? Visibility.Visible : Visibility.Collapsed;

            if (BooksScrollViewer != null)
                BooksScrollViewer.Visibility = show ? Visibility.Collapsed : Visibility.Visible;
        }

        private void ShowNoBooks(bool show)
        {
            if (NoBooksGrid != null)
                NoBooksGrid.Visibility = show ? Visibility.Visible : Visibility.Collapsed;

            if (BooksScrollViewer != null && !show)
                BooksScrollViewer.Visibility = Visibility.Visible;
        }

        private void RefreshBtn_Click(object sender, RoutedEventArgs e)
        {
            LoadBooks();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (allBooks != null && allBooks.Count > 0)
            {
                ShowFilteredBooks();
                UpdateBooksCount();
            }
        }

        private void StatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (allBooks != null && allBooks.Count > 0)
            {
                ShowFilteredBooks();
                UpdateBooksCount();
            }
        }

        private void LanguageFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (allBooks != null && allBooks.Count > 0)
            {
                ShowFilteredBooks();
                UpdateBooksCount();
            }
        }

        private void GoToFavorites_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.init.frame.Navigate(new Pages.Favorites());
        }

        private void GoToAIButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.init.frame.Navigate(new Pages.RecomendationAI());
        }
    }
}