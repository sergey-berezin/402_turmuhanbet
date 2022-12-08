using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp.PixelFormats;
using ImageContracts;
using ClassLibrary;
namespace ImageServer.Database
{
    public class ImagesContext : DbContext
    {
        public DbSet<Image> Images { get; set; }
        public DbSet<ImageDetails> Details { get; set; }

        public ImagesContext()
        {
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder o)
        {
            o.UseSqlite("Data Source=images.db");
        }
    }
    public interface IImagesDb
    {
        Task<List<int>> GetAllImages();
        Task<int> PostImage(byte[] img, string path, CancellationToken token);
        Task<Image> TryGetImageById(int id);
        Task<int> TryDeleteImage(int id);
    }
    public class ImagesDatabase : IImagesDb
    {
        ClassArcFace obj1 = new ClassArcFace();
        public async Task<List<int>> GetAllImages()
        {
            var result = new List<int>();
            try
            {
                using (var db = new ImagesContext())
                {
                    var query = db.Images;
                    foreach (var image in query)
                    {
                        result.Add(image.Id);
                    }
                    return result;
                }
            }
            catch (Exception ex)
            {
                return result;
            }
        }

        public async Task<Image> TryGetImageById(int id)
        {
            try
            {
                using (var db = new ImagesContext())
                {
                    Image image = db.Images.Where(x => x.Id == id).Include(x => x.Details).FirstOrDefault();
                    return image; 
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<int> TryDeleteImage(int id)
        {
            try
            {
                using (var db = new ImagesContext())
                {
                    var deletedImage = db.Images.Where(x => x.Id == id).Include(x => x.Details).First();
                    if (deletedImage == null)
                    {
                        return -1;
                    }
                    db.Details.Remove(deletedImage.Details);
                    db.Images.Remove(deletedImage);
                    db.SaveChanges();
                }
                return 1;
            }
            catch (Exception ex)
            {
                return -1;
            }
        }

        public async Task<int> PostImage(byte[] image, string path, CancellationToken token)
        {
            try
            {
                Image existImage = null;
                using (var db = new ImagesContext())   //По хэш-коду ищем изображение, если хэш-код совпадает, то дальше сверяем содержмое 
                {
                    string hash = Image.GetHash(image);
                    var q = db.Images.Where(x => x.Hash == hash)
                        .Include(x => x.Details)
                        .Where(x => Equals(x.Details.Data, image));
                    if (q.Any())
                    {
                        existImage = q.First();
                    }
                }
                if (existImage is not null)   //Если в базе данных уже есть данное изображение, то просто возвращаем его id
                {
                    return existImage.Id;
                }
                else                        //Иначе вычисляем embeddings для данного изображения, добавялем в базу данных и возвращаем его id
                {
                    var face = SixLabors.ImageSharp.Image.Load<Rgb24>(path);
                    var task1 = obj1.CalculateAllEmbeddingsAsync(face, token);
                    await task1;
                    Console.WriteLine("Step1");
                    using (var db = new ImagesContext())           
                    {
                        var newImageDetails = new ImageDetails { Data = image };
                        var byteArray = new byte[task1.Result.Length * 4];
                        Buffer.BlockCopy(task1.Result, 0, byteArray, 0, byteArray.Length);
                        Image newImage = new Image
                        {
                            Name = path,
                            Embedding = byteArray,
                            Details = newImageDetails,
                            Hash = Image.GetHash(image)
                        };
                        db.Add(newImage);
                        db.SaveChanges();
                    }

                    existImage = null;
                    using (var db = new ImagesContext()) 
                    {
                        string hash = Image.GetHash(image);
                        var q = db.Images.Where(x => x.Hash == hash)
                            .Include(x => x.Details)
                            .Where(x => Equals(x.Details.Data, image));
                        if (q.Any())
                        {
                            existImage = q.First();
                        }              
                    }
                    if (existImage is not null)   //Если в базе данных уже есть данное изображение, то просто возвращаем его id
                    {
                        return existImage.Id;
                    }
                    else
                    {
                        return -1;
                    }
                }
            }
            catch (OperationCanceledException e1)
            {
                return -1;
            }
        }
    }
}
