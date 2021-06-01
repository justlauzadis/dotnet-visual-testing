using System.Collections.Generic;
using OpenQA.Selenium;

namespace DotNetVisualTesting.Selenium.ChromeDriver
{
    public static class ChromeDriverHelpers
    {
        public static Screenshot GetFullSizeScreenshot(this OpenQA.Selenium.Chrome.ChromeDriver driver)
        {
            var metrics = new Dictionary<string, object>(); 
            metrics["width"] = driver.ExecuteScript("return Math.max(window.innerWidth,document.body.scrollWidth,document.documentElement.scrollWidth)");
            metrics["height"] = driver.ExecuteScript("return Math.max(window.innerHeight,document.body.scrollHeight,document.documentElement.scrollHeight)");
            metrics["deviceScaleFactor"] = double.Parse(driver.ExecuteScript("return window.devicePixelRatio").ToString());
            metrics["mobile"] = driver.ExecuteScript("return typeof window.orientation !== 'undefined'");
            driver.ExecuteChromeCommand("Emulation.setDeviceMetricsOverride", metrics);
            var screenshot = driver.GetScreenshot();
            driver.ExecuteChromeCommand("Emulation.clearDeviceMetricsOverride", new Dictionary<string, object>());
            return screenshot;
        }
    }
}
