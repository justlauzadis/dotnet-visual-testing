using System.Collections.Generic;
using System.IO;
using DotNetVisualTesting.Core;
using Microsoft.Playwright;
using SkiaSharp;

namespace DotNetVisualTesting.Playwright
{
    public class PlaywrightVisualTestBuilder : VisualTestBuilderBase
    {
        public PlaywrightVisualTestBuilder(IPage page, string name) :
            base(name,
                () => SKBitmap.Decode(new MemoryStream(page.ScreenshotAsync().Result)),
                () => SKBitmap.Decode(new MemoryStream(page.ScreenshotAsync(new PageScreenshotOptions
                {
                    FullPage = true
                }).Result)))
        {}

        public PlaywrightVisualTestBuilder SetWebElement(IElementHandle element)
        {
            var rect = element.BoundingBoxAsync().Result;
            SetViewportRectangle(new SKRectI((int)rect.X, (int)rect.Y, (int)(rect.X + rect.Width), (int)(rect.Y + rect.Height)));
            return this;
        }

        public PlaywrightVisualTestBuilder SetIgnoredElements(List<(IElementHandle, SKColor)> ignoredElements)
        {
            var ignoredRectangles = new List<(SKRectI, SKColor)>();
            foreach (var (element, color) in ignoredElements)
            {
                var rect = element.BoundingBoxAsync().Result;
                ignoredRectangles.Add((new SKRectI((int)rect.X, (int)rect.Y, (int)(rect.X + rect.Width), (int)(rect.Y + rect.Height)), color));
            }

            SetIgnoredRectangles(ignoredRectangles);
            return this;
        }
    }
}
