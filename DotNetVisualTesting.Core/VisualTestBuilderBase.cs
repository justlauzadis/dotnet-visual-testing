using SkiaSharp;
using System;
using System.Collections.Generic;
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
        private readonly Func<SKBitmap> _getViewportScreenshotFunc;
        private readonly Func<SKBitmap> _getFullSizeScreenshotFunc;

        private int _timeoutInSeconds = 10;
        private double _tolerance = 0.0001;
        private int _absoluteToleranceInPixels = 100;
        private SKRectI? _scopeRectangle;
        private List<(SKRectI, SKColor)> _ignoredRectangles;
        private bool _useFullPageScreenshot;

        private string BaselineImgPath => Path.Combine(_baselineDir, _name + ".png");
        private string DiffImgPath => Path.Combine(_diffDir, $"{_name}_{DateTime.Now:yyyyMMdd_HHmmss}.png");

        protected VisualTestBuilderBase(string name, Func<SKBitmap> getViewportScreenshotFunc, Func<SKBitmap> getFullSizeScreenshotFunc)
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

        protected VisualTestBuilderBase SetViewportRectangle(SKRectI scopeRectangle)
        {
            _scopeRectangle = scopeRectangle;
            return this;
        }

        protected VisualTestBuilderBase SetIgnoredRectangles(List<(SKRectI, SKColor)> ignoredElements)
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
            SKBitmap actualImg = null;
            SKBitmap baselineImg = null;
            try
            {
                WaitHelpers.WaitFor(() =>
                {
                    try
                    {
                        if (File.Exists(BaselineImgPath))
                        {
                            actualImg = GetAndProcessScreenshot();
                            baselineImg = SKBitmap.Decode(BaselineImgPath);
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
                        actualImg.SaveAsPng(BaselineImgPath);

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
                    joinedImg2.SaveAsPng(DiffImgPath);
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

        private SKBitmap GetAndProcessScreenshot()
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

                    var actualImgWidth = actualImg.Width;
                    var actualImgHeight = actualImg.Height; 

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
                    var X = _scopeRectangle.Value.Location.X;
                    var Y = _scopeRectangle.Value.Location.Y;
                    var width = _scopeRectangle.Value.Width;
                    var height = _scopeRectangle.Value.Height;
                    using (SKBitmap croppedImg = new SKBitmap(width, height))
                    {
                        for (var i = 0; i < width; i++)
                        {
                            for (var j = 0; j < height; j++)
                            {
                                var pixel = actualImg.GetPixel(X + i, Y + j);
                                croppedImg.SetPixel(i, j, pixel);
                            }
                        }
                        actualImg = croppedImg.Copy();
                    }
                }
                catch (ArgumentException e)
                {
                    throw new VisualTestFailedException(
                        "Visual test throws ArgumentException - consider using full page screenshot.", e);
                }
            }

            return actualImg;
        }

        private static VisualTestResult GetVisualTestResult(SKBitmap img1, SKBitmap img2)
        {
            var diffColor = SKColors.Red;

            var s1 = img1.Info.Size;
            var s2 = img2.Info.Size;
            
            var verticalOffsets = s1.Height != s2.Height ? new List<int> {0, s1.Height - s2.Height} : new List<int>{0};
            var horizontalOffsets = s1.Width != s2.Width ? new List<int> {0, s1.Width - s2.Width} : new List<int>{0};

            var maxImgWidth = s1.Width > s2.Width ? s1.Width : s2.Width;
            var maxImgHeight = s1.Height > s2.Height ? s1.Height : s2.Height;

            var finalDiffImage = new SKBitmap(maxImgWidth, maxImgHeight);
            var finalDiffPixelsCount = long.MaxValue;

            foreach (var verticalOffset in verticalOffsets)
            {
                foreach (var horizontalOffset in horizontalOffsets)
                {
                    using var diffImg = new SKBitmap(maxImgWidth, maxImgHeight);
                    long diffPixelsCount = 0;
                    for (var y = 0; y < maxImgHeight; y++)
                    {
                        for (var x = 0; x < maxImgWidth; x++)
                        {
                            // pixel out of bounds
                            if (x - horizontalOffset < 0 || x - horizontalOffset >= s2.Width || y - verticalOffset < 0 || y - verticalOffset >= s2.Height)
                            {
                                diffImg.SetPixel(x, y, diffColor);
                                diffPixelsCount++;
                                continue;
                            }

                            var c1 = img1.GetPixel(x, y);
                            var c2 = img2.GetPixel(x - horizontalOffset, y - verticalOffset);
                        
                            if (c1 == c2)
                            {
                                var rgbAverage = (c1.Red + c1.Green + c1.Blue) / 3;
                                var grayscaleColor = new SKColor((byte)rgbAverage, (byte)rgbAverage, (byte)rgbAverage);
                                diffImg.SetPixel(x, y, grayscaleColor);
                            }
                            else
                            {
                                diffImg.SetPixel(x, y, diffColor);
                                diffPixelsCount++;
                            }
                        }
                    }

                    if (diffPixelsCount < finalDiffPixelsCount)
                    {
                        finalDiffImage = diffImg.Copy();
                        finalDiffPixelsCount = diffPixelsCount;
                    }
                }
            }
            
            return new VisualTestResult(finalDiffImage, finalDiffPixelsCount);
        }

        private static SKBitmap JoinImages(SKBitmap img1, SKBitmap img2)
        {
            var imgHeight = img1.Height > img2.Height ? img1.Height : img2.Height;
            var newImg = new SKBitmap(img1.Width + img2.Width, imgHeight);
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
                        newImg.SetPixel(x, y, SKColors.White);
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
                        newImg.SetPixel(img1.Width + x, y, SKColors.White);
                    }
                }
            }

            return newImg;
        }
    }
}
