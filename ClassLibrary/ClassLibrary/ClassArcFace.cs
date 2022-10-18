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
public class ClassArcFace : IDisposable
{
    private InferenceSession session;
    private ConcurrentDictionary<Image<Rgb24>, float[]> embeddingsDict;

    public ClassArcFace()
    {
        using var modelStream = typeof(ClassArcFace).Assembly.GetManifestResourceStream("ClassLibrary.arcfaceresnet100-8.onnx");
        using var memoryStream = new MemoryStream();
        modelStream.CopyTo(memoryStream);
        var sessionOptions = new SessionOptions();
        sessionOptions.ExecutionMode = ExecutionMode.ORT_PARALLEL;
        session = new InferenceSession(memoryStream.ToArray(), sessionOptions);
        embeddingsDict = new ConcurrentDictionary<Image<Rgb24>, float[]>();
    }

    ~ClassArcFace()
    {
        this.session.Dispose();
    }

    public async Task CalculateAllEmbeddingsAsync(Image<Rgb24> face, CancellationToken token)
    {
        await Task.Factory.StartNew(() =>
        {
            if (token.IsCancellationRequested)
                token.ThrowIfCancellationRequested(); // генерируем исключение
            if (embeddingsDict.ContainsKey(face))
            {
                return;
            }
            var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor("data", ImageToTensor(face)) };
            IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results;
            lock (session)
            {
                results = session.Run(inputs);
            }
            if (token.IsCancellationRequested)
                token.ThrowIfCancellationRequested(); // генерируем исключение
            float[] embeddings = Normalize(results.First(v => v.Name == "fc1").AsEnumerable<float>().ToArray());
            embeddingsDict.TryAdd(face, embeddings);
        }, token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
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

}

