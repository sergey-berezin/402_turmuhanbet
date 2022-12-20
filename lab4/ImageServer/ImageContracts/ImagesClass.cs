using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
namespace ImageContracts
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

    public class DataStruct
    {
        public byte[] Image { get; set; }
        public string Path { get; set; }

        public DataStruct(byte[] image, string path)
        {
            Image = image;
            Path = path;
        }
    }
}