using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.Net.Http;
using Newtonsoft.Json;
using System.Text.Json;
using Polly;
using Polly.Retry;

namespace ImageClient
{
    /// <summary>
    /// Логика взаимодействия для WindowDatabasexaml.xaml
    /// </summary>
    public partial class WindowDatabasexaml : Window
    {
        private readonly string url = "http://localhost:5032/api/images";
        public ObservableCollection<ImageContracts.Image> ImagesCollection { get; private set; }
        private const int MaxRetries = 3;
        private readonly AsyncRetryPolicy _retryPolicy;

        public WindowDatabasexaml()
        {
            _retryPolicy = Policy.Handle<HttpRequestException>().WaitAndRetryAsync(MaxRetries, times =>
                 TimeSpan.FromMilliseconds(Math.Exp(times) * 250));
            ImagesCollection = new ObservableCollection<ImageContracts.Image>();
            GetAllImages();
            InitializeComponent();
            DataContext = this;         
        }

        public async void GetAllImages()
        {
            try
            {
                await _retryPolicy.ExecuteAsync(async () =>
                {
                    HttpClient client = new HttpClient();
                    var taskGetAllImages = await client.GetAsync(url);
                    if (taskGetAllImages.IsSuccessStatusCode)
                    {
                        var id_list = JsonConvert.DeserializeObject<List<int>>(taskGetAllImages.Content.ReadAsStringAsync().Result);
                        for (int i = 0; i < id_list.Count; i++)
                        {
                            try
                            {
                                await _retryPolicy.ExecuteAsync(async () =>
                                {
                                    var taskGetImage = await client.GetAsync($"{url}/{id_list[i]}");
                                    if (taskGetImage.IsSuccessStatusCode)
                                    {
                                        var image = JsonConvert.DeserializeObject<ImageContracts.Image>(taskGetImage.Content.ReadAsStringAsync().Result);
                                        ImagesCollection.Add(image);
                                    }
                                });
                            }
                            catch (Exception ex1)
                            {
                                MessageBox.Show(ex1.Message);
                                throw new Exception($"Не получается отправить запрос по \"{url}/{id_list[i]}\" в {MaxRetries} попытках");
                            }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                throw new Exception($"Не получается отправить запрос по \"{url}\" в {MaxRetries} попытках");
            }        
            
        }
        private async void Button_Delete_Image(object sender, RoutedEventArgs e)
        {                    
            var image = ImagesCollection[ImagesCollectionListBox.SelectedIndex];
            try
            {              
                await _retryPolicy.ExecuteAsync(async () =>
                {
                    HttpClient client = new HttpClient();
                    var task = await client.DeleteAsync($"{url}/{image.Id}");
                    if (task.IsSuccessStatusCode)
                    {
                        var result = JsonConvert.DeserializeObject<int>(task.Content.ReadAsStringAsync().Result);
                        if (result == 1)
                        {
                            MessageBox.Show("Удаление прошло успешно!");
                            ImagesCollection.Remove(image);
                        }
                        else
                        {
                            MessageBox.Show("Удаление завершилось неудачей, пожалуйста, повторите попытку позже!");
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                throw new Exception($"Не получается отправить запрос по \"{url}/{image.Id}\" в {MaxRetries} попытках");
            }
        }
    }
}
