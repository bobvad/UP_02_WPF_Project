using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Newtonsoft.Json;

namespace UP_02.Pages.Users
{
    /// <summary>
    /// Логика взаимодействия для Authtorization.xaml
    /// </summary>
    public partial class Authtorization : Page
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://localhost:7000/api/v1/";

        public Authtorization()
        {
            InitializeComponent();
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(BaseUrl);
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

                var loginData = new
                {
                    Login = LoginBox.Text,
                    Password = PasswordBox.Text
                };

                string json = JsonConvert.SerializeObject(loginData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("UsersControllers/Auth", content);

                string responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Авторизация успешна");
                    NavigationService.Navigate(new MainPage());
                }
                else
                {
                    MessageBox.Show($"Ошибка: {responseContent}");
                }
            }
            catch (HttpRequestException)
            {
                MessageBox.Show("Ошибка подключения к серверу");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }
    }
}
