using System;
using System.IO;
using System.Linq;
using ImageDataBase;
using System.Drawing;
using System.Windows;
using System.Threading;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using ImageRecognitionComponent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Windows.Media.Imaging;
using System.Collections.Concurrent;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace WPF
{
	public partial class MainWindow : Window
	{
		private string imageFolder = "";
		private ImmutableDictionary<string, BitmapImage> imageDictionary = ImmutableDictionary.Create<string, BitmapImage>();
		private CancellationTokenSource cts = new CancellationTokenSource();
		private ImageDB dataBase = new ImageDB();

		public MainWindow()
		{
			InitializeComponent();

			foreach (ImageDataBase.Image image in dataBase.Images)
			{
				Bitmap bitmap;
				using (var memoryStream = new MemoryStream(image.ImagePhoto.ImageDataArray))
				{
					bitmap = new Bitmap(memoryStream);
				}

				imageDictionary = imageDictionary.Add(image.ImageName, BitmapToBitmapImage(bitmap));
			}

			imageFolder = @"C:\Users\torre\Desktop\Images";
			ImageListBox.ItemsSource = imageDictionary.ToImmutableList();
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

		private void OpenButton(object sender, RoutedEventArgs args)
		{
			CommonOpenFileDialog dialog = new CommonOpenFileDialog
			{
				InitialDirectory = @"C:\Users\torre\Desktop\Images",
				IsFolderPicker = true
			};

			if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
			{
				imageFolder = dialog.FileName;
				string[] fileEntries = Directory.GetFiles(dialog.FileName, "*.jpg");
				imageDictionary = imageDictionary.Clear();

				for (int i = 0; i < fileEntries.Length; i++)
				{
					imageDictionary = imageDictionary.Add(fileEntries[i], new BitmapImage(new Uri(fileEntries[i])));
				}

				ImageListBox.ItemsSource = imageDictionary.ToImmutableList();
			}
		}

		private async void StartButton(object sender, RoutedEventArgs args)
		{
			openButton.IsEnabled = false;
			resetButton.IsEnabled = false;

			if (startButton.Content.ToString() == "Start")
			{
				startButton.Content = "Stop";

				Component.resultCollection.Dispose();
				Component.resultCollection = new BlockingCollection<Result>();

				cts.Dispose();
				cts = new CancellationTokenSource();

				var task1 = Task.Factory.StartNew(() => Component.ImageProcess(imageFolder, cts));

				var task2 = Task.Factory.StartNew(() =>
				{
					try
					{
						while (true)
						{
							Result result = Component.resultCollection.Take();

							using (var bitmap = BitmapImageToBitmap(imageDictionary[result.FileName]))
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

								if (dataBase.Images.All(o => o.ImageName != result.FileName))
								{
									dataBase.Add(new ImageDataBase.Image
									{
										ImageName = result.FileName,
										ImageHash = GetImageHash(BitmapToByteArray(bitmap)),
										ImagePhoto = new ImageData { ImageDataArray = BitmapToByteArray(bitmap) },
										ImageObjects = new List<ImageObject>() { new ImageObject { ImageObjectName = result.Label, X1 = result.BBox[0], Y1 = result.BBox[1], X2 = result.BBox[2], Y2 = result.BBox[3] } }
									});
								}
								else
								{
									var image = dataBase.Images.Where(o => o.ImageName == result.FileName).First();

									if (!image.ImageObjects.Contains(new ImageObject { ImageObjectName = result.Label, X1 = result.BBox[0], Y1 = result.BBox[1], X2 = result.BBox[2], Y2 = result.BBox[3] }))
									{
										image.ImagePhoto = new ImageData { ImageDataArray = BitmapToByteArray(bitmap) };
										image.ImageObjects.Add(new ImageObject { ImageObjectName = result.Label, X1 = result.BBox[0], X2 = result.BBox[1], Y1 = result.BBox[2], Y2 = result.BBox[3] });
									}
								}

								dataBase.SaveChanges();

								BitmapImage bitmapImage = BitmapToBitmapImage(bitmap);
								imageDictionary = imageDictionary.SetItem(result.FileName, bitmapImage);

								Dispatcher.BeginInvoke(new Action(() =>
								{
									ImageListBox.ItemsSource = imageDictionary.ToImmutableList();
								}));
							}
						}
					}
					catch (InvalidOperationException)
					{
						Trace.WriteLine("That's All!");
					}
				}, TaskCreationOptions.LongRunning);

				await Task.WhenAll(task1, task2);
				startButton.Content = "Start";
			}
			else
			{
				startButton.Content = "Start";
				cts.Cancel();
			}

			openButton.IsEnabled = true;
			resetButton.IsEnabled = true;
		}

		private void ResetButton(object sender, RoutedEventArgs args)
		{
			openButton.IsEnabled = false;
			startButton.IsEnabled = false;

			foreach (ImageDataBase.Image image in dataBase.Images)
			{
				dataBase.Images.Remove(image);
			}

			dataBase.SaveChanges();
			imageDictionary = imageDictionary.Clear();
			ImageListBox.ItemsSource = imageDictionary;

			openButton.IsEnabled = true;
			startButton.IsEnabled = true;
		}
	}
}
