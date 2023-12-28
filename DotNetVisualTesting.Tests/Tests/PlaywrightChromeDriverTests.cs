using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DotNetVisualTesting.Playwright;
using NUnit.Framework;
using Microsoft.Playwright;
using SkiaSharp;

namespace DotNetVisualTesting.Tests.Tests
{
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public class PlaywrightChromeDriverTests : TestsBase
    {
        private const string DriverPlatform = "playwright";
        private const string BrowserName = "chrome";

        private IPlaywright _playwright;
        private IBrowser _browser;
        
        private sealed class TestScope : IDisposable {
            public IPage Page { get; }
            
            public TestScope(IBrowser browser) {
                Page = browser.NewPageAsync().Result;
                Page.SetViewportSizeAsync(1920, 1000);
            }

            public void Dispose() {
                Page.CloseAsync().Wait();
            }
        }

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            _playwright = Microsoft.Playwright.Playwright.CreateAsync().Result;
            _browser = _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true
            }).Result;
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _browser.CloseAsync().Wait();
        }
        
        [Test]
        public void CreatesBaselineImageIfItDoesNotExist_Viewport()
        {
            using var scope = new TestScope(_browser);
            var imageName = $"{DriverPlatform}_{BrowserName}_create_baseline_test";
            var baselineImagePath = GetBaselineImagePath(imageName);

            DeleteBaselineImageFile(imageName);
            
            Assert.False(File.Exists(baselineImagePath), "Unexpected baseline image found.");

            scope.Page.GotoAsync(PathToTestPage).Wait();
            VisualTestHelper.InitTest(scope.Page, imageName).Test();
            
            Assert.True(File.Exists(baselineImagePath), "Expected baseline image not created.");
            Assert.AreEqual(
                File.OpenRead(GetExpectedImagePath(imageName)),
                File.OpenRead(GetBaselineImagePath(imageName)),
                "Created image not equal to expected image.");
        }
        
        [Test]
        public void CreatesBaselineImageIfItDoesNotExist_FullPage()
        {
            using var scope = new TestScope(_browser);
            var imageName = $"{DriverPlatform}_{BrowserName}_create_full_page_baseline_test";
            var baselineImagePath = GetBaselineImagePath(imageName);

            DeleteBaselineImageFile(imageName);
            
            Assert.False(File.Exists(baselineImagePath), "Unexpected baseline image found.");
            
                scope.Page.GotoAsync(PathToTestPage).Wait();
            VisualTestHelper.InitTest(scope.Page, imageName)
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
            using var scope = new TestScope(_browser);
            var imageName = $"{DriverPlatform}_{BrowserName}_create_element_scope_baseline_test";
            var baselineImagePath = GetBaselineImagePath(imageName);

            DeleteBaselineImageFile(imageName);
            
            Assert.False(File.Exists(baselineImagePath), "Unexpected baseline image found.");
            
            scope.Page.GotoAsync(PathToTestPage).Wait();
            var sectionElement = scope.Page.QuerySelectorAsync("xpath=.//div[contains(@class,'panel-default') and .//a[contains(.,'APDEX')]]").Result;
            VisualTestHelper.InitTest(scope.Page, imageName)
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
            using var scope = new TestScope(_browser);
            var imageName = $"{DriverPlatform}_{BrowserName}_create_with_ignored_element_baseline_test";
            var baselineImagePath = GetBaselineImagePath(imageName);

            DeleteBaselineImageFile(imageName);
            
            Assert.False(File.Exists(baselineImagePath), "Unexpected baseline image found.");
            
            scope.Page.GotoAsync(PathToTestPage).Wait();
            var sectionElement1 = scope.Page.QuerySelectorAsync("xpath=.//div[contains(@class,'panel-default') and .//a[contains(.,'APDEX')]]").Result;
            var sectionElement2 = scope.Page.QuerySelectorAsync("xpath=.//div[contains(@class,'panel-default') and .//p[contains(.,'Requests Summary')]]").Result;
            VisualTestHelper.InitTest(scope.Page, imageName)
                .SetIgnoredElements(new List<(IElementHandle, SKColor)>
                {
                    (sectionElement1, SKColors.Black),
                    (sectionElement2, SKColors.Green)
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
            using var scope = new TestScope(_browser);
            var imageName = $"{DriverPlatform}_{BrowserName}_create_diff_test";
            
            DeleteAllDiffImageFiles(imageName);
            Assert.AreEqual(0, GetDiffImageFiles(imageName).Count, "Unable to delete images.");
            Assert.True(File.Exists(GetBaselineImagePath(imageName)));

            scope.Page.GotoAsync(PathToTestPage).Wait();

            var visualTestThrowsException = false;
            try
            {
                VisualTestHelper.InitTest(scope.Page, imageName).Test();
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
            using var scope = new TestScope(_browser);
            var imageName = $"{DriverPlatform}_{BrowserName}_create_compares_all_corners_diff_test";
            
            DeleteAllDiffImageFiles(imageName);
            Assert.AreEqual(0, GetDiffImageFiles(imageName).Count, "Unable to delete images.");
            Assert.True(File.Exists(GetBaselineImagePath(imageName)));

            scope.Page.GotoAsync(PathToTestPage).Wait();

            var visualTestThrowsException = false;
            try
            {
                VisualTestHelper.InitTest(scope.Page, imageName).Test();
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
            using var scope = new TestScope(_browser);
            var imageName = $"{DriverPlatform}_{BrowserName}_do_not_create_exact_match_diff_test";
            
            DeleteAllDiffImageFiles(imageName);
            Assert.AreEqual(0, GetDiffImageFiles(imageName).Count, "Unable to delete images.");
            Assert.True(File.Exists(GetBaselineImagePath(imageName)));
            
            scope.Page.GotoAsync(PathToTestPage).Wait();
            VisualTestHelper.InitTest(scope.Page, imageName)
                .UseFullPageScreenshot()
                .Test();
            
            Assert.AreEqual(0, GetDiffImageFiles(imageName).Count, "Unexpected diff image created.");
        }
        
        [Test]
        public void DoesNotCreateDiffImage_UnderTolerance()
        {
            using var scope = new TestScope(_browser);
            var imageName = $"{DriverPlatform}_{BrowserName}_do_not_create_under_tolerance_diff_test";
            
            DeleteAllDiffImageFiles(imageName);
            Assert.AreEqual(0, GetDiffImageFiles(imageName).Count, "Unable to delete images.");
            Assert.True(File.Exists(GetBaselineImagePath(imageName)));
            
            scope.Page.GotoAsync(PathToTestPage).Wait();
            VisualTestHelper.InitTest(scope.Page, imageName)
                .SetTolerance(0.08)
                .Test();
            
            Assert.AreEqual(0, GetDiffImageFiles(imageName).Count, "Unexpected diff image created.");
        }
        
        [Test]
        public void DoesNotCreateDiffImage_UnderAbsoluteTolerance()
        {
            using var scope = new TestScope(_browser);
            var imageName = $"{DriverPlatform}_{BrowserName}_do_not_create_under_tolerance_diff_test";
            
            DeleteAllDiffImageFiles(imageName);
            Assert.AreEqual(0, GetDiffImageFiles(imageName).Count, "Unable to delete images.");
            Assert.True(File.Exists(GetBaselineImagePath(imageName)));
            
            scope.Page.GotoAsync(PathToTestPage).Wait();
            VisualTestHelper.InitTest(scope.Page, imageName)
                .SetAbsoluteTolerance(152090)
                .Test();
            
            Assert.AreEqual(0, GetDiffImageFiles(imageName).Count, "Unexpected diff image created.");
        }
    }
}