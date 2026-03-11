using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using UP_02.Models;

namespace UP_02.Pages
{
    public partial class Favorites : Page
    {
        private HttpClient client;
        private int currentUser => SessionManager.CurrentUserId;

        public Favorites()
        {
            InitializeComponent();

            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

            client = new HttpClient(handler);
            client.BaseAddress = new Uri("https://localhost:7000/");
            client.Timeout = TimeSpan.FromSeconds(10);

            Loaded += (s, e) => LoadFavorites();
        }

        private async void LoadFavorites()
        {
            try
            {
                FavoritesList.ItemsSource = null;
                EmptyStatePanel.Visibility = Visibility.Collapsed;

                string url = $"api/Isbrannoe/user/{SessionManager.CurrentUserId}";
                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();

                    using JsonDocument doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("books", out JsonElement books) && books.GetArrayLength() > 0)
                    {
                        var list = new List<Book>();

                        foreach (var item in books.EnumerateArray())
                        {
                            var book = new Book
                            {
                                Id = item.GetProperty("bookId").GetInt32(),
                                Title = item.GetProperty("bookTitle").GetString(),
                                Author = item.GetProperty("bookAuthor").GetString()
                            };

                            if (item.TryGetProperty("bookImage", out JsonElement img))
                                book.ImageUrl = img.GetString();

                            // Добавляем дополнительные поля, если они есть в JSON
                            if (item.TryGetProperty("bookGenre", out JsonElement genre))
                                book.Genre = genre.GetString();

                            if (item.TryGetProperty("bookYear", out JsonElement year))
                                book.Year = year.GetInt32();

                            if (item.TryGetProperty("bookLanguage", out JsonElement language))
                                book.Language = language.GetString();

                            list.Add(book);
                        }

                        FavoritesList.ItemsSource = list;
                        FavoritesCountText.Text = GetWord(list.Count);
                        FavoritesList.Visibility = Visibility.Visible;
                        EmptyStatePanel.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        EmptyStatePanel.Visibility = Visibility.Visible;
                        FavoritesList.Visibility = Visibility.Collapsed;
                        FavoritesCountText.Text = "0 книг";
                    }
                }
                else
                {
                    string error = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Ошибка {response.StatusCode}: {error}", "Ошибка");
                    EmptyStatePanel.Visibility = Visibility.Visible;
                }
            }
            catch (HttpRequestException ex)
            {
                MessageBox.Show($"Сервер не доступен. Проверь запущен ли API по адресу https://localhost:7000/\n\nОшибка: {ex.Message}", "Ошибка");
                EmptyStatePanel.Visibility = Visibility.Visible;
            }
            catch (TaskCanceledException)
            {
                MessageBox.Show("Превышено время ожидания ответа от сервера", "Ошибка");
                EmptyStatePanel.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка");
                EmptyStatePanel.Visibility = Visibility.Visible;
            }
        }

        private string GetWord(int count)
        {
            if (count == 0) return "0 книг";
            if (count == 1) return "1 книга";
            if (count >= 2 && count <= 4) return $"{count} книги";
            return $"{count} книг";
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                var text = (sender as TextBox)?.Text?.ToLower() ?? "";

                if (FavoritesList.ItemsSource is List<Book> books)
                {
                    if (string.IsNullOrEmpty(text))
                    {
                        FavoritesList.ItemsSource = books;
                        FavoritesCountText.Text = GetWord(books.Count);
                    }
                    else
                    {
                        var filtered = books.Where(x =>
                            (x.Title?.ToLower().Contains(text) ?? false) ||
                            (x.Author?.ToLower().Contains(text) ?? false)).ToList();

                        FavoritesList.ItemsSource = filtered;
                        FavoritesCountText.Text = GetWord(filtered.Count);
                    }
                }
            }
            catch { }
        }

        private async void RemoveFromFavorites_Click(object sender, RoutedEventArgs e)
        {
            // Этот метод теперь обрабатывается в CartsFavorites
        }

        private void ReadBook_Click(object sender, RoutedEventArgs e)
        {
            // Этот метод теперь обрабатывается в CartsFavorites
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService != null && NavigationService.CanGoBack)
                NavigationService.GoBack();
            else
                NavigationService?.Navigate(new MainPage());
        }

        private void GoToCatalog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                NavigationService?.Navigate(new MainPage());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        // Публичный метод для обновления списка из UserControl
        public void RefreshFavorites()
        {
            LoadFavorites();
        }
    }
}