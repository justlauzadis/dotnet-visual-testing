using System;

namespace DotNetVisualTesting.Core
{
    public class VisualTestFailedException : Exception
    {
        public VisualTestFailedException(string message) : base(message) {}

        public VisualTestFailedException(string message, Exception exception) : base(message, exception) {}
    }
}
