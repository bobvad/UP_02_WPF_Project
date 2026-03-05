using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using UP_02.Models;

namespace UP_02.Pages
{
    public partial class PageReadUsers : Page
    {
        private HttpClient client;
        private int currentFontSize = 16;
        private int bookId;
        private string bookTitle;
        private string bookAuthor;
        private string bookImage;
        private Book currentBook;

        public class BookContentResponse
        {
            public int BookId { get; set; }
            public string Content { get; set; }
            public string Error { get; set; }
        }

        public PageReadUsers()
        {
            InitializeComponent();

            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

            client = new HttpClient(handler);
            client.BaseAddress = new Uri("https://localhost:7000/");
            client.Timeout = TimeSpan.FromSeconds(30);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Проверяем разные способы передачи данных
                if (this.Tag != null)
                {
                    // Вариант 1: передан словарь параметров
                    if (this.Tag is Dictionary<string, string> dict)
                    {
                        if (dict.ContainsKey("bookId"))
                            int.TryParse(dict["bookId"], out bookId);

                        if (dict.ContainsKey("title"))
                            bookTitle = dict["title"];

                        if (dict.ContainsKey("author"))
                            bookAuthor = dict["author"];

                        if (dict.ContainsKey("image"))
                            bookImage = dict["image"];
                    }
                    // Вариант 2: передан объект Book напрямую
                    else if (this.Tag is Book book)
                    {
                        currentBook = book;
                        bookId = book.Id;
                        bookTitle = book.Title;
                        bookAuthor = book.Author;
                        bookImage = book.ImageUrl;

                        // Если у книги уже есть контент, показываем его сразу
                        if (!string.IsNullOrEmpty(book.Content))
                        {
                            ShowBookContent(book.Content);
                            return;
                        }
                    }
                }

                if (bookId == 0)
                {
                    LoadingPanel.Visibility = Visibility.Collapsed;
                    ContentScrollViewer.Visibility = Visibility.Visible;
                    BookContentText.Text = "Не указан ID книги";
                    return;
                }

                // Отображаем информацию о книге
                BookTitleText.Text = !string.IsNullOrEmpty(bookTitle) ? bookTitle : "Загрузка...";
                BookAuthorText.Text = bookAuthor ?? "";

                if (!string.IsNullOrEmpty(bookImage))
                {
                    try
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(bookImage);
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        BookCoverImageSmall.Source = bitmap;
                    }
                    catch { }
                }

                // Загружаем контент
                LoadBookContent();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}");
            }
        }

        private async void LoadBookContent()
        {
            try
            {
                ShowLoadingPanel();

                // Сначала пробуем получить книгу из базы данных
                string dbUrl = $"api/Books/{bookId}";
                var dbResponse = await client.GetAsync(dbUrl);

                if (dbResponse.IsSuccessStatusCode)
                {
                    string json = await dbResponse.Content.ReadAsStringAsync();
                    try
                    {
                        var bookFromDb = JsonSerializer.Deserialize<Book>(json);
                        if (bookFromDb != null && !string.IsNullOrEmpty(bookFromDb.Content))
                        {
                            ShowBookContent(bookFromDb.Content);
                            return;
                        }
                    }
                    catch { }
                }

                // Если в базе нет, пробуем получить через парсинг
                string url = $"api/ParsingBooks/book/{bookId}/content";
                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();

                    try
                    {
                        // Пробуем десериализовать как BookContentResponse
                        var result = JsonSerializer.Deserialize<BookContentResponse>(json);

                        if (result != null && !string.IsNullOrEmpty(result.Content) && result.Content != "Текст книги не найден")
                        {
                            ShowBookContent(result.Content);

                            // Сохраняем контент в базу данных
                            await SaveContentToDatabase(bookId, result.Content);
                        }
                        else
                        {
                            // Если не получилось, пробуем как обычную строку
                            ShowBookContent(json);
                        }
                    }
                    catch
                    {
                        // Если JSON не парсится, показываем как есть
                        ShowBookContent(json);
                    }
                }
                else
                {
                    ShowBookContent($"Ошибка сервера: {response.StatusCode}");
                }
            }
            catch (HttpRequestException)
            {
                ShowBookContent("Не удалось подключиться к серверу");
            }
            catch (Exception ex)
            {
                ShowBookContent($"Ошибка: {ex.Message}");
            }
        }

        private void ShowLoadingPanel()
        {
            LoadingPanel.Visibility = Visibility.Visible;
            ContentScrollViewer.Visibility = Visibility.Collapsed;

            var stackPanel = LoadingPanel.Children[0] as StackPanel;
            if (stackPanel != null)
            {
                stackPanel.Children.Clear();
                stackPanel.Children.Add(new TextBlock
                {
                    Text = "📖",
                    FontSize = 48,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#007ACC")),
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                stackPanel.Children.Add(new TextBlock
                {
                    Text = "Загрузка книги...",
                    FontSize = 18,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666666")),
                    Margin = new Thickness(0, 20, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                stackPanel.Children.Add(new ProgressBar
                {
                    Width = 200,
                    Height = 4,
                    Margin = new Thickness(0, 20, 0, 0),
                    IsIndeterminate = true,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#007ACC"))
                });
            }
        }

        private void ShowBookContent(string content)
        {
            BookContentText.Text = content;
            LoadingPanel.Visibility = Visibility.Collapsed;
            ContentScrollViewer.Visibility = Visibility.Visible;
        }

        private async System.Threading.Tasks.Task SaveContentToDatabase(int bookId, string content)
        {
            try
            {
                var updateModel = new { Content = content };
                string json = JsonSerializer.Serialize(updateModel);
                var httpContent = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                await client.PatchAsync($"api/Books/{bookId}", httpContent);
            }
            catch { }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService != null && NavigationService.CanGoBack)
                NavigationService.GoBack();
            else
                NavigationService?.Navigate(new MainPage());
        }
    }
}