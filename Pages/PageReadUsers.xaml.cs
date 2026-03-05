using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

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

        public class BookContentResponse
        {
            public int BookId { get; set; }
            public string Content { get; set; }
            public string Error { get; set; }
        }

        public class Book
        {
            public int Id { get; set; }
            public string Title { get; set; }
            public string Author { get; set; }
            public string Content { get; set; }
            public string ImageUrl { get; set; }
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
                if (this.Tag != null && this.Tag is Dictionary<string, string> dict)
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

                if (bookId == 0)
                {
                    LoadingPanel.Visibility = Visibility.Collapsed;
                    ContentScrollViewer.Visibility = Visibility.Visible;
                    BookContentText.Text = "Не указан ID книги";
                    return;
                }

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

                string url = $"api/ParsingBooks/book/{bookId}/content";
                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();

                    try
                    {
                        var result = JsonSerializer.Deserialize<BookContentResponse>(json);

                        if (result != null && !string.IsNullOrEmpty(result.Content) && result.Content != "Текст книги не найден")
                        {
                            BookContentText.Text = result.Content;
                        }
                        else
                        {
                            BookContentText.Text = "Текст книги не найден";
                        }
                    }
                    catch
                    {
                        BookContentText.Text = json;
                    }
                }
                else
                {
                    BookContentText.Text = $"Ошибка сервера: {response.StatusCode}";
                }

                LoadingPanel.Visibility = Visibility.Collapsed;
                ContentScrollViewer.Visibility = Visibility.Visible;
            }
            catch (HttpRequestException)
            {
                LoadingPanel.Visibility = Visibility.Collapsed;
                ContentScrollViewer.Visibility = Visibility.Visible;
                BookContentText.Text = "Не удалось подключиться к серверу";
            }
            catch (Exception ex)
            {
                LoadingPanel.Visibility = Visibility.Collapsed;
                ContentScrollViewer.Visibility = Visibility.Visible;
                BookContentText.Text = $"Ошибка: {ex.Message}";
            }
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