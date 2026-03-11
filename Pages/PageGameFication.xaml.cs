using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace UP_02.Pages
{
    /// <summary>
    /// Логика взаимодействия для PageGameFication.xaml
    /// </summary>
    public partial class PageGameFication : Page
    {
        private HttpClient client;
        private DispatcherTimer refreshTimer;

        public class LeaderModel
        {
            public int UserId { get; set; }
            public string UserName { get; set; }
            public int Place { get; set; }
            public int BooksRead { get; set; }
            public int PagesRead { get; set; }
            public string BackgroundColor { get; set; }
            public bool IsCurrentUser { get; set; }
        }

        public PageGameFication()
        {
            InitializeComponent();

            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

            client = new HttpClient(handler);
            client.BaseAddress = new Uri("https://localhost:7000/");
            client.Timeout = TimeSpan.FromSeconds(30);
        }

        private void PageGameFication_Loaded(object sender, RoutedEventArgs e)
        {
            LoadLeaders();

            refreshTimer = new DispatcherTimer();
            refreshTimer.Interval = TimeSpan.FromSeconds(30);
            refreshTimer.Tick += (s, args) => LoadLeaders();
            refreshTimer.Start();
        }

        private void PageGameFication_Unloaded(object sender, RoutedEventArgs e)
        {
            refreshTimer?.Stop();
        }

        private async void LoadLeaders()
        {
            try
            {
                int? currentUserId = Models.SessionManager.CurrentUserId;
                string url = "api/Leaders";

                if (currentUserId.HasValue)
                {
                    url += $"?currentUserId={currentUserId.Value}";
                }

                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    var leaders = JsonSerializer.Deserialize<List<LeaderModel>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (leaders != null)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            LeadersListView.ItemsSource = leaders;
                        });
                    }
                }
                else
                {
                    string error = await response.Content.ReadAsStringAsync();
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"Ошибка загрузки таблицы лидеров: {response.StatusCode}",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                }
            }
            catch (HttpRequestException)
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("Не удалось подключиться к серверу",
                        "Ошибка соединения", MessageBoxButton.OK, MessageBoxImage.Warning);
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Ошибка: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadLeaders();
        }
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.init.frame.Navigate(new MainPage());
        }
    }
}