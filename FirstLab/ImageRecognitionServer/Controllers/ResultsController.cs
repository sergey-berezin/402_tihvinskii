using ImageDataBase;
using ImageRecognitionComponent;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using System;
using System.Diagnostics;
using System.Windows.Media.Imaging;
using System.Collections.Concurrent;

namespace ImageRecognitionServer.Controllers
{
	[ApiController]
	public class ResultsController : ControllerBase
	{
		private ImageDB database;
		private CancellationTokenSource cts;

		public ResultsController(ImageDB database, CancellationTokenSource cts)
		{
			this.database = database;
			this.cts = cts;
		}

		private byte[] BitmapToByteArray(Bitmap bitmap)
		{
			using (var memoryStream = new MemoryStream())
			{
				bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
				return memoryStream.ToArray();
			}
		}

		private string GetImageHash(byte[] data)
		{
			using (var sha = new System.Security.Cryptography.SHA1CryptoServiceProvider())
			{
				return string.Concat(sha.ComputeHash(data).Select(x => x.ToString("X2")));
			}
		}

		[Route("start")]
		public async Task<ImmutableList<Tuple<string, byte[]>>> GetResult(string imageFolder)
		{
			var imageList = ImmutableList.Create<Tuple<string, byte[]>>();

			var task1 = Task.Factory.StartNew(() => Component.ImageProcess(imageFolder, cts));

			var task2 = Task.Factory.StartNew(() =>
			{
				try
				{
					while (true)
					{
						Result result = Component.resultCollection.Take();

						if (database.Images.All(o => o.ImageName != result.FileName))
						{
							var bm = new Bitmap(System.Drawing.Image.FromFile(Path.Combine(imageFolder, result.FileName)));

							ImageRecognitionContract.Image current = new ImageRecognitionContract.Image
							{
								ImageName = result.FileName,
								ImageHash = GetImageHash(BitmapToByteArray(bm)),
								ImagePhoto = new ImageRecognitionContract.ImageData { ImageDataArray = BitmapToByteArray(bm) },
								ImageObjects = new List<ImageRecognitionContract.ImageObject>() { }
							};

							database.Add(current);
							database.SaveChanges();
						}

						var img = database.Images.Where(o => o.ImageName == result.FileName).First();

						Bitmap bitmap;
						using (var ms = new MemoryStream(img.ImagePhoto.ImageDataArray))
						{
							bitmap = new Bitmap(ms);
						}
						
						using (var g = Graphics.FromImage(bitmap))
						{
							g.DrawRectangle(Pens.Red, result.BBox[0], result.BBox[1], result.BBox[2] - result.BBox[0], result.BBox[3] - result.BBox[1]);

							using (var brushes = new SolidBrush(Color.FromArgb(50, Color.Red)))
							{
								g.FillRectangle(brushes, result.BBox[0], result.BBox[1], result.BBox[2] - result.BBox[0], result.BBox[3] - result.BBox[1]);
							}

							g.DrawString(result.Label + " " + result.Confidence.ToString("0.00"), new Font("Arial", 12), Brushes.Blue, new PointF(result.BBox[0], result.BBox[1]));
						}

						var image = database.Images.Where(o => o.ImageName == result.FileName).First();

						if (!image.ImageObjects.Contains(new ImageRecognitionContract.ImageObject { ImageObjectName = result.Label, X1 = result.BBox[0], Y1 = result.BBox[1], X2 = result.BBox[2], Y2 = result.BBox[3] }))
						{
							image.ImagePhoto = new ImageRecognitionContract.ImageData { ImageDataArray = BitmapToByteArray(bitmap) };
							image.ImageObjects.Add(new ImageRecognitionContract.ImageObject { ImageObjectName = result.Label, X1 = result.BBox[0], X2 = result.BBox[1], Y1 = result.BBox[2], Y2 = result.BBox[3] });
						}

						database.SaveChanges();
						imageList = imageList.Add(new Tuple<string, byte[]>(result.FileName, BitmapToByteArray(bitmap)));
					}
				}
				catch (InvalidOperationException)	
				{
					Trace.WriteLine("That's All!");
				}
			}, TaskCreationOptions.LongRunning);

			await Task.WhenAll(task1, task2);

			Component.resultCollection = new BlockingCollection<Result>();
			return imageList;
		}

		[Route("stop")]
		public void Stop()
		{
			cts.Cancel();
		}

		[Route("clear")]
		public void Clear()
		{
			foreach (ImageRecognitionContract.Image image in database.Images)
			{
				database.Images.Remove(image);
			}

			database.SaveChanges();
		}

		[Route("show")]
		public List<ImageRecognitionContract.Image> Show()
		{
			return database.Images.ToList();
		}
	}
}
