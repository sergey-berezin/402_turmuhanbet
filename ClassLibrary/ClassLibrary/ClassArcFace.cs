using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Threading.Tasks.Dataflow;
using System.Collections.Concurrent;

namespace ClassLibrary;
public class ClassArcFace
{
    private InferenceSession session;
    private TransformBlock<Image<Rgb24>, float[]> transform;
    private BufferBlock<Image<Rgb24>> buffer;
    private ConcurrentDictionary<Image<Rgb24>, float[]> embeddingsDict;

    public ClassArcFace(CancellationToken token)
    {
        using var modelStream = typeof(ClassArcFace).Assembly.GetManifestResourceStream("ClassLibrary.arcfaceresnet100-8.onnx");
        using var memoryStream = new MemoryStream();
        modelStream.CopyTo(memoryStream);
        var sessionOptions = new SessionOptions();
        sessionOptions.ExecutionMode = ExecutionMode.ORT_PARALLEL;
        this.session = new InferenceSession(memoryStream.ToArray(), sessionOptions);
        this.transform = new TransformBlock<Image<Rgb24>, float[]>(face => GetEmbeddings(face, token));
        this.buffer = new BufferBlock<Image<Rgb24>>();
        buffer.LinkTo(transform);
        this.embeddingsDict = new ConcurrentDictionary<Image<Rgb24>, float[]>();
    }

    ~ClassArcFace()
    {
        this.session.Dispose();
    }

    /*
        Данный способ доступа к API позволяет, передав два изображения, получить результат distance и similarity. Метод startCalculations
        проверяет, существует ли уже просчитанный embedding для данного изображения, тем самым спасает нас от повторного вычисления
        embeddings для изображения(но не гарантирует это на 100 процентов). У него есть один большой недостаток:
        При асинхронном вызове данного метода в консольном приложении, вызов может не успеть добавить в словарь (face, embeddings) новый элемент,
        тем саммы при параллельной работе нескольких вызовов, метод может повторно просчитать уже существующий embedding

    */
    public async Task<Tuple<float, float>> startCalculations(Image<Rgb24> face1, Image<Rgb24> face2, CancellationToken token)
    {
        float[] embeddings1;
        float[] embeddings2;

        if (embeddingsDict.ContainsKey(face1) == true && embeddingsDict.ContainsKey(face2) == true)
        {
            //Console.WriteLine("face1 and face2 are already exists");
            embeddings1 = embeddingsDict[face1];
            embeddings2 = embeddingsDict[face2];
        }
        else if(embeddingsDict.ContainsKey(face1) == true && embeddingsDict.ContainsKey(face2) == false)
        {
            //Console.WriteLine("face1 is already exists");
            embeddings1 = embeddingsDict[face1];
            buffer.Post(face2);
            embeddings2 = await transform.ReceiveAsync();
            embeddingsDict.TryAdd(face2, embeddings2); //добавляем в словарь вычисленный embedding2
        }
        else if (embeddingsDict.ContainsKey(face1) == false && embeddingsDict.ContainsKey(face2) == true)
        {
            //Console.WriteLine("face2 is already exists");
            embeddings2 = embeddingsDict[face2];
            buffer.Post(face1);
            embeddings1 = await transform.ReceiveAsync();
            embeddingsDict.TryAdd(face1, embeddings1); //добавляем в словарь вычисленный embedding1
        }
        else
        {
            //Console.WriteLine("face1 and face2 are not exists");
            buffer.Post(face1);
            buffer.Post(face2);
            embeddings1 = await transform.ReceiveAsync();
            embeddings2 = await transform.ReceiveAsync();
            embeddingsDict.TryAdd(face1, embeddings1); //добавляем в словарь вычисленный embedding1
            embeddingsDict.TryAdd(face2, embeddings2); //добавляем в словарь вычисленный embedding2
        }

        //buffer.Complete();

        var dist = Distance(embeddings1, embeddings2);
        var similarity = Similarity(embeddings1, embeddings2);
        await dist;
        await similarity;

        return Tuple.Create(dist.Result, similarity.Result);

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

    float[] Normalize(float[] v)
    {
        var len = Length(v);
        return v.Select(x => x / len).ToArray();
    }

    DenseTensor<float> ImageToTensor(Image<Rgb24> img)
    {
        var w = img.Width;
        var h = img.Height;
        var t = new DenseTensor<float>(new[] { 1, 3, h, w });

        img.ProcessPixelRows(pa =>
        {
            for (int y = 0; y < h; y++)
            {
                Span<Rgb24> pixelSpan = pa.GetRowSpan(y);
                for (int x = 0; x < w; x++)
                {
                    t[0, 0, y, x] = pixelSpan[x].R;
                    t[0, 1, y, x] = pixelSpan[x].G;
                    t[0, 2, y, x] = pixelSpan[x].B;
                }
            }
        });
        return t;
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }

    
    float[] GetEmbeddings(Image<Rgb24> face, CancellationToken token)
    {
        if (token.IsCancellationRequested)
            token.ThrowIfCancellationRequested(); // генерируем исключение

        var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor("data", ImageToTensor(face)) };
        using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = session.Run(inputs);
        if (token.IsCancellationRequested)
            token.ThrowIfCancellationRequested(); // генерируем исключение
        return Normalize(results.First(v => v.Name == "fc1").AsEnumerable<float>().ToArray());
    }

    /*
        Данные два метода дают альтернативный доступ к API. Сначала в консольном приложении мы запускаем метод GalculateAllEmbeddings
        для вычисления embeddings для каждого изображения.
        После этого запускаем метод CalculateDistanceSimilarity, что бы посчитать distance и similarity между любыми двумя изображениями.
        Важным преимуществом данного подхода является то, что embeddings для лиц не будет постоянно пересчитываться. Он сохраняется в словаре
        и при необходимости выдает значение embeddings по ключу face
    */
    public async Task CalculateAllEmbeddings(Image<Rgb24> face, CancellationToken token)  //метод считает все embeddings всех лиц и помещает в словарь
    {
        if (token.IsCancellationRequested)
            token.ThrowIfCancellationRequested(); // генерируем исключение
        if (embeddingsDict.ContainsKey(face) == false)
        {
            buffer.Post(face);
            float[] embeddings = await transform.ReceiveAsync();
            embeddingsDict.TryAdd(face, embeddings);
        }   
    }

    public async Task<Tuple<float, float>> CalculateDistanceSimilarity(Image<Rgb24> face1, Image<Rgb24> face2)
    {
        float[] embeddings1 = embeddingsDict[face1];
        float[] embeddings2 = embeddingsDict[face2];
        var dist = Distance(embeddings1, embeddings2);
        var similarity = Similarity(embeddings1, embeddings2);
        await dist;
        await similarity;
        return Tuple.Create(dist.Result, similarity.Result);
    }

}

