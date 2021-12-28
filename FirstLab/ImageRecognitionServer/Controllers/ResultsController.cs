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

namespace ImageRecognitionServer.Controllers
{
	[ApiController]
	public class ResultsController : ControllerBase
	{
		private ImageDB database;
		private CancellationTokenSource cts = new CancellationTokenSource();

		public ResultsController(ImageDB database)
		{
			this.database = database;
		}

		private Bitmap BitmapImageToBitmap(BitmapImage bitmapImage)
		{
			using (MemoryStream outStream = new MemoryStream())
			{
				BitmapEncoder enc = new BmpBitmapEncoder();
				enc.Frames.Add(BitmapFrame.Create(bitmapImage));
				enc.Save(outStream);
				Bitmap bitmap = new Bitmap(outStream);

				return new Bitmap(bitmap);
			}
		}

		private BitmapImage BitmapToBitmapImage(Bitmap bitmap)
		{
			using (var memory = new MemoryStream())
			{
				bitmap.Save(memory, ImageFormat.Png);
				memory.Position = 0;

				var bitmapImage = new BitmapImage();
				bitmapImage.BeginInit();
				bitmapImage.StreamSource = memory;
				bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
				bitmapImage.EndInit();
				bitmapImage.Freeze();

				return bitmapImage;
			}
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
		public async Task<ImmutableDictionary<string, BitmapImage>> GetResult(string imageFolder)
		{
			var imageDictionary = ImmutableDictionary.Create<string, BitmapImage>();

			var task1 = Task.Factory.StartNew(() => Component.ImageProcess(imageFolder, cts));

			var task2 = Task.Factory.StartNew(() =>
			{
				try
				{
					while (true)
					{
						Result result = Component.resultCollection.Take();

						using (var bitmap = new Bitmap(System.Drawing.Image.FromFile(Path.Combine(imageFolder, result.FileName))))
						{
							using (var g = Graphics.FromImage(bitmap))
							{
								g.DrawRectangle(Pens.Red, result.BBox[0], result.BBox[1], result.BBox[2] - result.BBox[0], result.BBox[3] - result.BBox[1]);

								using (var brushes = new SolidBrush(Color.FromArgb(50, Color.Red)))
								{
									g.FillRectangle(brushes, result.BBox[0], result.BBox[1], result.BBox[2] - result.BBox[0], result.BBox[3] - result.BBox[1]);
								}

								g.DrawString(result.Label + " " + result.Confidence.ToString("0.00"), new Font("Arial", 12), Brushes.Blue, new PointF(result.BBox[0], result.BBox[1]));
							}

							if (database.Images.All(o => o.ImageName != result.FileName))
							{
								database.Add(new ImageRecognitionContract.Image
								{
									ImageName = result.FileName,
									ImageHash = GetImageHash(BitmapToByteArray(bitmap)),
									ImagePhoto = new ImageRecognitionContract.ImageData { ImageDataArray = BitmapToByteArray(bitmap) },
									ImageObjects = new List<ImageRecognitionContract.ImageObject>() { new ImageRecognitionContract.ImageObject { ImageObjectName = result.Label, X1 = result.BBox[0], Y1 = result.BBox[1], X2 = result.BBox[2], Y2 = result.BBox[3] } }
								});
							}
							else
							{
								var image = database.Images.Where(o => o.ImageName == result.FileName).First();

								if (!image.ImageObjects.Contains(new ImageRecognitionContract.ImageObject { ImageObjectName = result.Label, X1 = result.BBox[0], Y1 = result.BBox[1], X2 = result.BBox[2], Y2 = result.BBox[3] }))
								{
									image.ImagePhoto = new ImageRecognitionContract.ImageData { ImageDataArray = BitmapToByteArray(bitmap) };
									image.ImageObjects.Add(new ImageRecognitionContract.ImageObject { ImageObjectName = result.Label, X1 = result.BBox[0], X2 = result.BBox[1], Y1 = result.BBox[2], Y2 = result.BBox[3] });
								}
							}

							database.SaveChanges();

							BitmapImage bitmapImage = BitmapToBitmapImage(bitmap);
							imageDictionary = imageDictionary.SetItem(result.FileName, bitmapImage);
						}
					}
				}
				catch (InvalidOperationException)
				{
					Trace.WriteLine("That's All!");
				}
			}, TaskCreationOptions.LongRunning);

			await Task.WhenAll(task1, task2);
			return imageDictionary;
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
