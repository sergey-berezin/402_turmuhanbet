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

        public WindowDatabasexaml()
        {
            ImagesCollection = new ObservableCollection<ImageContracts.Image>();
            GetAllImages();
            InitializeComponent();
            DataContext = this;         
        }

        public async void GetAllImages()
        {
            int requestCount = 0;
            HttpClient client = new HttpClient();
            var taskGetAllImages = await client.GetAsync(url);
            requestCount++;
            while (requestCount < MaxRetries && taskGetAllImages.StatusCode != System.Net.HttpStatusCode.OK)
            {
                taskGetAllImages = await client.GetAsync(url);
                requestCount++;
            }
            if (taskGetAllImages.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception($"Не получается получить изображения по запросу \"{url}\" в {requestCount} попытках");
            }
            var id_list = JsonConvert.DeserializeObject<List<int>>(taskGetAllImages.Content.ReadAsStringAsync().Result);

            
            for (int i = 0; i < id_list.Count; i++)
            {
                requestCount = 0;
                var taskGetImage = await client.GetAsync($"{url}/{id_list[i]}");
                requestCount++;
                while(requestCount < MaxRetries && taskGetImage.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    taskGetImage = await client.GetAsync($"{url}/{id_list[i]}");
                    requestCount++;
                }
                if (taskGetImage.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    throw new Exception($"Не получается получить изображения по запросу \"{url}\" в {requestCount} попытках");
                }
                var image = JsonConvert.DeserializeObject<ImageContracts.Image>(taskGetImage.Content.ReadAsStringAsync().Result);
                ImagesCollection.Add(image);
            }

        }
        private async void Button_Delete_Image(object sender, RoutedEventArgs e)
        {         
            HttpClient client = new HttpClient();
            var image = ImagesCollection[ImagesCollectionListBox.SelectedIndex];
            int requestCount = 0;
            var task = await client.DeleteAsync($"{url}/{image.Id}");
            requestCount++;
            while (requestCount < MaxRetries && task.StatusCode != System.Net.HttpStatusCode.OK)
            {
                task = await client.DeleteAsync($"{url}/{image.Id}");
                requestCount++;
            }
            if (task.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception($"Не получается отправить запрос по \"{url}\" в {requestCount} попытках");
            }
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
    }
}
