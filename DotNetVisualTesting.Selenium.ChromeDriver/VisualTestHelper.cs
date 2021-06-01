using OpenQA.Selenium;

namespace DotNetVisualTesting.Selenium.ChromeDriver
{
    public static class VisualTestHelper
    {
        public static SeleniumChromeDriverVisualTestBuilder InitTest(IWebDriver driver, string name)
        {
            return new SeleniumChromeDriverVisualTestBuilder(driver, name);
        }
    }
}
