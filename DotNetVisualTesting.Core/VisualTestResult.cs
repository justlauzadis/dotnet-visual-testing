using System;
using System.Drawing;

namespace DotNetVisualTesting.Core
{
    public class VisualTestResult : IDisposable
    {
        public readonly Bitmap DiffImage;
        public readonly long DiffPixelsCount;

        public VisualTestResult(Bitmap diffImage, long diffPixelsCount)
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
