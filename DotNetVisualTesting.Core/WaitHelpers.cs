using System;
using System.Threading;

namespace DotNetVisualTesting.Core
{
    public static class WaitHelpers
    {
        private const double DefaultTimeoutInSeconds = 10;
        public static void WaitFor(Func<(bool result, Exception exception)> action, double timeoutInSecs = DefaultTimeoutInSeconds)
        {
            var start = DateTime.Now;
            var actionResult = false;
            Exception actionException = null;
            var iteration = 0;

            while ((DateTime.Now - start).TotalSeconds < timeoutInSecs || iteration < 1)
            {
                var outcome = action();
                actionResult = outcome.result;
                actionException = outcome.exception;

                if (actionResult)
                {
                    break;
                }

                Thread.Sleep(500);
                iteration++;
            }

            if (!actionResult)
            {
                var outputMessage = $"Action returned false after timeout (in {iteration} tries)";
                Exception outputException = actionException == null
                    ? new TimeoutException(outputMessage)
                    : new TimeoutException(outputMessage, actionException);
                throw outputException;
            }
        }
    }
}
