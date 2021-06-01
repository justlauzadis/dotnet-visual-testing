using System;
using System.IO;

namespace DotNetVisualTesting.Core
{
    public static class DirectoryHelpers
    {
        public static string GetWorkingDirectory() => Environment.CurrentDirectory;

        public static string GetProjectDirectory() =>
            Directory.GetParent(GetWorkingDirectory()).Parent?.Parent?.FullName;
    }
}
