namespace ImageRecognitionComponent
{
	public class Result
	{
		public float[] BBox { get; }

		public string Label { get; }

		public float Confidence { get; }

		public Result(float[] bbox, string label, float confidence)
		{
			BBox = bbox;
			Label = label;
			Confidence = confidence;
		}
	}
}
