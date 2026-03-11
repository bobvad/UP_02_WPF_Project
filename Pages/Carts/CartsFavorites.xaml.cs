using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Input;
using UP_02.Models;

namespace UP_02.Pages.Carts
{
    public partial class CartsFavorites : UserControl
    {
        private readonly HttpClient _httpClient;
        private readonly string _favoritesApiUrl = "https://localhost:7000/api/Isbrannoe";

        private Book _currentBook;
        private int _currentUserId => SessionManager.CurrentUserId;
        private string _addedDate;

        // Событие для уведомления об удалении из избранного
        public event EventHandler FavoriteRemoved;

        public Book CurrentBook
        {
            get => _currentBook;
            set
            {
                _currentBook = value;
                UpdateBookDisplay();
            }
        }

        public string AddedDate
        {
            get => _addedDate;
            set
            {
                _addedDate = value;
            }
        }

        public CartsFavorites()
        {
            InitializeComponent();

            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback =
                (message, cert, chain, errors) => true;

            _httpClient = new HttpClient(handler);
            _httpClient.Timeout = TimeSpan.FromSeconds(30);

            AddAnimations();
        }

        private void AddAnimations()
        {
            var enterAnimation = new System.Windows.Media.Animation.Storyboard();
            var scaleXEnter = new System.Windows.Media.Animation.DoubleAnimation
            {
                To = 1.02,
                Duration = TimeSpan.FromMilliseconds(100)
            };
            var scaleYEnter = new System.Windows.Media.Animation.DoubleAnimation
            {
                To = 1.02,
                Duration = TimeSpan.FromMilliseconds(100)
            };
            System.Windows.Media.Animation.Storyboard.SetTargetProperty(scaleXEnter, new PropertyPath("RenderTransform.ScaleX"));
            System.Windows.Media.Animation.Storyboard.SetTargetProperty(scaleYEnter, new PropertyPath("RenderTransform.ScaleY"));
            enterAnimation.Children.Add(scaleXEnter);
            enterAnimation.Children.Add(scaleYEnter);
            Resources.Add("MouseEnterAnimation", enterAnimation);

            var leaveAnimation = new System.Windows.Media.Animation.Storyboard();
            var scaleXLeave = new System.Windows.Media.Animation.DoubleAnimation
            {
                To = 1,
                Duration = TimeSpan.FromMilliseconds(100)
            };
            var scaleYLeave = new System.Windows.Media.Animation.DoubleAnimation
            {
                To = 1,
                Duration = TimeSpan.FromMilliseconds(100)
            };
            System.Windows.Media.Animation.Storyboard.SetTargetProperty(scaleXLeave, new PropertyPath("RenderTransform.ScaleX"));
            System.Windows.Media.Animation.Storyboard.SetTargetProperty(scaleYLeave, new PropertyPath("RenderTransform.ScaleY"));
            leaveAnimation.Children.Add(scaleXLeave);
            leaveAnimation.Children.Add(scaleYLeave);
            Resources.Add("MouseLeaveAnimation", leaveAnimation);
        }

        public void SetBookData(Book book, string addedDate = null)
        {
            CurrentBook = book;
            if (!string.IsNullOrEmpty(addedDate))
            {
                AddedDate = addedDate;
            }
        }

        private void UpdateBookDisplay()
        {
            if (_currentBook == null) return;

            Dispatcher.Invoke(() =>
            {
                TitleText.Text = _currentBook.Title ?? "Без названия";
                AuthorText.Text = _currentBook.Author ?? "Неизвестен";
                GenreText.Text = _currentBook.Genre ?? "Не указан";
                YearText.Text = _currentBook.Year?.ToString() ?? "Не указан";
                LanguageText.Text = _currentBook.Language ?? "Не указан";

                // Отображение даты добавления, если она есть
                if (!string.IsNullOrEmpty(_addedDate))
                {
                    // Можно добавить TextBlock для отображения даты в разметке
                }

                if (!string.IsNullOrEmpty(_currentBook.ImageUrl))
                {
                    try
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(_currentBook.ImageUrl);
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        BookImage.Source = bitmap;
                        BookImage.Visibility = Visibility.Visible;
                    }
                    catch
                    {
                        BookImage.Visibility = Visibility.Collapsed;
                    }
                }
                else
                {
                    BookImage.Visibility = Visibility.Collapsed;
                }
            });
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

                if (_currentBook == null)
                {
                    MessageBox.Show("Книга не выбрана", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var result = MessageBox.Show($"Удалить '{_currentBook.Title}' из избранного?",
                    "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    var response = await _httpClient.DeleteAsync(
                        $"{_favoritesApiUrl}/remove?userId={_currentUserId}&bookId={_currentBook.Id}");

                    if (response.IsSuccessStatusCode)
                    {
                        // Вариант 1: Использовать событие
                        FavoriteRemoved?.Invoke(this, EventArgs.Empty);

                        // Вариант 2: Найти родительскую страницу Favorites и вызвать метод
                        // (раскомментируйте, если нужен этот вариант)
                        /*
                        var parentPage = FindParent<Favorites>();
                        if (parentPage != null)
                        {
                            // Предполагается, что в Favorites есть публичный метод LoadFavorites()
                            parentPage.LoadFavorites();
                        }
                        */

                        // Вариант 3: Просто удалить этот контрол из родительской панели
                        var parentPanel = FindParent<StackPanel>() ?? FindParent<WrapPanel>() as Panel;
                        if (parentPanel != null)
                        {
                            parentPanel.Children.Remove(this);
                        }

                        MessageBox.Show("Книга успешно удалена из избранного", "Успешно",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        MessageBox.Show($"Ошибка при удалении: {error}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла ошибка: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ReadButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentBook != null)
            {
                var parameters = new Dictionary<string, string>
                {
                    { "bookId", _currentBook.Id.ToString() },
                    { "title", _currentBook.Title ?? "" },
                    { "author", _currentBook.Author ?? "" },
                    { "image", _currentBook.ImageUrl ?? "" }
                };

                var readPage = new PageReadUsers();
                readPage.Tag = parameters;

                var window = Window.GetWindow(this);
                if (window is MainWindow mainWindow)
                {
                    mainWindow.frame.Navigate(readPage);
                }
            }
            else
            {
                MessageBox.Show("Книга не выбрана", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Border_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                var animation = Resources["MouseEnterAnimation"] as System.Windows.Media.Animation.Storyboard;
                animation?.Begin(border);
            }
        }

        private void Border_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                var animation = Resources["MouseLeaveAnimation"] as System.Windows.Media.Animation.Storyboard;
                animation?.Begin(border);
                border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0"));
            }
        }

        private void Button_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Button btn)
            {
                btn.Cursor = Cursors.Hand;
            }
        }

        private void Button_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Button btn)
            {
                btn.Cursor = Cursors.Arrow;
            }
        }

        private T FindParent<T>() where T : DependencyObject
        {
            DependencyObject parent = VisualTreeHelper.GetParent(this);
            while (parent != null && !(parent is T))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }
            return parent as T;
        }
    }
}