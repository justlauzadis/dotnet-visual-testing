using PlaywrightSharp;

namespace DotNetVisualTesting.Playwright
{
    public static class VisualTestHelper
    {
        public static PlaywrightVisualTestBuilder InitTest(IPage page, string name)
        {
            return new PlaywrightVisualTestBuilder(page, name);
        }
    }
}
