using SkiaSharp;
using System.IO;

namespace DotNetVisualTesting.Core
{
    public static class BitmapHelpers
    {
        public static void SaveAsPng(this SKBitmap bitmap, string path)
        {
            using (var data = bitmap.Encode(SKEncodedImageFormat.Png, 100))
            using (var stream = File.OpenWrite(path))
            {
                data.SaveTo(stream);
            }
        }
    }
}
