using SkiaSharp;
using System;

namespace DotNetVisualTesting.Core
{
    public class VisualTestResult : IDisposable
    {
        public readonly SKBitmap DiffImage;
        public readonly long DiffPixelsCount;

        public VisualTestResult(SKBitmap diffImage, long diffPixelsCount)
        {
            DiffImage = diffImage;
            DiffPixelsCount = diffPixelsCount;
        }

        public void Dispose()
        {
            DiffImage?.Dispose();
        }
    }
}
