using System.Collections.Generic;
using System.Drawing;
using System.IO;
using DotNetVisualTesting.Core;
using PlaywrightSharp;

namespace DotNetVisualTesting.Playwright
{
    public class PlaywrightVisualTestBuilder : VisualTestBuilderBase
    {
        public PlaywrightVisualTestBuilder(IPage page, string name) :
            base(name,
                () => new Bitmap(new MemoryStream(page.ScreenshotAsync().Result)),
                () => new Bitmap(new MemoryStream(page.ScreenshotAsync(true).Result)))
        {}

        public PlaywrightVisualTestBuilder SetWebElement(IElementHandle element)
        {
            var rect = element.GetBoundingBoxAsync().Result;
            SetViewportRectangle(new Rectangle((int) rect.X, (int) rect.Y, (int) rect.Width, (int) rect.Height));
            return this;
        }

        public PlaywrightVisualTestBuilder SetIgnoredElements(List<(IElementHandle, Color)> ignoredElements)
        {
            var ignoredRectangles = new List<(Rectangle, Color)>();
            foreach (var (element, color) in ignoredElements)
            {
                var rect = element.GetBoundingBoxAsync().Result;
                ignoredRectangles.Add((new Rectangle((int) rect.X, (int) rect.Y, (int) rect.Width, (int) rect.Height), color));
            }

            SetIgnoredRectangles(ignoredRectangles);
            return this;
        }
    }
}
