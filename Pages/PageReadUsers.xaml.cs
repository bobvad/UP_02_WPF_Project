using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
        private int currentUserId;

        private List<string> pages = new List<string>();
        private int currentPageIndex = 0;
        private const double PageHeight = 800;
        private bool isPageChanging = false;

        private int totalPages = 0;
        private int savedCurrentPage = 1;
        private bool isProgressLoaded = false;
        private System.Threading.Timer saveProgressTimer;
        private const int SaveDelayMs = 2000; 

        public class BookContentResponse
        {
            public int BookId { get; set; }
            public string Content { get; set; }
            public string Error { get; set; }
        }

        public class ReadingProgressResponse
        {
            public int Id { get; set; }
            public int UserId { get; set; }
            public int BookId { get; set; }
            public string Status { get; set; }
            public DateTime? StartDate { get; set; }
            public DateTime? FinishDate { get; set; }
            public int? CurrentPage { get; set; }
            public Book Book { get; set; }
        }

        public PageReadUsers()
        {
            InitializeComponent();

            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

            client = new HttpClient(handler);
            client.BaseAddress = new Uri("https://localhost:7000/");
            client.Timeout = TimeSpan.FromSeconds(30);

            this.Loaded += Page_Loaded;
            this.Unloaded += Page_Unloaded;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Focus();

                currentUserId = Models.SessionManager.CurrentUserId;

                if (currentUserId == 0)
                {
                    MessageBox.Show("Не удалось определить пользователя. Пожалуйста, войдите в систему.");
                    NavigationService?.GoBack();
                    return;
                }

                if (this.Tag != null)
                {
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
                    else if (this.Tag is Book book)
                    {
                        currentBook = book;
                        bookId = book.Id;
                        bookTitle = book.Title;
                        bookAuthor = book.Author;
                        bookImage = book.ImageUrl;

                        if (!string.IsNullOrEmpty(book.Content))
                        {
                            string cleanedText = CleanBookText(book.Content);
                            ShowBookContent(cleanedText);
                            SplitIntoPages(cleanedText);

                            // Загружаем прогресс после разбиения на страницы
                            LoadReadingProgress();
                            return;
                        }
                    }
                }

                if (bookId == 0)
                {
                    LoadingPanel.Visibility = Visibility.Collapsed;
                    ContentGrid.Visibility = Visibility.Visible;
                    BookContentText.Text = "Не указан ID книги";
                    UpdateNavigationButtons();
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

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            // Сохраняем прогресс при выходе со страницы
            SaveProgressImmediately();
            saveProgressTimer?.Dispose();
        }

        private string CleanBookText(string rawText)
        {
            if (string.IsNullOrEmpty(rawText))
                return rawText;

            string cleaned = rawText;

            cleaned = Regex.Replace(cleaned, @"^\s*\{\s*""bookId""\s*:\s*\d+\s*,\s*""content""\s*:\s*""(.*)""\s*\}\s*$", "$1", RegexOptions.Singleline);
            cleaned = Regex.Replace(cleaned, @"^\s*\[\s*""bookid""\s*:\s*\d+\s*,\s*""content""\s*:\s*""(.*)""\s*\]\s*$", "$1", RegexOptions.Singleline);

            cleaned = Regex.Replace(cleaned, @"\[""bookid"":\d+,\s*""content"":""", "");
            cleaned = Regex.Replace(cleaned, @"\{""bookId"":\d+,\s*""content"":""", "");

            cleaned = Regex.Replace(cleaned, @"\\u[0-9a-fA-F]{4}", "");

            cleaned = Regex.Replace(cleaned, @"\\u[oO0][a-fA-F0-9]{2}[a-fA-F0-9]", "");
            cleaned = Regex.Replace(cleaned, @"\\u[oO0][a-fA-F0-9]{3}", "");

            cleaned = cleaned.Replace("\\\"", "\"");
            cleaned = cleaned.Replace("\\\\", "\\");

            cleaned = cleaned.Replace("\\n", "\n");
            cleaned = cleaned.Replace("\\r\\n", "\n");
            cleaned = cleaned.Replace("\\r", "\n");

            cleaned = cleaned.Trim();
            if (cleaned.StartsWith("\"") && cleaned.EndsWith("\""))
            {
                cleaned = cleaned.Substring(1, cleaned.Length - 2);
            }

            cleaned = Regex.Replace(cleaned, @"–+\\+u+[oO0]+[a-fA-F0-9]+[a-fA-F0-9]?", "");

            cleaned = cleaned.Replace("–", "-");
            cleaned = cleaned.Replace("«", "\"");
            cleaned = cleaned.Replace("»", "\"");

            cleaned = cleaned.TrimStart('\n');
            cleaned = cleaned.TrimEnd('\n');

            cleaned = FormatParagraphs(cleaned);

            return cleaned;
        }

        private string FormatParagraphs(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            string[] lines = text.Split(new[] { "\n" }, StringSplitOptions.None);
            var formattedLines = new List<string>();

            string currentParagraph = "";
            bool inParagraph = false;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();

                if (string.IsNullOrWhiteSpace(line) && !inParagraph)
                    continue;

                if (string.IsNullOrWhiteSpace(line))
                {
                    if (inParagraph)
                    {
                        if (!string.IsNullOrWhiteSpace(currentParagraph))
                        {
                            formattedLines.Add(currentParagraph.Trim());
                            formattedLines.Add("");
                        }
                        currentParagraph = "";
                        inParagraph = false;
                    }
                    continue;
                }

                if (IsChapterHeader(line))
                {
                    if (!string.IsNullOrWhiteSpace(currentParagraph))
                    {
                        formattedLines.Add(currentParagraph.Trim());
                        formattedLines.Add("");
                        currentParagraph = "";
                    }

                    formattedLines.Add(line);
                    formattedLines.Add("");
                    inParagraph = false;
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(currentParagraph))
                {
                    currentParagraph += " " + line;
                }
                else
                {
                    currentParagraph = line;
                    inParagraph = true;
                }
            }

            if (!string.IsNullOrWhiteSpace(currentParagraph))
            {
                formattedLines.Add(currentParagraph.Trim());
            }

            var result = new List<string>();
            bool lastWasEmpty = false;

            foreach (string line in formattedLines)
            {
                bool isEmpty = string.IsNullOrEmpty(line);

                if (!isEmpty || !lastWasEmpty)
                {
                    result.Add(line);
                }

                lastWasEmpty = isEmpty;
            }
            if (result.Count > 0 && string.IsNullOrEmpty(result[result.Count - 1]))
            {
                result.RemoveAt(result.Count - 1);
            }

            return string.Join("\n", result);
        }

        private bool IsChapterHeader(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return false;

            string trimmed = line.Trim();

            return Regex.IsMatch(trimmed, @"^Глава\s+\d+", RegexOptions.IgnoreCase) ||
                   Regex.IsMatch(trimmed, @"^Chapter\s+\d+", RegexOptions.IgnoreCase) ||
                   Regex.IsMatch(trimmed, @"^\d+\..*") ||
                   (trimmed.Length < 50 && trimmed.Contains("Глава")) ||
                   (trimmed.Length < 50 && trimmed.Contains("Chapter")) ||
                   (trimmed.Length < 100 && trimmed == trimmed.ToUpper() && trimmed.Length > 3) ||
                   (trimmed.Length < 100 && trimmed.StartsWith("Часть")) ||
                   (trimmed.Length < 100 && trimmed.StartsWith("Эпилог")) ||
                   (trimmed.Length < 100 && trimmed.StartsWith("Пролог"));
        }

        private async void LoadBookContent()
        {
            try
            {
                ShowLoadingPanel();

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
                            string cleanedContent = CleanBookText(bookFromDb.Content);
                            ShowBookContent(cleanedContent);
                            SplitIntoPages(cleanedContent);

                            // Загружаем прогресс после разбиения на страницы
                            LoadReadingProgress();
                            return;
                        }
                    }
                    catch { }
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
                            string cleanedContent = CleanBookText(result.Content);
                            ShowBookContent(cleanedContent);
                            SplitIntoPages(cleanedContent);
                            await SaveContentToDatabase(bookId, cleanedContent);

                            // Загружаем прогресс после разбиения на страницы
                            LoadReadingProgress();
                        }
                        else
                        {
                            string cleanedContent = CleanBookText(json);
                            ShowBookContent(cleanedContent);
                            SplitIntoPages(cleanedContent);
                            LoadReadingProgress();
                        }
                    }
                    catch
                    {
                        string cleanedContent = CleanBookText(json);
                        ShowBookContent(cleanedContent);
                        SplitIntoPages(cleanedContent);
                        LoadReadingProgress();
                    }
                }
                else
                {
                    ShowBookContent($"Ошибка сервера: {response.StatusCode}");
                    SplitIntoPages($"Ошибка сервера: {response.StatusCode}");
                }
            }
            catch (HttpRequestException)
            {
                ShowBookContent("Не удалось подключиться к серверу");
                SplitIntoPages("Не удалось подключиться к серверу");
            }
            catch (Exception ex)
            {
                ShowBookContent($"Ошибка: {ex.Message}");
                SplitIntoPages($"Ошибка: {ex.Message}");
            }
        }

        private void ShowLoadingPanel()
        {
            LoadingPanel.Visibility = Visibility.Visible;
            ContentGrid.Visibility = Visibility.Collapsed;

            var stackPanel = LoadingPanel.Children[0] as StackPanel;
            if (stackPanel != null)
            {
                stackPanel.Children.Clear();
                stackPanel.Children.Add(new TextBlock
                {
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
            ContentGrid.Visibility = Visibility.Visible;
        }

        private void SplitIntoPages(string content)
        {
            pages.Clear();

            if (string.IsNullOrEmpty(content))
            {
                pages.Add(content);
                totalPages = 1;
                UpdateNavigationButtons();
                return;
            }

            string[] words = content.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            string currentPage = "";
            double currentHeight = 0;

            var measureBlock = new TextBlock
            {
                FontSize = BookContentText.FontSize,
                FontFamily = BookContentText.FontFamily,
                LineHeight = BookContentText.LineHeight,
                LineStackingStrategy = BookContentText.LineStackingStrategy,
                TextWrapping = TextWrapping.Wrap,
                Width = ContentBorder.ActualWidth > 0 ? ContentBorder.ActualWidth - 100 : 700
            };

            foreach (string word in words)
            {
                string testText = string.IsNullOrEmpty(currentPage) ? word : currentPage + " " + word;
                measureBlock.Text = testText;

                measureBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                measureBlock.Arrange(new Rect(measureBlock.DesiredSize));

                double testHeight = measureBlock.ActualHeight;

                if (testHeight > PageHeight && !string.IsNullOrEmpty(currentPage))
                {
                    pages.Add(currentPage);
                    currentPage = word;

                    measureBlock.Text = word;
                    measureBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    measureBlock.Arrange(new Rect(measureBlock.DesiredSize));
                    currentHeight = measureBlock.ActualHeight;
                }
                else
                {
                    currentPage = testText;
                    currentHeight = testHeight;
                }
            }

            if (!string.IsNullOrEmpty(currentPage))
            {
                pages.Add(currentPage);
            }

            if (pages.Count == 0)
            {
                pages.Add(content);
            }

            totalPages = pages.Count;
            currentPageIndex = 0;
            ShowCurrentPage();
            UpdateNavigationButtons();
        }

        /// <summary>
        /// Загружает прогресс чтения пользователя для этой книги
        /// </summary>
        private async void LoadReadingProgress()
        {
            try
            {
                if (currentUserId == 0 || bookId == 0)
                    return;

                string url = $"api/ReadingProgress/user/{currentUserId}/book/{bookId}";
                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    var progress = JsonSerializer.Deserialize<ReadingProgressResponse>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (progress != null && progress.CurrentPage.HasValue)
                    {
                        savedCurrentPage = progress.CurrentPage.Value;

                        // Проверяем, не закончена ли уже книга
                        if (progress.Status == "Прочитано")
                        {
                            // Книга уже прочитана, показываем последнюю страницу
                            currentPageIndex = Math.Min(savedCurrentPage - 1, totalPages - 1);
                            if (currentPageIndex < 0) currentPageIndex = 0;

                            // Показываем сообщение о том, что книга уже прочитана
                            Dispatcher.Invoke(() => {
                                MessageBox.Show("Вы уже завершили чтение этой книги ранее.", "Информация",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                            });
                        }
                        else if (progress.Status == "Читаю" || progress.Status == "Хочу прочитать")
                        {
                            // Восстанавливаем страницу, на которой остановились
                            currentPageIndex = Math.Min(savedCurrentPage - 1, totalPages - 1);
                            if (currentPageIndex < 0) currentPageIndex = 0;

                            // Если статус "Хочу прочитать", обновляем на "Читаю"
                            if (progress.Status == "Хочу прочитать")
                            {
                                await StartReading();
                            }
                        }

                        ShowCurrentPage();
                    }
                    else
                    {
                        // Нет прогресса - начинаем чтение
                        await StartReading();
                    }
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    // Прогресс не найден - начинаем чтение
                    await StartReading();
                }

                isProgressLoaded = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки прогресса: {ex.Message}");
                isProgressLoaded = true;
            }
        }

        /// <summary>
        /// Начинает чтение книги (создает или обновляет прогресс)
        /// </summary>
        private async System.Threading.Tasks.Task StartReading()
        {
            try
            {
                string url = $"api/ReadingProgress/start-reading?userId={currentUserId}&bookId={bookId}";
                var response = await client.PutAsync(url, null);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    var progress = JsonSerializer.Deserialize<ReadingProgressResponse>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (progress != null && progress.CurrentPage.HasValue)
                    {
                        savedCurrentPage = progress.CurrentPage.Value;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка начала чтения: {ex.Message}");
            }
        }

        /// <summary>
        /// Сохраняет текущую страницу
        /// </summary>
        private async void SaveCurrentPageProgress()
        {
            if (!isProgressLoaded || currentUserId == 0 || bookId == 0)
                return;

            try
            {
                // Текущая страница (индекс + 1, так как страницы считаются с 1)
                int currentPage = currentPageIndex + 1;

                // Не сохраняем, если страница не изменилась
                if (currentPage == savedCurrentPage)
                    return;

                string url = $"api/ReadingProgress/update-page?userId={currentUserId}&bookId={bookId}&page={currentPage}";
                var response = await client.PutAsync(url, null);

                if (response.IsSuccessStatusCode)
                {
                    savedCurrentPage = currentPage;
                    Console.WriteLine($"Прогресс сохранен: страница {currentPage}");

                    // Проверяем, не дочитали ли книгу
                    if (currentPage >= totalPages)
                    {
                        // Книга дочитана, показываем сообщение
                        Dispatcher.Invoke(() => {
                            MessageBox.Show("Поздравляем! Вы дочитали книгу до конца!",
                                "Книга завершена", MessageBoxButton.OK, MessageBoxImage.Information);
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сохранения прогресса: {ex.Message}");
            }
        }

        /// <summary>
        /// Немедленное сохранение прогресса (при выходе)
        /// </summary>
        private async void SaveProgressImmediately()
        {
            if (saveProgressTimer != null)
            {
                saveProgressTimer.Dispose();
                saveProgressTimer = null;
            }

            SaveCurrentPageProgress();

            // Даем время на сохранение
            await System.Threading.Tasks.Task.Delay(500);
        }

        /// <summary>
        /// Показывает текущую страницу
        /// </summary>
        private void ShowCurrentPage()
        {
            if (pages.Count > 0 && currentPageIndex >= 0 && currentPageIndex < pages.Count)
            {
                BookContentText.Text = pages[currentPageIndex];
                CurrentPageText.Text = (currentPageIndex + 1).ToString();

                ContentScrollViewer?.ScrollToTop();

                // Запускаем таймер для сохранения прогресса (с задержкой)
                if (isProgressLoaded)
                {
                    saveProgressTimer?.Dispose();
                    saveProgressTimer = new System.Threading.Timer(_ =>
                    {
                        Dispatcher.Invoke(() => SaveCurrentPageProgress());
                    }, null, SaveDelayMs, System.Threading.Timeout.Infinite);
                }
            }
        }

        /// <summary>
        /// Обновляет состояние кнопок навигации
        /// </summary>
        private void UpdateNavigationButtons()
        {
            TotalPagesText.Text = pages.Count.ToString();

            PrevPageButton.IsEnabled = currentPageIndex > 0;
            NextPageButton.IsEnabled = currentPageIndex < pages.Count - 1;

            if (pages.Count > 0)
            {
                int percent = (int)((double)(currentPageIndex + 1) / pages.Count * 100);
            }
        }

        /// <summary>
        /// Переход на предыдущую страницу
        /// </summary>
        private void GoToPreviousPage()
        {
            if (currentPageIndex > 0)
            {
                currentPageIndex--;
                ShowCurrentPage();
                UpdateNavigationButtons();
            }
        }

        /// <summary>
        /// Переход на следующую страницу
        /// </summary>
        private void GoToNextPage()
        {
            if (currentPageIndex < pages.Count - 1)
            {
                currentPageIndex++;
                ShowCurrentPage();
                UpdateNavigationButtons();

                // Если дошли до последней страницы, показываем поздравление
                if (currentPageIndex == pages.Count - 1)
                {
                    // Поздравление покажется при сохранении прогресса
                }
            }
        }

        private void PrevPageButton_Click(object sender, RoutedEventArgs e)
        {
            GoToPreviousPage();
        }

        private void NextPageButton_Click(object sender, RoutedEventArgs e)
        {
            GoToNextPage();
        }

        private void Page_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Left:
                case Key.A:
                    GoToPreviousPage();
                    e.Handled = true;
                    break;
                case Key.Right:
                case Key.D:
                    GoToNextPage();
                    e.Handled = true;
                    break;
                case Key.Space:
                    GoToNextPage();
                    e.Handled = true;
                    break;
            }
        }

        private void ContentScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (!isPageChanging && ContentScrollViewer.VerticalOffset >= ContentScrollViewer.ScrollableHeight - 50)
            {
                // Автоматический переход на следующую страницу при прокрутке вниз
                // Можно раскомментировать, если нужно
                // GoToNextPage();
            }
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
            SaveProgressImmediately();

            if (NavigationService != null && NavigationService.CanGoBack)
                NavigationService.GoBack();
            else
                NavigationService?.Navigate(new MainPage());
        }
    }
}