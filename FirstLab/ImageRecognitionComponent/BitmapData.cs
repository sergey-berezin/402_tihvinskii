using System.Drawing;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms.Image;

namespace ImageRecognitionComponent
{
    public class BitmapData
    {
        [ColumnName("bitmap")]
        [ImageType(416, 416)]
        public Bitmap Image { get; set; }

        [ColumnName("width")]
        public float ImageWidth => Image.Width;

        [ColumnName("height")]
        public float ImageHeight => Image.Height;
    }
}
