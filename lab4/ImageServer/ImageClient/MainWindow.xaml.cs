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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using ImageContracts;
using System.Net.Http;
using Newtonsoft.Json;
using System.Text.Json;

namespace ImageClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly string url = "http://localhost:5032/api/images";
        private List<Tuple<byte[], string>> images_bytes_path;
        private CancellationTokenSource cancelTokenSource;
        private CancellationToken token;
        private bool calculations_status;
        private Dictionary<byte[], int> id_image_dict;
        private const int MaxRetries = 3;  


        public MainWindow()
        {
            InitializeComponent();
            cancelTokenSource = new CancellationTokenSource();
            token = cancelTokenSource.Token;
            calculations_status = false;
            images_bytes_path = new List<Tuple<byte[], string>>();
            id_image_dict = new Dictionary<byte[], int>();
        }

        //Метод загружает изображения с выбранного каталога и вызввает метод, который строит сетку
        private void Button_Open_Images(object sender, RoutedEventArgs e)
        {
            Grid_Clear();
            Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();
            ofd.Multiselect = true;
            ofd.Filter = "Images (*.jpg, *.png)|*.jpg;*.png";
            var projectRootFolder = System.IO.Path.GetFullPath("../../../Images");
            ofd.InitialDirectory = projectRootFolder;
            var response = ofd.ShowDialog();
            if (response == true)
            {
                foreach (var path in ofd.FileNames)
                {
                    images_bytes_path.Add(Tuple.Create(System.IO.File.ReadAllBytes(path), path));
                }
            }
            Grid_Construct();
        }

        //Метод строит сетку по каталогу изображений
        private void Grid_Construct()
        {
            int n = images_bytes_path.Count;
            for (int i = 0; i < n + 1; i++)
            {
                table.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
                table.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });

                if (i > 0)
                {
                    var image1 = new System.Windows.Controls.Image
                    {
                        Source = (BitmapSource)new ImageSourceConverter().ConvertFrom(images_bytes_path[i - 1].Item1)
                    };

                    var image2 = new System.Windows.Controls.Image
                    {
                        Source = (BitmapSource)new ImageSourceConverter().ConvertFrom(images_bytes_path[i - 1].Item1)
                    };

                    Grid.SetColumn(image1, 0);
                    Grid.SetRow(image1, i);
                    table.Children.Add(image1);

                    Grid.SetColumn(image2, i);
                    Grid.SetRow(image2, 0);
                    table.Children.Add(image2);
                }
            }
        }

        //метод очищает сетку, массивы с изображениями, обновляет токены
        public void Grid_Clear()
        {
            calculations_status = false;
            cancelTokenSource = new CancellationTokenSource();
            token = cancelTokenSource.Token;
            int size = images_bytes_path.Count;
            if (size == 0)
                return;
            table.Children.Clear();
            pbStatus.Value = 0;
            for (int i = 0; i < size + 1; i++)
            {
                table.RowDefinitions.Clear();
            }
            for (int i = 0; i < size + 1; i++)
            {
                table.ColumnDefinitions.Clear();
            }
            images_bytes_path.Clear();
        }


        public async Task<int> CalculateImage(byte[] image, string path)
        {
            try
            {          
                DataStruct obj1 = new DataStruct(image, path);
                var s = JsonConvert.SerializeObject(obj1);
                var content = new StringContent(s);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
  
                int requestCount = 0;
                HttpClient client = new HttpClient();
                var task1 = await client.PostAsync(url, content, token);
                requestCount++;
                while (requestCount < MaxRetries && task1.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    task1 = await client.PostAsync(url, content, token);
                    requestCount++;
                }
                if (task1.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    throw new Exception($"Не получается добавить изображение в базу данных по запросу \"{url}\" в {requestCount} попытках");
                }
                var task1_result = JsonConvert.DeserializeObject<int>(task1.Content.ReadAsStringAsync().Result);
                return task1_result;
            }
            catch (OperationCanceledException e1)
            {
                Console.WriteLine($"{nameof(OperationCanceledException)} thrown with message: {e1.Message}");
                return -1;
            }
        }

        async Task<float> Distance(float[] v1, float[] v2)
        {
            return await Task<float>.Factory.StartNew(() => {
                return Length(v1.Zip(v2).Select(p => p.First - p.Second).ToArray());
            });
        }

        async Task<float> Similarity(float[] v1, float[] v2)
        {
            return await Task<float>.Factory.StartNew(() =>
            {
                return v1.Zip(v2).Select(p => p.First * p.Second).Sum();
            });
        }
        float Length(float[] v) => (float)Math.Sqrt(v.Select(x => x * x).Sum());

        //Метод начинает вычисления по заданным изображениям
        private async void Button_Start_Calculations(object sender, RoutedEventArgs e)
        {
            if (images_bytes_path.Count == 0)
            {
                MessageBox.Show("Пожалуйста, выберите каталог с изображениями.");
                return;
            }
            if (calculations_status)
            {
                MessageBox.Show("Вычисления уже произведены. Пожлауйста, обновите матрицу.");
                return;
            }
            int step1 = 500 / images_bytes_path.Count;
            int step2 = 500 / (images_bytes_path.Count * images_bytes_path.Count);
            var tasks = new List<Task<int>>();
            id_image_dict.Clear();
            

            for (int i = 0; i < images_bytes_path.Count; i++)
            {
                var task1 = CalculateImage(images_bytes_path[i].Item1, images_bytes_path[i].Item2);
                tasks.Add(task1);
            }

            for (int i = 0; i < tasks.Count; i++)
            {
                try
                {
                    await tasks[i];                   
                    if(tasks[i].Result != -1)
                    {
                        if (id_image_dict.ContainsKey(images_bytes_path[i].Item1) == false)
                        {
                            id_image_dict.Add(images_bytes_path[i].Item1, tasks[i].Result);
                        }
                        pbStatus.Value += step1;
                    }
                }
                catch (OperationCanceledException e2)
                {
                    Console.WriteLine($"{nameof(OperationCanceledException)} thrown with message: {e2.Message}");
                }
            }
            HttpClient client = new HttpClient();
            int requestCount = 0;
            for (int i = 0; i < images_bytes_path.Count; i++)
            {
                for (int j = 0; j < images_bytes_path.Count; j++)
                {
                    var l = new Label();
                    Grid.SetColumn(l, i + 1);
                    Grid.SetRow(l, j + 1);
                    l.HorizontalAlignment = HorizontalAlignment.Center;
                    l.VerticalAlignment = VerticalAlignment.Center;
                    l.FontSize = 12;
                    if(id_image_dict.ContainsKey(images_bytes_path[i].Item1) == false || id_image_dict.ContainsKey(images_bytes_path[j].Item1) == false)
                    {
                        l.Content = $"Distance: Not calculated\n Similarity: Not calculated";
                    }
                    else
                    {
                        int id1 = id_image_dict[images_bytes_path[i].Item1];
                        int id2 = id_image_dict[images_bytes_path[j].Item1];
                        requestCount = 0;
                        var taskGetImage1 = await client.GetAsync($"{url}/{id1}");
                        requestCount++;
                        while (requestCount < MaxRetries && taskGetImage1.StatusCode != System.Net.HttpStatusCode.OK)
                        {
                            taskGetImage1 = await client.GetAsync($"{url}/{id1}");
                            requestCount++;
                        }
                        if (taskGetImage1.StatusCode != System.Net.HttpStatusCode.OK)
                        {
                            throw new Exception($"Не получается получить изображение по запросу \"{url}/{id1}\" в {requestCount} попытках");
                        }
                        var image1 = JsonConvert.DeserializeObject<ImageContracts.Image>(taskGetImage1.Content.ReadAsStringAsync().Result);

                        requestCount = 0;
                        var taskGetImage2 = await client.GetAsync($"{url}/{id2}");
                        requestCount++;
                        while (requestCount < MaxRetries && taskGetImage2.StatusCode != System.Net.HttpStatusCode.OK)
                        {
                            taskGetImage2 = await client.GetAsync($"{url}/{id2}");
                            requestCount++;
                        }
                        if (taskGetImage1.StatusCode != System.Net.HttpStatusCode.OK)
                        {
                            throw new Exception($"Не получается получить изображение по запросу \"{url}/{id2}\" в {requestCount} попытках");
                        }                   
                        var image2 = JsonConvert.DeserializeObject<ImageContracts.Image>(taskGetImage2.Content.ReadAsStringAsync().Result);

                        float[] embeddings1 = new float[image1.Embedding.Length / 4];
                        for (int t = 0; t < image1.Embedding.Length / 4; t++)
                            embeddings1[t] = BitConverter.ToSingle(image1.Embedding, t * 4);

                        float[] embeddings2 = new float[image2.Embedding.Length / 4];
                        for (int t = 0; t < image2.Embedding.Length / 4; t++)
                            embeddings2[t] = BitConverter.ToSingle(image2.Embedding, t * 4);

                        var dist = Distance(embeddings1, embeddings2);
                        var similarity = Similarity(embeddings1, embeddings2);
                        await dist;
                        await similarity;

                        l.Content = $"Distance: {dist.Result}\n Similarity: {similarity.Result}";
                        pbStatus.Value += step2;
                    }
                    table.Children.Add(l);
                }
            }
            if (!token.IsCancellationRequested)
            {
                pbStatus.Value = 1000;
            }
            calculations_status = true;
        }

        //Метод очищает сетку
        private void Button_Grid_Clear(object sender, RoutedEventArgs e)
        {
            if (images_bytes_path.Count == 0)
            {
                MessageBox.Show("Матрица уже очищена.");
                return;
            }
            Grid_Clear();
        }

        //Метод открывает диалоговое окно с данными из базы данных
        private void Button_Open_Database(object sender, RoutedEventArgs e)
        {
            WindowDatabasexaml windowDatabase = new WindowDatabasexaml();
            windowDatabase.ShowDialog();
        }

        //Метод отменяет вычисления
        private void Button_Cancel_Calculations(object sender, RoutedEventArgs e)
        {
            cancelTokenSource.Cancel();
            MessageBox.Show("Вычисления прерваны.");
        }
    }
}
