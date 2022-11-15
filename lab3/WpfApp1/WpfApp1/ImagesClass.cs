using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;

namespace WpfApp1
{
    
    public class Image
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Hash { get; set; }
        public byte[] Embedding { get; set; }
        public ImageDetails Details { get; set; }

        public static string GetHash(byte[] data)
        {
            using (var sha256 = SHA256.Create())
            {
                return string.Concat(sha256.ComputeHash(data).Select(x => x.ToString("X2")));
            }
        }
    }

    public class ImageDetails
    {
        public int Id { get; set; }
        public byte[] Data { get; set; }
    }

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
    
}
