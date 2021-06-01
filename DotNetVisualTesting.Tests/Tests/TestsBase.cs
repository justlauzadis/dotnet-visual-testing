using System.Collections.Generic;
using System.IO;
using System.Linq;
using DotNetVisualTesting.Core;
using DotNetVisualTesting.Selenium.ChromeDriver;

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
    }
}