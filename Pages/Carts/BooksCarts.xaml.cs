using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;
using UP_02.Models;

namespace UP_02.Pages.Carts
{
    public partial class BooksCarts : UserControl
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiUrl = "https://localhost:7000/api/ParsingBooks/database/books";
        private readonly string _favoritesApiUrl = "https://localhost:7000/api/Isbrannoe";

        private Book _currentBook;
        private int _currentUserId => SessionManager.CurrentUserId;

        public Book CurrentBook
        {
            get => _currentBook;
            set
            {
                _currentBook = value;
                UpdateBookDisplay();
            }
        }

        public BooksCarts()
        {
            InitializeComponent();

            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback =
                (message, cert, chain, errors) => true;

            _httpClient = new HttpClient(handler);
            _httpClient.Timeout = TimeSpan.FromSeconds(30);

            Loaded += BooksCarts_Loaded;
        }

        private async void BooksCarts_Loaded(object sender, RoutedEventArgs e)
        {
            if (CurrentBook == null)
            {
                await LoadBookData();
            }
            else
            {
                await CheckIfFavorite();
            }
        }

        public async Task LoadBookData()
        {
            try
            {
                Console.WriteLine($"Запрос к API: {_apiUrl}");

                if (!await IsServerAvailable())
                {
                    MessageBox.Show(
                        "API сервер не доступен.\n\n" +
                        "Проверьте:\n" +
                        "1. Запущен ли проект API_UP_02\n" +
                        "2. Порт 7000 (https://localhost:7000)\n" +
                        "3. SSL сертификат доверенный",
                        "Ошибка подключения",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                var response = await _httpClient.GetAsync(_apiUrl);
                Console.WriteLine($"Статус ответа: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Получено данных: {json.Length} байт");

                    if (string.IsNullOrWhiteSpace(json) || json == "[]")
                    {
                        MessageBox.Show(
                            "База данных пуста.\n\n" +
                            "Сначала выполните парсинг книг через API:\n" +
                            "GET /api/ParsingBooks/readli/books?count=20",
                            "Нет данных",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                        return;
                    }

                    var books = JsonConvert.DeserializeObject<List<Book>>(json);

                    if (books?.Any() == true)
                    {
                        Console.WriteLine($"Загружено книг: {books.Count}");
                        CurrentBook = books.First();
                        await CheckIfFavorite(); 
                    }
                    else
                    {
                        MessageBox.Show("Книги не найдены в базе данных");
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Ошибка API: {errorContent}");
                    MessageBox.Show(
                        $"Ошибка API: {response.StatusCode}\n\n" +
                        $"Детали: {errorContent}",
                        "Ошибка сервера",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            catch (HttpRequestException httpEx)
            {
                Console.WriteLine($"HTTP ошибка: {httpEx.Message}");
                MessageBox.Show(
                    $"Ошибка подключения к серверу:\n{httpEx.Message}\n\n" +
                    $"Проверьте:\n" +
                    $"1. Запущен ли API проект (API_UP_02)\n" +
                    $"2. Правильный ли порт (7000)\n" +
                    $"3. HTTPS сертификат доверенный\n\n" +
                    $"Попробуйте открыть в браузере:\n" +
                    $"https://localhost:7000/api/ParsingBooks/database/books",
                    "Ошибка сети",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (JsonException jsonEx)
            {
                Console.WriteLine($"JSON ошибка: {jsonEx.Message}");
                MessageBox.Show(
                    $"Ошибка обработки данных:\n{jsonEx.Message}",
                    "Ошибка данных",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Общая ошибка: {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"Внутренняя ошибка: {ex.InnerException.Message}");

                MessageBox.Show(
                    $"Произошла ошибка:\n{ex.Message}\n\n" +
                    $"См. консоль для деталей",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async Task<bool> IsServerAvailable()
        {
            try
            {
                var baseUri = new Uri("https://localhost:7000");
                using var testClient = new HttpClient();
                testClient.Timeout = TimeSpan.FromSeconds(5);

                var response = await testClient.GetAsync(baseUri);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void SetBookData(Book book)
        {
            CurrentBook = book;
        }

        private void UpdateBookDisplay()
        {
            if (CurrentBook == null) return;

            Dispatcher.Invoke(() =>
            {
                TitleText.Text = CurrentBook.Title ?? "Без названия";
                AuthorText.Text = CurrentBook.Author ?? "Неизвестен";
                GenreText.Text = CurrentBook.Genre ?? "Не указан";
                YearText.Text = CurrentBook.Year?.ToString() ?? "Не указан";
                LanguageText.Text = CurrentBook.Language ?? "Не указан";

                if (!string.IsNullOrEmpty(CurrentBook.ImageUrl))
                {
                    try
                    {
                        BookImage.Source = new BitmapImage(new Uri(CurrentBook.ImageUrl));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка загрузки изображения: {ex.Message}");
                        BookImage.Source = null;
                    }
                }
                else
                {
                    BookImage.Source = null;
                }
            });
        }

        public async Task RefreshData()
        {
            await LoadBookData();
        }


        private async Task CheckIfFavorite()
        {
            try
            {

                var response = await _httpClient.GetAsync($"{_favoritesApiUrl}/check?userId={_currentUserId}&bookId={CurrentBook.Id}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    dynamic result = JsonConvert.DeserializeObject(json);

                    bool isFavorite = result.isFavorite;

                    Dispatcher.Invoke(() =>
                    {
                        UpdateFavoriteButtonState(isFavorite);
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка проверки избранного: {ex.Message}");
            }
        }

        private void UpdateFavoriteButtonState(bool isFavorite)
        {
            if (isFavorite)
            {
                FavoriteButton.Content = "⭐ В избранном";
                FavoriteButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD966"));
                FavoriteButton.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C69500"));
                FavoriteAddedIndicator.Text = "✓ В избранном";
                FavoriteAddedIndicator.Visibility = Visibility.Visible;
            }
            else
            {
                FavoriteButton.Content = "⭐ В избранное";
                FavoriteButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF9E6"));
                FavoriteButton.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFC107"));
                FavoriteAddedIndicator.Visibility = Visibility.Collapsed;
            }
        }

        private async void FavoriteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentUserId == 0)
                {
                    MessageBox.Show("Необходимо авторизоваться", "Внимание",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (CurrentBook == null)
                {
                    MessageBox.Show("Книга не выбрана", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var checkResponse = await _httpClient.GetAsync($"{_favoritesApiUrl}/check?userId={_currentUserId}&bookId={CurrentBook.Id}");

                if (!checkResponse.IsSuccessStatusCode)
                {
                    MessageBox.Show("Ошибка проверки статуса", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var checkJson = await checkResponse.Content.ReadAsStringAsync();
                dynamic checkResult = JsonConvert.DeserializeObject(checkJson);
                bool isCurrentlyFavorite = checkResult.isFavorite;

                HttpResponseMessage response;

                if (isCurrentlyFavorite)
                {
                    response = await _httpClient.DeleteAsync($"{_favoritesApiUrl}/remove?userId={_currentUserId}&bookId={CurrentBook.Id}");

                    if (response.IsSuccessStatusCode)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            UpdateFavoriteButtonState(false);

                            FavoriteAddedIndicator.Text = "Удалено из избранного";
                            FavoriteAddedIndicator.Foreground = new SolidColorBrush(Colors.Red);
                            FavoriteAddedIndicator.Visibility = Visibility.Visible;

                        });
                    }
                }
                else
                {
                    response = await _httpClient.PostAsync($"{_favoritesApiUrl}/add?userId={_currentUserId}&bookId={CurrentBook.Id}", null);

                    if (response.IsSuccessStatusCode)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            UpdateFavoriteButtonState(true);

                            FavoriteAddedIndicator.Text = "Добавлено в избранное";
                            FavoriteAddedIndicator.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#28A745"));
                            FavoriteAddedIndicator.Visibility = Visibility.Visible;

                        });
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        MessageBox.Show($"Ошибка при добавлении: {error}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                MessageBox.Show($"Ошибка подключения к серверу: {ex.Message}",
                    "Ошибка сети", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла ошибка: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ReadButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentBook != null)
            {
                var parameters = new Dictionary<string, string>
        {
            { "bookId", CurrentBook.Id.ToString() },
            { "title", CurrentBook.Title ?? "" },
            { "author", CurrentBook.Author ?? "" },
            { "image", CurrentBook.ImageUrl ?? "" }
        };

                var readPage = new PageReadUsers();
                readPage.Tag = parameters;
                MainWindow.init.frame.Navigate(readPage);
            }
            else
            {
                MessageBox.Show("Книга не выбрана", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Border_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is Border border)
            {
                border.Background = new SolidColorBrush(Color.FromRgb(248, 249, 250));
                border.BorderBrush = new SolidColorBrush(Color.FromRgb(0, 123, 255));
            }
        }

        private void Border_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is Border border)
            {
                border.Background = Brushes.White;
                border.BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224));
            }
        }

        private void Button_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is Button btn)
            {
                if (btn == ReadButton)
                {
                    btn.Background = new SolidColorBrush(Color.FromRgb(0, 86, 179));
                }
                else if (btn == FavoriteButton)
                {
                    if (FavoriteButton.Content.ToString() == "В избранном")
                    {
                        btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0A800"));
                    }
                    else
                    {
                        btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFE9B3"));
                    }
                }
            }
        }

        private void Button_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is Button btn)
            {
                if (btn == ReadButton)
                {
                    btn.Background = new SolidColorBrush(Color.FromRgb(0, 123, 255));
                }
                else if (btn == FavoriteButton)
                {
                    if (FavoriteButton.Content.ToString() == "В избранном")
                    {
                        btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD966"));
                    }
                    else
                    {
                        btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF9E6"));
                    }
                }
            }
        }
    }
}