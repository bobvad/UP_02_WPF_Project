using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;
using UP_02.Models;

namespace UP_02.Pages.Carts
{
    /// <summary>
    /// Логика взаимодействия для BooksCarts.xaml
    /// </summary>
    public partial class BooksCarts : UserControl
    {
        private HttpClient _httpClient = new HttpClient();
        private string _apiUrl = "https://localhost:7000/api/ParsingBooks/books";

        private Book _currentBook;
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
            Loaded += BooksCarts_Loaded;
        }

        private async void BooksCarts_Loaded(object sender, RoutedEventArgs e)
        {
            if (CurrentBook == null)
            {
                await LoadBookData();
            }
        }

        public async Task LoadBookData()
        {
            try
            {
                var response = await _httpClient.GetAsync(_apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();

                    CurrentBook = JsonConvert.DeserializeObject<Book>(json);
                }
                else
                {
                    ShowErrorMessage($"Ошибка: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Ошибка загрузки: {ex.Message}");
            }
        }

        public void SetBookData(Book book)
        {
            CurrentBook = book;
        }

        private void UpdateBookDisplay()
        {
            if (CurrentBook != null)
            {
                TitleText.Text = CurrentBook.Title;
                AuthorText.Text = CurrentBook.Author;
                DescriptionText.Text = CurrentBook.Description;
                LanguageText.Text = CurrentBook.Language;
                PageCountText.Text = CurrentBook.PageCount.ToString();

                if (!string.IsNullOrEmpty(CurrentBook.ImageUrl))
                {
                    try
                    {
                        BookImage.Source = new BitmapImage(new Uri(CurrentBook.ImageUrl));
                    }
                    catch
                    {
                        BookImage.Source = null;
                    }
                }

                UpdateStatusDisplay();
            }
        }

        private void UpdateStatusDisplay()
        {
            if (CurrentBook != null)
            {
                StatusText.Text = CurrentBook.IsCompleted ? "Завершена" : "В процессе";

                var statusBorder = StatusText.Parent as Border;
                if (statusBorder != null)
                {
                    statusBorder.Background = CurrentBook.IsCompleted ?
                        Brushes.Green : Brushes.Red;
                }
            }
        }

        private void ShowErrorMessage(string message)
        {
            TitleText.Text = "Ошибка";
            DescriptionText.Text = message;
            StatusText.Text = "Ошибка загрузки";

            if (StatusText.Parent is Border border)
            {
                border.Background = Brushes.Orange;
            }
        }
        public async Task RefreshData()
        {
            await LoadBookData();
        }
    }
}
