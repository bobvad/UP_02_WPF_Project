using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Newtonsoft.Json;
using UP_02.Models;

namespace UP_02.Pages
{
    public partial class RecomendationAI : Page
    {
        private const string ApiBaseUrl = "https://localhost:7000/api/AI/";
        private int CurrentUserId => SessionManager.CurrentUserId;

        private readonly HttpClient _httpClient;
        private bool _isLoading = false;

        public class SimpleAskResponse
        {
            public string response { get; set; }
            public bool success { get; set; }
            public string error { get; set; }
        }

        public class PersonalRecommendationResponse
        {
            public string recommendation { get; set; }
            public bool success { get; set; }
            public string error { get; set; }
        }

        public class AutoRecommendationResponse
        {
            public string recommendation { get; set; }
            public bool show { get; set; }
            public bool success { get; set; }
            public string message { get; set; }
            public string error { get; set; }
        }

        public RecomendationAI()
        {
            InitializeComponent();

            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

            _httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(ApiBaseUrl),
                Timeout = TimeSpan.FromSeconds(60)
            };

            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            Loaded += (s, e) => CheckUserId();
        }

        private void CheckUserId()
        {
            if (CurrentUserId == 0)
            {
                MessageBox.Show("Необходимо авторизоваться", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NavigationService?.Navigate(new Users.Authtorization());
            }
        }

        private async void BtnSimpleAsk_Click(object sender, RoutedEventArgs e)
        {
            var query = TxtSimpleQuery.Text?.Trim();
            if (string.IsNullOrWhiteSpace(query) || query == "Порекомендуй книгу...")
            {
                ShowError("Введите запрос");
                return;
            }
            await SendSimpleAskRequest(query);
        }

        private async void BtnPersonal_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentUserId == 0)
            {
                ShowError("Сначала авторизуйтесь");
                return;
            }
            await SendPersonalRequest(CurrentUserId);
        }

        private async void BtnAuto_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentUserId == 0)
            {
                ShowError("Сначала авторизуйтесь");
                return;
            }
            await SendAutoRequest(CurrentUserId);
        }

        private void BtnCopy_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(TxtResult.Text))
            {
                Clipboard.SetText(TxtResult.Text);
                ShowStatus("Скопировано");
            }
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            TxtResult.Text = string.Empty;
            HideError();
            TxtSimpleQuery.Text = "Порекомендуй книгу...";
            ShowStatus("Очищено");
        }

        private async Task SendSimpleAskRequest(string query)
        {
            if (!SetLoading(true)) return;

            try
            {
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("query", query)
                });

                var response = await _httpClient.PostAsync("simple-ask", content);
                var responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonConvert.DeserializeObject<SimpleAskResponse>(responseString);

                    if (result?.success == true)
                    {
                        DisplayResult(result.response);
                        ShowStatus("Рекомендация получена");
                    }
                    else
                    {
                        ShowError(result?.error ?? "Не удалось получить рекомендацию");
                    }
                }
                else
                {
                    ShowError($"Ошибка сервера: {response.StatusCode}");
                }
            }
            catch (HttpRequestException)
            {
                ShowError("Не удалось подключиться к серверу. Проверьте запущен ли API по адресу https://localhost:7000");
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка: {ex.Message}");
            }
            finally
            {
                SetLoading(false);
            }
        }

        private async Task SendPersonalRequest(int userId)
        {
            if (!SetLoading(true)) return;

            try
            {
                ShowStatus($"Загрузка персональных рекомендаций...");

                var response = await _httpClient.GetAsync($"personal/{userId}");
                var responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonConvert.DeserializeObject<PersonalRecommendationResponse>(responseString);

                    if (result?.success == true)
                    {
                        DisplayResult($"Персональные рекомендации:\n\n{result.recommendation}");
                        ShowStatus("Персональные рекомендации загружены");
                    }
                    else
                    {
                        ShowError(result?.error ?? "Рекомендации не найдены");
                    }
                }
                else
                {
                    ShowError($"Ошибка сервера: {response.StatusCode}");
                }
            }
            catch (HttpRequestException)
            {
                ShowError("Не удалось подключиться к серверу");
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка: {ex.Message}");
            }
            finally
            {
                SetLoading(false);
            }
        }

        private async Task SendAutoRequest(int userId)
        {
            if (!SetLoading(true)) return;

            try
            {
                ShowStatus($"Проверка авто-рекомендаций...");

                var response = await _httpClient.GetAsync($"auto/{userId}");
                var responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonConvert.DeserializeObject<AutoRecommendationResponse>(responseString);

                    if (result?.success == true)
                    {
                        if (result.show == true && !string.IsNullOrWhiteSpace(result.recommendation))
                        {
                        }
                        else if (!string.IsNullOrWhiteSpace(result.message))
                        {
                            TxtResult.Text = result.message;
                            TxtResult.Foreground = new SolidColorBrush(Color.FromRgb(127, 140, 141));
                            ShowStatus(result.message);
                        }
                        else
                        {
                            TxtResult.Text = "Новых рекомендаций пока нет";
                            TxtResult.Foreground = new SolidColorBrush(Color.FromRgb(127, 140, 141));
                        }
                    }
                    else
                    {
                        ShowError(result?.error ?? "Ошибка получения рекомендаций");
                    }
                }
                else
                {
                    ShowError($"Ошибка сервера: {response.StatusCode}");
                }
            }
            catch (HttpRequestException)
            {
                ShowError("Не удалось подключиться к серверу");
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка: {ex.Message}");
            }
            finally
            {
                SetLoading(false);
            }
        }

        private void DisplayResult(string text)
        {
            TxtResult.Foreground = new SolidColorBrush(Color.FromRgb(44, 62, 80));
            TxtResult.Text = text;
            ScrollResult?.ScrollToTop();
            HideError();
        }

        private void ShowError(string message)
        {
            TxtError.Text = message;
            PnlError.Visibility = Visibility.Visible;
            PnlLoading.Visibility = Visibility.Collapsed;
            TxtResult.Visibility = Visibility.Collapsed;
        }

        private void HideError()
        {
            PnlError.Visibility = Visibility.Collapsed;
            TxtResult.Visibility = Visibility.Visible;
        }

        private void ShowStatus(string message)
        {
        }

        private bool SetLoading(bool isLoading)
        {
            if (_isLoading && isLoading) return false;

            _isLoading = isLoading;

            BtnSimpleAsk.IsEnabled = !isLoading;
            BtnPersonal.IsEnabled = !isLoading;

            PnlLoading.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;

            if (isLoading)
            {
                TxtResult.Visibility = Visibility.Collapsed;
                HideError();
            }

            return true;
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.init.frame.Navigate(new Pages.MainPage());
        }
    }
}