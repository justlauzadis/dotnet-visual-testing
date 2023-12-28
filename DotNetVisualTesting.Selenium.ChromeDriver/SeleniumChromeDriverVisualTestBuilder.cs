using System.Collections.Generic;
using System.Drawing;
using System.IO;
using DotNetVisualTesting.Core;
using OpenQA.Selenium;
using SkiaSharp;

namespace DotNetVisualTesting.Selenium.ChromeDriver
{
    public class SeleniumChromeDriverVisualTestBuilder : VisualTestBuilderBase
    {
        public SeleniumChromeDriverVisualTestBuilder(IWebDriver driver, string name) :
            base(name,
                () => SKBitmap.Decode(new MemoryStream(((ITakesScreenshot) driver).GetScreenshot().AsByteArray)),
                () => SKBitmap.Decode(new MemoryStream(((OpenQA.Selenium.Chrome.ChromeDriver) driver)
                    .GetFullSizeScreenshot().AsByteArray)))
        {}

        public SeleniumChromeDriverVisualTestBuilder SetWebElement(IWebElement element)
        {
            var location = element.Location;
            var size = element.Size;
            SetViewportRectangle(new SKRectI(location.X, location.Y, location.X + size.Width, location.Y + size.Height));
            return this;
        }

        public SeleniumChromeDriverVisualTestBuilder SetIgnoredElements(List<(IWebElement, SKColor)> ignoredElements)
        {
            var ignoredRectangles = new List<(SKRectI, SKColor)>();
            foreach (var (element, color) in ignoredElements)
            {
                var location = element.Location;
                var size = element.Size;
                ignoredRectangles.Add((new SKRectI(location.X, location.Y, location.X + size.Width, location.Y + size.Height), color));
            }

            SetIgnoredRectangles(ignoredRectangles);
            return this;
        }
    }
}
