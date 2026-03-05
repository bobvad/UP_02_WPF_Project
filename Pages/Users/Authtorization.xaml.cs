using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Newtonsoft.Json;
using UP_02.Models;

namespace UP_02.Pages.Users
{
    public partial class Authtorization : Page
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://localhost:7000/api/v1/";

        public class AuthResponse
        {
            public int Id { get; set; }
            public string Login { get; set; }
            public string Message { get; set; }
        }

        public Authtorization()
        {
            InitializeComponent();

            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

            _httpClient = new HttpClient(handler);
            _httpClient.BaseAddress = new Uri(BaseUrl);
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
        }

        private void GoToRegisterLink_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Registration());
        }

        private async void OnLoginClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(LoginBox.Text))
                {
                    MessageBox.Show("Введите логин");
                    LoginBox.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(PasswordBox.Text))
                {
                    MessageBox.Show("Введите пароль");
                    PasswordBox.Focus();
                    return;
                }

                var formData = new Dictionary<string, string>
                {
                    { "Login", LoginBox.Text },
                    { "Password", PasswordBox.Text }
                };

                var content = new FormUrlEncodedContent(formData);
                var response = await _httpClient.PostAsync("UsersControllers/Auth", content);

                string responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    if (responseContent == "500")
                    {
                        MessageBox.Show("Ошибка сервера при авторизации");
                        return;
                    }

                    try
                    {
                        var authResponse = JsonConvert.DeserializeObject<AuthResponse>(responseContent);

                        if (authResponse != null && authResponse.Id > 0)
                        {
                            SessionManager.CurrentUserId = authResponse.Id;
                            SessionManager.CurrentUserLogin = authResponse.Login ?? LoginBox.Text;

                            MessageBox.Show($"Авторизация успешна! Добро пожаловать, {SessionManager.CurrentUserLogin}", "Успех");

                            NavigationService.Navigate(new MainPage());
                        }
                        else
                        {
                            MessageBox.Show("Авторизация успешна, но не удалось получить ID пользователя");
                            NavigationService.Navigate(new MainPage());
                        }
                    }
                    catch
                    {
                        MessageBox.Show("Авторизация успешна");
                        NavigationService.Navigate(new MainPage());
                    }
                }
                else
                {
                    MessageBox.Show($"Ошибка авторизации: Неверный логин или пароль");
                }
            }
            catch (HttpRequestException)
            {
                MessageBox.Show("Ошибка подключения к серверу. Проверьте подключение к интернету и доступность сервера.");
            }
            catch (JsonException)
            {
                MessageBox.Show("Ошибка при обработке ответа от сервера");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Неожиданная ошибка: {ex.Message}");
            }
        }
    }
}