using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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
                        var list = new List<Models.FavoriteBook>();

                        foreach (var item in books.EnumerateArray())
                        {
                            var book = new Models.FavoriteBook
                            {
                                Id = item.GetProperty("favoriteId").GetInt32(),
                                BookId = item.GetProperty("bookId").GetInt32(),
                                Title = item.GetProperty("bookTitle").GetString(),
                                Author = item.GetProperty("bookAuthor").GetString(),
                                AddedDate = item.GetProperty("addedDate").GetString()
                            };

                            if (item.TryGetProperty("bookImage", out JsonElement img))
                                book.ImageUrl = img.GetString();

                            list.Add(book);
                        }

                        FavoritesList.ItemsSource = list;
                        FavoritesCountText.Text = GetWord(list.Count);
                        FavoritesList.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        EmptyStatePanel.Visibility = Visibility.Visible;
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

                if (FavoritesList.ItemsSource is List<Models.FavoriteBook> books)
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
            try
            {
                var btn = sender as Button;
                if (btn?.Tag == null) return;

                int id = (int)btn.Tag;

                if (FavoritesList.ItemsSource is List<Models.FavoriteBook> books)
                {
                    var book = books.FirstOrDefault(x => x.Id == id);
                    if (book == null) return;

                    var result = MessageBox.Show($"Удалить '{book.Title}' из избранного?", "Подтверждение",
                        MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        var response = await client.DeleteAsync($"api/Isbrannoe/remove?userId={currentUser}&bookId={book.BookId}");

                        if (response.IsSuccessStatusCode)
                        {
                            LoadFavorites();
                        }
                        else
                        {
                            MessageBox.Show("Не удалось удалить книгу", "Ошибка");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка");
            }
        }

        private void BookBorder_MouseEnter(object sender, MouseEventArgs e)
        {
        }

        private void BookBorder_MouseLeave(object sender, MouseEventArgs e)
        {
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

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}