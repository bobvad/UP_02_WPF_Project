using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace UP_02.Pages.Users
{
    public partial class Registration : Page
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://localhost:7000/api/v1/";

        public Registration()
        {
            InitializeComponent();

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(BaseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        private async void OnRegisterClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn) btn.IsEnabled = false;

            try
            {
                if (string.IsNullOrWhiteSpace(LoginBox.Text))
                {
                    ShowWarning("Введите логин");
                    return;
                }

                if (string.IsNullOrWhiteSpace(PasswordBox.Text))
                {
                    ShowWarning("Введите пароль");
                    return;
                }

                var formData = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("Login", LoginBox.Text.Trim()),
                    new KeyValuePair<string, string>("Password", PasswordBox.Text.Trim())
                });

                var response = await _httpClient.PostAsync("UsersControllers/Reg", formData);
                var responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Регистрация успешна! Теперь вы можете войти.",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    NavigationService?.Navigate(new Authtorization());
                }
                else
                {
                    ShowError($"Ошибка регистрации: {response.StatusCode}\n{responseString}");
                }
            }
            catch (HttpRequestException ex)
            {
                ShowError($"Ошибка подключения к серверу: {ex.Message}\nПроверьте, запущен ли API на {BaseUrl}");
            }
            catch (TaskCanceledException)
            {
                ShowError("Превышено время ожидания ответа от сервера");
            }
            catch (Exception ex)
            {
                ShowError($"Произошла ошибка: {ex.Message}");
            }
        }

        private void GoToLoginLink_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new Authtorization());
        }

        private void ShowWarning(string message) =>
            MessageBox.Show(message, "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);

        private void ShowError(string message) =>
            MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}