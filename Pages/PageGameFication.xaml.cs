using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using UP_02.Models;

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
            public string PlaceColor
            {
                get
                {
                    return Place switch
                    {
                        1 => "#FFD700",
                        2 => "#C0C0C0", 
                        3 => "#CD7F32", 
                        _ => "#2C3E50"  
                    };
                }
            }
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
                ShowLoading(true);

                int? currentUserId = SessionManager.CurrentUserId;

                string url = "api/Leaders";

                if (currentUserId.HasValue && currentUserId.Value > 0)
                {
                    url += $"?currentUserId={currentUserId.Value}";
                }

                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();

                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    var leaders = JsonSerializer.Deserialize<List<LeaderModel>>(json, options);

                    if (leaders != null)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            LeadersListView.ItemsSource = leaders;

                            if (currentUserId.HasValue)
                            {
                                ScrollToCurrentUser(leaders);
                            }
                        });
                    }
                }
                else
                {
                    string error = await response.Content.ReadAsStringAsync();
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"Ошибка загрузки таблицы лидеров: {response.StatusCode}\n{error}",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                }
            }
            catch (HttpRequestException ex)
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Не удалось подключиться к серверу: {ex.Message}",
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
            finally
            {
                ShowLoading(false);
            }
        }

        private void ScrollToCurrentUser(List<LeaderModel> leaders)
        {
            if (LeadersListView.Items.Count == 0) return;

            for (int i = 0; i < leaders.Count; i++)
            {
                if (leaders[i].IsCurrentUser)
                {
                    var item = LeadersListView.Items[i];
                    LeadersListView.ScrollIntoView(item);
                    break;
                }
            }
        }

        private void ShowLoading(bool show)
        {
            Dispatcher.Invoke(() =>
            {
                if (LoadingIndicator != null)
                {
                    LoadingIndicator.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
                }

                if (LeadersListView != null)
                {
                    LeadersListView.Opacity = show ? 0.5 : 1;
                }
            });
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadLeaders();
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