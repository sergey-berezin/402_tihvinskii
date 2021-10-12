using System;
using System.IO;
using System.Linq;
using Microsoft.ML;
using System.Drawing;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using static Microsoft.ML.Transforms.Image.ImageResizingEstimator;

namespace ImageRecognitionComponent
{
    public class Component
    {
        const string modelPath = @"your\path\to\yolov4.onnx";

        static readonly string[] classesNames = new string[] { "person", "bicycle", "car", "motorbike", "aeroplane", "bus", "train", "truck", "boat", "traffic light", "fire hydrant", "stop sign", "parking meter", "bench", "bird", "cat", "dog", "horse", "sheep", "cow", "elephant", "bear", "zebra", "giraffe", "backpack", "umbrella", "handbag", "tie", "suitcase", "frisbee", "skis", "snowboard", "sports ball", "kite", "baseball bat", "baseball glove", "skateboard", "surfboard", "tennis racket", "bottle", "wine glass", "cup", "fork", "knife", "spoon", "bowl", "banana", "apple", "sandwich", "orange", "broccoli", "carrot", "hot dog", "pizza", "donut", "cake", "chair", "sofa", "pottedplant", "bed", "diningtable", "toilet", "tvmonitor", "laptop", "mouse", "remote", "keyboard", "cell phone", "microwave", "oven", "toaster", "sink", "refrigerator", "book", "clock", "vase", "scissors", "teddy bear", "hair drier", "toothbrush" };

        public static async IAsyncEnumerable<string> ImageProcessAsync(string imageFolder, CancellationTokenSource cts)
        {
            MLContext mlContext = new MLContext();

            var pipeline = mlContext.Transforms.ResizeImages(inputColumnName: "bitmap", outputColumnName: "input_1:0", imageWidth: 416, imageHeight: 416, resizing: ResizingKind.IsoPad)
                .Append(mlContext.Transforms.ExtractPixels(outputColumnName: "input_1:0", scaleImage: 1f / 255f, interleavePixelColors: true))
                .Append(mlContext.Transforms.ApplyOnnxModel(
                    shapeDictionary: new Dictionary<string, int[]>()
                    {
                        { "input_1:0", new[] { 1, 416, 416, 3 } },
                        { "Identity:0", new[] { 1, 52, 52, 3, 85 } },
                        { "Identity_1:0", new[] { 1, 26, 26, 3, 85 } },
                        { "Identity_2:0", new[] { 1, 13, 13, 3, 85 } },
                    },
                    inputColumnNames: new[]
                    {
                        "input_1:0"
                    },
                    outputColumnNames: new[]
                    {
                        "Identity:0",
                        "Identity_1:0",
                        "Identity_2:0"
                    },
                    modelFile: modelPath, recursionLimit: 100));

            var model = pipeline.Fit(mlContext.Data.LoadFromEnumerable(new List<BitmapData>()));

            var predictionEngine = mlContext.Model.CreatePredictionEngine<BitmapData, Prediction>(model);

            string[] fileEntries = Directory.GetFiles(imageFolder, "*.jpg");

            var n = fileEntries.Length;
            var tasks = new Task[n];
            var ct = cts.Token;
            
            var sw = new Stopwatch();
            sw.Start();

            for (int i = 0; i < n; i++)
			{
                if (ct.IsCancellationRequested)
				{
                    Console.WriteLine("Cancellation is requested!");
                    break;
				}

                var bitmap = new Bitmap(Image.FromFile(Path.Combine(imageFolder, fileEntries[i])));
                Prediction predict = null;

                lock (predictionEngine)
				{
                    predict = predictionEngine.Predict(new BitmapData() { Image = bitmap });
                }
                    
                var results = predict.GetResults(classesNames, 0.3f, 0.7f);

                foreach (var res in results)
                {
                    var x1 = res.BBox[0];
                    var y1 = res.BBox[1];
                    var x2 = res.BBox[2];
                    var y2 = res.BBox[3];

					yield return fileEntries[i].Split("\\").Last() + ": (" + x1.ToString("0.0") + ", " + y1.ToString("0.0") + ") - (" + x2.ToString("0.0") + ", " + y2.ToString("0.0") + "); " + res.Label + " - " + res.Confidence.ToString("0.00");
                }
            }

            await Task.WhenAll(tasks.Where(t => t != null));

            sw.Stop();
            Console.WriteLine($"Done in {sw.ElapsedMilliseconds} ms.");
        }
    }
}
