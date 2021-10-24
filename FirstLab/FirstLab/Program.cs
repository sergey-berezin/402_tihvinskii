using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ImageRecognitionComponent;

namespace FirstLab
{
	class Program
	{
		static async Task Main(string[] args)
		{
			if (args.Length == 1)
			{
				string imageFolder = args[0];

				if (Directory.Exists(imageFolder))
				{
					using (var cts = new CancellationTokenSource())
					{
						Console.CancelKeyPress += (sender, args) =>
						{
							cts.Cancel();
							args.Cancel = true;
						};

						await foreach (Result result in Component.ImageProcessAsync(imageFolder, cts))
						{
							Console.WriteLine(result.FileName + " - " + result.Label + " = " + result.Confidence + ": " + result.BBox[0] + " - " + result.BBox[1] + " - " + result.BBox[2] + " - " + result.BBox[3]);
						}
					}
				}
				else
				{
					Console.WriteLine("Error: the source directory does not exist!");
				}
			}
			else
			{
				Console.WriteLine("Error: the number of command-line arguments is wrong!");
			}
		}
	}
}
