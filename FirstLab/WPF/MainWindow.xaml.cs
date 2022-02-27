using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using ImageRecognitionContract;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Windows.Media.Imaging;
using System.Collections.Concurrent;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Net.Http;
using Newtonsoft.Json;

namespace WPF
{
	public partial class MainWindow : Window
	{
		private string imageFolder = "";
		private ImmutableDictionary<string, BitmapImage> imageDictionary = ImmutableDictionary.Create<string, BitmapImage>();
		HttpClient client = new HttpClient();

		private async void ShowDB()
		{
			try
			{
				string result = await client.GetStringAsync("https://localhost:5001/show");
				var images = JsonConvert.DeserializeObject<List<ImageRecognitionContract.Image>>(result);
				imageDictionary = imageDictionary.Clear();

				foreach (ImageRecognitionContract.Image image in images)
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
			catch (HttpRequestException ex)
			{
				MessageBox.Show(ex.Message, "Server Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		public MainWindow()
		{
			InitializeComponent();
			ShowDB();
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

				try
				{
					string answer = await client.GetStringAsync("https://localhost:5001/start?imageFolder=" + imageFolder);
					var images = JsonConvert.DeserializeObject<ImmutableList<Tuple<string, byte[]>>>(answer);

					foreach (var image in images)
					{
						Bitmap bmp;
						using (var ms = new MemoryStream(image.Item2))
						{
							bmp = new Bitmap(ms);
						}

						imageDictionary = imageDictionary.SetItem(image.Item1, BitmapToBitmapImage(bmp));
					}

					ImageListBox.ItemsSource = imageDictionary.ToImmutableList();
				}
				catch (HttpRequestException ex)
				{
					MessageBox.Show(ex.Message, "Server Error", MessageBoxButton.OK, MessageBoxImage.Error);
				}

				startButton.Content = "Start";
			}
			else
			{
				startButton.Content = "Start";
				try
				{
					await client.GetAsync("https://localhost:5001/stop");
				}
				catch (HttpRequestException ex)
				{
					MessageBox.Show(ex.Message, "Server Error", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}

			ShowDB();

			openButton.IsEnabled = true;
			resetButton.IsEnabled = true;
		}

		private async void ResetButton(object sender, RoutedEventArgs args)
		{
			try
			{
				openButton.IsEnabled = false;
				startButton.IsEnabled = false;

				await client.DeleteAsync("https://localhost:5001/clear");
				imageDictionary = imageDictionary.Clear();
			} 
			catch (HttpRequestException ex)
			{
				MessageBox.Show(ex.Message, "Server Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}

			ShowDB();

			openButton.IsEnabled = true;
			startButton.IsEnabled = true;
		}
	}
}
