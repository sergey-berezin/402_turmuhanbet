// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using Microsoft.ML.OnnxRuntime;
using System.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Threading;
using System.Threading.Tasks;

using ClassLibrary;

namespace testConsole 
{
    internal class Program
    {

        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");


            /*
            Console.WriteLine("Predicting contents of image...");
            
            foreach (var kv in obj1.session.InputMetadata)
                Console.WriteLine($"{kv.Key}: {MetadataToString(kv.Value)}");
            foreach (var kv in obj1.session.OutputMetadata)
                Console.WriteLine($"{kv.Key}: {MetadataToString(kv.Value)}]");

            string MetadataToString(NodeMetadata metadata)
            => $"{metadata.ElementType}[{String.Join(",", metadata.Dimensions.Select(i => i.ToString()))}]";
            */

            CancellationTokenSource cancelTokenSource = new CancellationTokenSource();
            CancellationToken token = cancelTokenSource.Token;

            ClassArcFace obj1 = new ClassArcFace();
            ClassArcFace obj2 = new ClassArcFace();


            using var face1 = Image.Load<Rgb24>("face1.png");
            using var face2 = Image.Load<Rgb24>("face2.png");

            using var face3 = Image.Load<Rgb24>("madiyar1.jpg");
            using var face4 = Image.Load<Rgb24>("madiyar2.jpg");

            Stopwatch sw1 = new Stopwatch();
            sw1.Start();

            var emb1 = obj1.CalculateAllEmbeddingsAsync(face1, token);
            var emb2 = obj1.CalculateAllEmbeddingsAsync(face2, token);
            var emb3 = obj1.CalculateAllEmbeddingsAsync(face3, token);
            var emb4 = obj1.CalculateAllEmbeddingsAsync(face4, token);
            //cancelTokenSource.Cancel();
            await emb1;
            await emb2;
            await emb3;
            await emb4;
            var res1 = obj1.CalculateDistanceSimilarity(face1, face2);
            var res2 = obj1.CalculateDistanceSimilarity(face1, face1);
            var res3 = obj1.CalculateDistanceSimilarity(face3, face4);
            await res1;
            await res2;
            await res3;

            sw1.Stop();

            Console.WriteLine("------ Results of face1 and face2 ------");
            Console.WriteLine($"Distance: {res1.Result.Item1}");
            Console.WriteLine($"Similarity: {res1.Result.Item2}");

            Console.WriteLine("------ Results of face1 and face1 ------");
            Console.WriteLine($"Distance: {res2.Result.Item1}");
            Console.WriteLine($"Similarity: {res2.Result.Item2}");

            Console.WriteLine("------ Results of face3 and face4 ------");
            Console.WriteLine($"Distance: {res3.Result.Item1}");
            Console.WriteLine($"Similarity: {res3.Result.Item2}");

            Console.WriteLine("Time elapsed: {0} ms", sw1.ElapsedMilliseconds);


            var emb5 = obj2.CalculateAllEmbeddingsAsync(face1, token);
            

            cancelTokenSource.Dispose();
        }
    }
}