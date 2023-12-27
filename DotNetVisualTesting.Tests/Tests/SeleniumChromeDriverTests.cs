using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using DotNetVisualTesting.Selenium.ChromeDriver;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace DotNetVisualTesting.Tests.Tests
{
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public class SeleniumChromeDriverTests : TestsBase
    {
        private const string DriverPlatform = "selenium";
        private const string BrowserName = "chrome";

        private sealed class TestScope : IDisposable {
            public IWebDriver WebDriver { get; }
            
            public TestScope() {
                var options = new ChromeOptions();
            
                // Hide scrollbars for accurate cropping in visual testing
                options.AddArgument("--hide-scrollbars");
                
                // Resolves crashing on big full size screenshots
                options.AddArgument("--disable-dev-shm-usage");
            
                options.AddArgument("--headless");
            
                options.AddArgument("--window-size=1920,1000");
                WebDriver = new ChromeDriver(options);
            }

            public void Dispose() {
                WebDriver.Quit();
                WebDriver.Dispose();
            }
        }

        [Test]
        public void CreatesBaselineImageIfItDoesNotExist_Viewport()
        {
            using var scope = new TestScope();
            var imageName = $"{DriverPlatform}_{BrowserName}_create_baseline_test";
            var baselineImagePath = GetBaselineImagePath(imageName);

            DeleteBaselineImageFile(imageName);
            
            Assert.False(File.Exists(baselineImagePath), "Unexpected baseline image found.");
            
            scope.WebDriver.Navigate().GoToUrl(PathToTestPage);
            VisualTestHelper.InitTest(scope.WebDriver, imageName).Test();
            
            Assert.True(File.Exists(baselineImagePath), "Expected baseline image not created.");
            Assert.AreEqual(
                File.OpenRead(GetExpectedImagePath(imageName)),
                File.OpenRead(GetBaselineImagePath(imageName)),
                "Created image not equal to expected image.");
        }
        
        [Test]
        public void CreatesBaselineImageIfItDoesNotExist_FullPage()
        {
            using var scope = new TestScope();
            var imageName = $"{DriverPlatform}_{BrowserName}_create_full_page_baseline_test";
            var baselineImagePath = GetBaselineImagePath(imageName);

            DeleteBaselineImageFile(imageName);
            
            Assert.False(File.Exists(baselineImagePath), "Unexpected baseline image found.");
            
            scope.WebDriver.Navigate().GoToUrl(PathToTestPage);
            VisualTestHelper.InitTest(scope.WebDriver, imageName)
                .UseFullPageScreenshot()
                .Test();
            
            Assert.True(File.Exists(baselineImagePath), "Expected baseline image not created.");
            Assert.AreEqual(
                File.OpenRead(GetExpectedImagePath(imageName)),
                File.OpenRead(GetBaselineImagePath(imageName)),
                "Created image not equal to expected image.");
        }
        
        [Test]
        public void CreatesBaselineImageIfItDoesNotExist_ElementScope()
        {
            using var scope = new TestScope();
            var imageName = $"{DriverPlatform}_{BrowserName}_create_element_scope_baseline_test";
            var baselineImagePath = GetBaselineImagePath(imageName);

            DeleteBaselineImageFile(imageName);
            
            Assert.False(File.Exists(baselineImagePath), "Unexpected baseline image found.");
            
            scope.WebDriver.Navigate().GoToUrl(PathToTestPage);
            var sectionElement = scope.WebDriver.FindElement(By.XPath(".//div[contains(@class,'panel-default') and .//a[contains(.,'APDEX')]]"));
            VisualTestHelper.InitTest(scope.WebDriver, imageName)
                .SetWebElement(sectionElement)
                .UseFullPageScreenshot()
                .Test();
            
            Assert.True(File.Exists(baselineImagePath), "Expected baseline image not created.");
            Assert.AreEqual(
                File.OpenRead(GetExpectedImagePath(imageName)),
                File.OpenRead(GetBaselineImagePath(imageName)),
                "Created image not equal to expected image.");
        }
        
        [Test]
        public void CreatesBaselineImageIfItDoesNotExist_WithIgnoredElement()
        {
            using var scope = new TestScope();
            var imageName = $"{DriverPlatform}_{BrowserName}_create_with_ignored_element_baseline_test";
            var baselineImagePath = GetBaselineImagePath(imageName);

            DeleteBaselineImageFile(imageName);
            
            Assert.False(File.Exists(baselineImagePath), "Unexpected baseline image found.");
            
            scope.WebDriver.Navigate().GoToUrl(PathToTestPage);
            var sectionElement1 = scope.WebDriver.FindElement(By.XPath(".//div[contains(@class,'panel-default') and .//a[contains(.,'APDEX')]]"));
            var sectionElement2 = scope.WebDriver.FindElement(By.XPath(".//div[contains(@class,'panel-default') and .//p[contains(.,'Requests Summary')]]"));
            VisualTestHelper.InitTest(scope.WebDriver, imageName)
                .SetIgnoredElements(new List<(IWebElement, Color)>
                {
                    (sectionElement1, Color.Black),
                    (sectionElement2, Color.Green)
                })
                .UseFullPageScreenshot()
                .Test();
            
            Assert.True(File.Exists(baselineImagePath), "Expected baseline image not created.");
            Assert.AreEqual(
                File.OpenRead(GetExpectedImagePath(imageName)),
                File.OpenRead(GetBaselineImagePath(imageName)),
                "Created image not equal to expected image.");
        }

        [Test]
        public void CreatesDiffImageInCaseOfDiff_Viewport()
        {
            using var scope = new TestScope();
            var imageName = $"{DriverPlatform}_{BrowserName}_create_diff_test";
            
            DeleteAllDiffImageFiles(imageName);
            Assert.AreEqual(0, GetDiffImageFiles(imageName).Count, "Unable to delete images.");
            Assert.True(File.Exists(GetBaselineImagePath(imageName)));

            scope.WebDriver.Navigate().GoToUrl(PathToTestPage);

            var visualTestThrowsException = false;
            try
            {
                VisualTestHelper.InitTest(scope.WebDriver, imageName).Test();
            }
            catch (Exception)
            {
                visualTestThrowsException = true;
            }
            
            Assert.IsTrue(visualTestThrowsException, "Visual test has not failed.");
            Assert.AreEqual(1, GetDiffImageFiles(imageName).Count, "Expected diff image not created.");
            Assert.AreEqual(
                File.OpenRead(GetExpectedImagePath(imageName)),
                File.OpenRead(GetDiffImagePath(Path.GetFileNameWithoutExtension(GetDiffImageFiles(imageName).FirstOrDefault()?.Name))),
                "Created image not equal to expected image.");
        }
        
        [Test]
        public void CreatesDiffImageInCaseOfDiff_ComparesAllCorners()
        {
            using var scope = new TestScope();
            var imageName = $"{DriverPlatform}_{BrowserName}_create_compares_all_corners_diff_test";
            
            DeleteAllDiffImageFiles(imageName);
            Assert.AreEqual(0, GetDiffImageFiles(imageName).Count, "Unable to delete images.");
            Assert.True(File.Exists(GetBaselineImagePath(imageName)));

            scope.WebDriver.Navigate().GoToUrl(PathToTestPage);

            var visualTestThrowsException = false;
            try
            {
                VisualTestHelper.InitTest(scope.WebDriver, imageName).Test();
            }
            catch (Exception)
            {
                visualTestThrowsException = true;
            }
            
            Assert.IsTrue(visualTestThrowsException, "Visual test has not failed.");
            Assert.AreEqual(1, GetDiffImageFiles(imageName).Count, "Expected diff image not created.");
            Assert.AreEqual(
                File.OpenRead(GetExpectedImagePath(imageName)),
                File.OpenRead(GetDiffImagePath(Path.GetFileNameWithoutExtension(GetDiffImageFiles(imageName).FirstOrDefault()?.Name))),
                "Created image not equal to expected image.");
        }

        [Test]
        public void DoesNotCreateDiffImage_ExactMatch()
        {
            using var scope = new TestScope();
            var imageName = $"{DriverPlatform}_{BrowserName}_do_not_create_exact_match_diff_test";
            
            DeleteAllDiffImageFiles(imageName);
            Assert.AreEqual(0, GetDiffImageFiles(imageName).Count, "Unable to delete images.");
            Assert.True(File.Exists(GetBaselineImagePath(imageName)));
            
            scope.WebDriver.Navigate().GoToUrl(PathToTestPage);
            VisualTestHelper.InitTest(scope.WebDriver, imageName)
                .UseFullPageScreenshot()
                .Test();
            
            Assert.AreEqual(0, GetDiffImageFiles(imageName).Count, "Unexpected diff image created.");
        }
        
        [Test]
        public void DoesNotCreateDiffImage_UnderTolerance()
        {
            using var scope = new TestScope();
            var imageName = $"{DriverPlatform}_{BrowserName}_do_not_create_under_tolerance_diff_test";
            
            DeleteAllDiffImageFiles(imageName);
            Assert.AreEqual(0, GetDiffImageFiles(imageName).Count, "Unable to delete images.");
            Assert.True(File.Exists(GetBaselineImagePath(imageName)));
            
            scope.WebDriver.Navigate().GoToUrl(PathToTestPage);
            VisualTestHelper.InitTest(scope.WebDriver, imageName)
                .SetTolerance(0.08)
                .Test();
            
            Assert.AreEqual(0, GetDiffImageFiles(imageName).Count, "Unexpected diff image created.");
        }
        
        [Test]
        public void DoesNotCreateDiffImage_UnderAbsoluteTolerance()
        {
            using var scope = new TestScope();
            var imageName = $"{DriverPlatform}_{BrowserName}_do_not_create_under_tolerance_diff_test";
            
            DeleteAllDiffImageFiles(imageName);
            Assert.AreEqual(0, GetDiffImageFiles(imageName).Count, "Unable to delete images.");
            Assert.True(File.Exists(GetBaselineImagePath(imageName)));
            
            scope.WebDriver.Navigate().GoToUrl(PathToTestPage);
            VisualTestHelper.InitTest(scope.WebDriver, imageName)
                .SetAbsoluteTolerance(153000)
                .Test();
            
            Assert.AreEqual(0, GetDiffImageFiles(imageName).Count, "Unexpected diff image created.");
        }
    }
}