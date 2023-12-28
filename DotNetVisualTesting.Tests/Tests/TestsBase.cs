using System.Collections.Generic;
using System.IO;
using System.Linq;
using DotNetVisualTesting.Core;
using NUnit.Framework;
using SkiaSharp;

namespace DotNetVisualTesting.Tests.Tests
{
    public abstract class TestsBase
    {
        protected readonly string PathToTestPage =
            Path.Combine(DirectoryHelpers.GetProjectDirectory(), "Data", "TestPage", "index.html");
        
        protected string GetBaselineImagePath(string imageName)
        {
            return Path.Combine(DirectoryHelpers.GetProjectDirectory(),
                "Visual", "Baselines", $"{imageName}.png");
        }
        
        protected string GetDiffImagePath(string imageName)
        {
            return Path.Combine(DirectoryHelpers.GetProjectDirectory(),
                "Visual", "Diff", $"{imageName}.png");
        }
        
        protected string GetExpectedImagePath(string imageName)
        {
            return Path.Combine(DirectoryHelpers.GetProjectDirectory(),
                "Visual", "Expected", $"{imageName}.png");
        }

        protected void DeleteBaselineImageFile(string imageName)
        {
            var baselineImagePath = GetBaselineImagePath(imageName);

            if (File.Exists(baselineImagePath))
            {
                File.Delete(baselineImagePath);
            }
        }

        protected void DeleteAllDiffImageFiles(string imageName)
        {
            var diffImages = GetDiffImageFiles(imageName);
            if (diffImages.Any())
            {
                foreach (var diffImage in diffImages)
                {
                    diffImage.Delete();
                }
            }
        }
        
        protected List<FileInfo> GetDiffImageFiles(string imageName)
        {
            return new DirectoryInfo(Path.Combine(DirectoryHelpers.GetProjectDirectory(), "Visual", "Diff"))
                .EnumerateFiles($"{imageName}*.png").ToList();
        }

        protected void AssertBitmapsAreEqual(SKBitmap bitmap1, SKBitmap bitmap2)
        {
            var width1 = bitmap1.Width;
            var heigth1 = bitmap1.Height;
            var width2 = bitmap2.Width;
            var heigth2 = bitmap2.Height;

            Assert.AreEqual(width1, width2, $"Widths are different. width1={width1}, width2={width2}.");
            Assert.AreEqual(heigth1, heigth2, $"Heights are different. heigth1={heigth1}, heigth2={heigth2}.");

            for (var i = 0; i < width1; i++)
            {
                for (var j = 0; j < heigth1; j++)
                {
                    Assert.AreEqual(bitmap1.GetPixel(i, j).Red, bitmap2.GetPixel(i, j).Red);
                    Assert.AreEqual(bitmap1.GetPixel(i, j).Green, bitmap2.GetPixel(i, j).Green);
                    Assert.AreEqual(bitmap1.GetPixel(i, j).Blue, bitmap2.GetPixel(i, j).Blue);
                }
            }
        }
    }
}