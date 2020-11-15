using Xunit;

namespace Marionet.Core.Test
{
    public class ExtensionsTest
    {
        [Fact]
        public void TestNameNormalization()
        {
            // The normalization is primarily used for equality checks; therefore the same is done here.
            Assert.Equal("name".NormalizeDesktopName(), "name".NormalizeDesktopName());
            Assert.Equal("name-1".NormalizeDesktopName(), "name-1".NormalizeDesktopName());
            Assert.Equal("name-1".NormalizeDesktopName(), "NaME-1".NormalizeDesktopName());
            Assert.Equal("name-1".NormalizeDesktopName(), "NAME-1".NormalizeDesktopName());
        }
    }
}
