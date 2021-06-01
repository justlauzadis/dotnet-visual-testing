using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;

namespace DotNetVisualTesting.Core
{
    public abstract class VisualTestBuilderBase
    {
        private readonly string _baselineDir;
        private readonly string _diffDir;
        private readonly string _name;
        private readonly Func<Bitmap> _getViewportScreenshotFunc;
        private readonly Func<Bitmap> _getFullSizeScreenshotFunc;

        private int _timeoutInSeconds = 10;
        private double _tolerance = 0.0001;
        private int _absoluteToleranceInPixels = 100;
        private Rectangle? _scopeRectangle;
        private List<(Rectangle, Color)> _ignoredRectangles;
        private bool _useFullPageScreenshot;

        private string BaselineImgPath => Path.Combine(_baselineDir, _name + ".png");
        private string DiffImgPath => Path.Combine(_diffDir, $"{_name}_{DateTime.Now:yyyyMMdd_HHmmss}.png");

        protected VisualTestBuilderBase(string name, Func<Bitmap> getViewportScreenshotFunc, Func<Bitmap> getFullSizeScreenshotFunc)
        {
            _name = name;
            _getViewportScreenshotFunc = getViewportScreenshotFunc;
            _getFullSizeScreenshotFunc = getFullSizeScreenshotFunc;

            _baselineDir = Path.Combine(DirectoryHelpers.GetProjectDirectory(), "Visual", "Baselines");
            _diffDir = Path.Combine(DirectoryHelpers.GetProjectDirectory(), "Visual", "Diff");

            if (!Directory.Exists(_baselineDir))
            {
                Directory.CreateDirectory(_baselineDir);
            }

            if (!Directory.Exists(_diffDir))
            {
                Directory.CreateDirectory(_diffDir);
            }
        }

        public VisualTestBuilderBase SetTimeout(int timeoutInSeconds)
        {
            _timeoutInSeconds = timeoutInSeconds;
            return this;
        }

        public VisualTestBuilderBase SetTolerance(double tolerance)
        {
            _tolerance = tolerance;
            return this;
        }
        
        public VisualTestBuilderBase SetAbsoluteTolerance(int toleranceInPixels)
        {
            _absoluteToleranceInPixels = toleranceInPixels;
            return this;
        }

        protected VisualTestBuilderBase SetViewportRectangle(Rectangle scopeRectangle)
        {
            _scopeRectangle = scopeRectangle;
            return this;
        }

        protected VisualTestBuilderBase SetIgnoredRectangles(List<(Rectangle, Color)> ignoredElements)
        {
            _ignoredRectangles = ignoredElements;
            return this;
        }

        public VisualTestBuilderBase UseFullPageScreenshot(bool useFullPageScreenshot = true)
        {
            _useFullPageScreenshot = useFullPageScreenshot;
            return this;
        }

        public void Test()
        {
            var shouldSaveDiff = false;
            VisualTestResult visualTestResult = null;
            Bitmap actualImg = null;
            Bitmap baselineImg = null;
            try
            {
                WaitHelpers.WaitFor(() =>
                {
                    try
                    {
                        if (File.Exists(BaselineImgPath))
                        {
                            actualImg = GetAndProcessScreenshot();
                            baselineImg = new Bitmap(BaselineImgPath);
                            visualTestResult = GetVisualTestResult(actualImg, baselineImg);

                            if ((double) visualTestResult.DiffPixelsCount / (baselineImg.Height * baselineImg.Width) > _tolerance && visualTestResult.DiffPixelsCount > _absoluteToleranceInPixels)
                            {
                                shouldSaveDiff = true;
                                return (false, new VisualTestFailedException($@"Visual test failed ({visualTestResult.DiffPixelsCount} diff px) - please review the diff image"));
                            }

                            shouldSaveDiff = false;
                            return (true, null);
                        }

                        Thread.Sleep(_timeoutInSeconds * 1000);
                        actualImg = GetAndProcessScreenshot();
                        actualImg.Save(BaselineImgPath, ImageFormat.Png);

                        return (true, null);
                    }
                    catch (VisualTestFailedException ex)
                    {
                        return (false, ex);
                    }

                }, _timeoutInSeconds);
            }
            catch (Exception)
            {
                if (shouldSaveDiff && visualTestResult?.DiffImage != null)
                {
                    using var joinedImg1 = JoinImages(baselineImg, visualTestResult.DiffImage);
                    using var joinedImg2 = JoinImages(joinedImg1, actualImg);
                    joinedImg2.Save(DiffImgPath, ImageFormat.Png);
                }
                throw;
            }
            finally
            {
                actualImg?.Dispose();
                baselineImg?.Dispose();
                visualTestResult?.Dispose();
            }
        }

        private Bitmap GetAndProcessScreenshot()
        {
            var actualImg = _useFullPageScreenshot
                ? _getFullSizeScreenshotFunc.Invoke()
                : _getViewportScreenshotFunc.Invoke();

            if (_ignoredRectangles != null && _ignoredRectangles.Any())
            {
                foreach (var (ignoredElement, color) in _ignoredRectangles)
                {
                    var ignoredElementLocationX = ignoredElement.Location.X;
                    var ignoredElementLocationY = ignoredElement.Location.Y;
                    var ignoredElementWidth = ignoredElement.Size.Width;
                    var ignoredElementHeight = ignoredElement.Size.Height;

                    var actualImgWidth = actualImg.Size.Width;
                    var actualImgHeight = actualImg.Size.Height; 

                    for (var x = ignoredElementLocationX;
                        x < ignoredElementLocationX + ignoredElementWidth;
                        x++)
                    {
                        for (var y = ignoredElementLocationY;
                            y < ignoredElementLocationY + ignoredElementHeight;
                            y++)
                        {
                            if (x < actualImgWidth && y < actualImgHeight)
                            {
                                actualImg.SetPixel(x, y, color);
                            }
                        }
                    }
                }
            }

            if (_scopeRectangle != null)
            {
                try
                {
                    actualImg = actualImg.Clone((Rectangle) _scopeRectangle, actualImg.PixelFormat);
                }
                catch (ArgumentException e)
                {
                    throw new VisualTestFailedException(
                        "Visual test throws ArgumentException - consider using full page screenshot.", e);
                }
            }

            return actualImg;
        }

        private static VisualTestResult GetVisualTestResult(Bitmap img1, Bitmap img2)
        {
            var diffColor = Color.Red;

            var s1 = img1.Size;
            var s2 = img2.Size;

            
            var verticalOffsets = s1.Height != s2.Height ? new List<int> {0, s1.Height - s2.Height} : new List<int>{0};
            var horizontalOffsets = s1.Width != s2.Width ? new List<int> {0, s1.Width - s2.Width} : new List<int>{0};

            var maxImgWidth = s1.Width > s2.Width ? s1.Width : s2.Width;
            var maxImgHeight = s1.Height > s2.Height ? s1.Height : s2.Height;

            var finalDiffImage = new Bitmap(maxImgWidth, maxImgHeight);
            var finalDiffPixelsCount = long.MaxValue;

            foreach (var verticalOffset in verticalOffsets)
            {
                foreach (var horizontalOffset in horizontalOffsets)
                {
                    using var diffImg = new Bitmap(maxImgWidth, maxImgHeight);
                    long diffPixelsCount = 0;
                    for (var y = 0; y < maxImgHeight; y++)
                    {
                        for (var x = 0; x < maxImgWidth; x++)
                        {
                            try
                            {
                                var c1 = img1.GetPixel(x, y);
                                var c2 = img2.GetPixel(x - horizontalOffset, y - verticalOffset);
                        
                                if (c1 == c2)
                                {
                                    var rgbAverage = (c1.R + c1.G + c1.B) / 3;
                                    var grayscaleColor = Color.FromArgb(rgbAverage, rgbAverage, rgbAverage);
                                    diffImg.SetPixel(x, y, grayscaleColor);
                                }
                                else
                                {
                                    diffImg.SetPixel(x, y, diffColor);
                                    diffPixelsCount++;
                                }
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                diffImg.SetPixel(x, y, diffColor);
                                diffPixelsCount++;
                            }
                        }
                    }

                    if (diffPixelsCount < finalDiffPixelsCount)
                    {
                        finalDiffImage = (Bitmap) diffImg.Clone();
                        finalDiffPixelsCount = diffPixelsCount;
                    }
                }
            }
            
            return new VisualTestResult(finalDiffImage, finalDiffPixelsCount);
        }

        private static Bitmap JoinImages(Bitmap img1, Bitmap img2)
        {
            var imgHeight = img1.Size.Height > img2.Size.Height ? img1.Size.Height : img2.Size.Height;
            var newImg = new Bitmap(img1.Width + img2.Width, imgHeight);
            for (var y = 0; y < imgHeight; y++)
            {
                for (var x = 0; x < img1.Width; x++)
                {
                    try
                    {
                        newImg.SetPixel(x, y, img1.GetPixel(x, y));
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        newImg.SetPixel(x, y, Color.White);
                    }
                }

                for (var x = 0; x < img2.Width; x++)
                {
                    try
                    {
                        newImg.SetPixel(img1.Width + x, y, img2.GetPixel(x, y));
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        newImg.SetPixel(img1.Width + x, y, Color.White);
                    }
                }
            }

            return newImg;
        }
    }
}
