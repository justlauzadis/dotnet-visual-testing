using System.Collections.Generic;
using System.Drawing;
using System.IO;
using DotNetVisualTesting.Core;
using OpenQA.Selenium;

namespace DotNetVisualTesting.Selenium.ChromeDriver
{
    public class SeleniumChromeDriverVisualTestBuilder : VisualTestBuilderBase
    {
        public SeleniumChromeDriverVisualTestBuilder(IWebDriver driver, string name) :
            base(name,
                () => new Bitmap(new MemoryStream(((ITakesScreenshot) driver).GetScreenshot().AsByteArray)),
                () => new Bitmap(new MemoryStream(((OpenQA.Selenium.Chrome.ChromeDriver) driver)
                    .GetFullSizeScreenshot().AsByteArray)))
        {}

        public SeleniumChromeDriverVisualTestBuilder SetWebElement(IWebElement element)
        {
            SetViewportRectangle(new Rectangle(element.Location, element.Size));
            return this;
        }

        public SeleniumChromeDriverVisualTestBuilder SetIgnoredElements(List<(IWebElement, Color)> ignoredElements)
        {
            var ignoredRectangles = new List<(Rectangle, Color)>();
            foreach (var (element, color) in ignoredElements)
            {
                ignoredRectangles.Add((new Rectangle(element.Location, element.Size), color));
            }

            SetIgnoredRectangles(ignoredRectangles);
            return this;
        }
    }
}
