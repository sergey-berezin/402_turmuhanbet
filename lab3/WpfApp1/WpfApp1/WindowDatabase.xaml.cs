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
using System.Collections;
using System.Windows.Navigation;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using ClassLibrary;     //подключили библиотеку классов из нашего пакета
using System.Threading;
using System.IO;
using System.Collections.ObjectModel;
using Microsoft.EntityFrameworkCore;

namespace WpfApp1
{
    /// <summary>
    /// Логика взаимодействия для WindowDatabase.xaml
    /// </summary>
    public partial class WindowDatabase : Window
    {
        public ObservableCollection<Image> ImagesCollection { get; private set; }
        public WindowDatabase()
        {
            ImagesCollection = new ObservableCollection<Image>();

            using (var db = new ImagesContext())
            {
                foreach (var image in db.Images)
                {
                    ImagesCollection.Add(image);
                }
            }

            InitializeComponent();
            DataContext = this;
        }

        private void Button_Delete_Image(object sender, RoutedEventArgs e)
        {
            try
            {
                var image = ImagesCollection[ImagesCollectionListBox.SelectedIndex];
                using (var db = new ImagesContext())
                {
                    var deletedImage = db.Images.Where(x => x.Id == image.Id).Include(x => x.Details).First();
                    if (deletedImage == null)
                    {
                        return;
                    }
                    db.Details.Remove(deletedImage.Details);
                    db.Images.Remove(deletedImage);
                    db.SaveChanges();
                    ImagesCollection.Remove(image);
                }
            }
            catch (Exception e1)
            {
                MessageBox.Show(e1.Message);
            }
        }
    }
}
