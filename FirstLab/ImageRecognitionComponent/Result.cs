namespace ImageRecognitionComponent
{
	public class Result
	{
		public float[] BBox { get; }

		public string Label { get; }

		public float Confidence { get; }

		public string FileName { get; }

		public Result(float[] bbox, string label, float confidence, string fileName)
		{
			BBox = bbox;
			Label = label;
			Confidence = confidence;
			FileName = fileName;
		}
	}
}
