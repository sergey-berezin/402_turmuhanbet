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

            ClassArcFace obj1 = new ClassArcFace(token);
            ClassArcFace obj2 = new ClassArcFace(token);
            ClassArcFace obj3 = new ClassArcFace(token);


            using var face1 = Image.Load<Rgb24>("face1.png");
            using var face2 = Image.Load<Rgb24>("face2.png");

            using var face3 = Image.Load<Rgb24>("madiyar1.jpg");
            using var face4 = Image.Load<Rgb24>("madiyar2.jpg");

            //синхронный запуск
            Stopwatch sw1 = new Stopwatch();
            sw1.Start();
            var res1 = obj1.startCalculations(face1, face2, token);
            await res1;
            var res2 = obj1.startCalculations(face1, face2, token);
            await res2;
            var res3 = obj1.startCalculations(face1, face2, token);
            await res3;
            sw1.Stop();

            Console.WriteLine("synchronous calculations:");
            Console.WriteLine("------ Results 1 ------");
            Console.WriteLine($"Distance = {res1.Result.Item1}");
            Console.WriteLine($"Similarity =  {res1.Result.Item2}");
            Console.WriteLine("------ Results 2 ------");
            Console.WriteLine($"Distance = {res2.Result.Item1}");
            Console.WriteLine($"Similarity =  {res2.Result.Item2}");
            Console.WriteLine("------ Results 3 ------");
            Console.WriteLine($"Distance = {res3.Result.Item1}");
            Console.WriteLine($"Similarity =  {res3.Result.Item2}");
            Console.WriteLine("Time elapsed: {0} ms", sw1.ElapsedMilliseconds);
            Console.WriteLine("------------------------------");


            //асинхронный запуск
            Stopwatch sw2 = new Stopwatch();
            sw2.Start();
            var res4 = obj2.startCalculations(face1, face2, token);
            //Thread.Sleep(3000);
            var res5 = obj2.startCalculations(face1, face2, token);
            var res6 = obj2.startCalculations(face1, face2, token);
            await res4;
            await res5;
            await res6;
            sw2.Stop();

            Console.WriteLine("asynchronous calculations:");
            Console.WriteLine("------ Results 1 ------");
            Console.WriteLine($"Distance = {res4.Result.Item1}");
            Console.WriteLine($"Similarity =  {res4.Result.Item2}");
            Console.WriteLine("------ Results 2 ------");
            Console.WriteLine($"Distance = {res5.Result.Item1}");
            Console.WriteLine($"Similarity =  {res5.Result.Item2}");
            Console.WriteLine("------ Results 3 ------");
            Console.WriteLine($"Distance = {res6.Result.Item1}");
            Console.WriteLine($"Similarity =  {res6.Result.Item2}");
            Console.WriteLine("Time elapsed: {0} ms", sw2.ElapsedMilliseconds);
            Console.WriteLine("------------------------------");

            //Отмена вычислений

            var res7 = obj2.startCalculations(face1, face2, token);
            //Thread.Sleep(1000);
            //cancelTokenSource.Cancel();
            Console.WriteLine("------ Results 1 ------");
            Console.WriteLine($"Distance = {res7.Result.Item1}");
            Console.WriteLine($"Similarity =  {res7.Result.Item2}");
            Console.WriteLine("------------------------------");
            

            //Альтернативный доступ к API
            Stopwatch sw3 = new Stopwatch();
            sw3.Start();
            //вычисляем embeddings для каждого лица
            var t1 = obj3.CalculateAllEmbeddings(face1, token);
            var t2 = obj3.CalculateAllEmbeddings(face2, token);
            var t3 = obj3.CalculateAllEmbeddings(face3, token);
            var t4 = obj3.CalculateAllEmbeddings(face4, token);
            await t1;
            await t2;
            await t3;
            await t4;
            //вычисляем distance и similarity между любыми двумя изображениями
            var res_face1_face2 = obj3.CalculateDistanceSimilarity(face1, face2);
            var res_face3_face4 = obj3.CalculateDistanceSimilarity(face3, face4);
            var res_face1_face1 = obj3.CalculateDistanceSimilarity(face1, face1);
            await res_face1_face2;
            await res_face3_face4;
            await res_face1_face1;
            sw3.Stop();
            Console.WriteLine("Result for face1 and face2");
            Console.WriteLine($"Distance = {res_face1_face2.Result.Item1}");
            Console.WriteLine($"Similarity =  {res_face1_face2.Result.Item2}");
            Console.WriteLine("------------");
            Console.WriteLine("Result for face3 and face4");
            Console.WriteLine($"Distance = {res_face3_face4.Result.Item1}");
            Console.WriteLine($"Similarity =  {res_face3_face4.Result.Item2}");
            Console.WriteLine("------------");
            Console.WriteLine("Result for face1 and face1");
            Console.WriteLine($"Distance = {res_face1_face1.Result.Item1}");
            Console.WriteLine($"Similarity =  {res_face1_face1.Result.Item2}");
            Console.WriteLine("------------");
            Console.WriteLine("Time elapsed: {0} ms", sw2.ElapsedMilliseconds);

            cancelTokenSource.Dispose();
        }
    }
}