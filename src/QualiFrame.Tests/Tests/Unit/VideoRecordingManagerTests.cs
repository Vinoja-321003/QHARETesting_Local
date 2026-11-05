using NUnit.Framework;
using QualiFrame.Framework.Utilities.WebDriver;

namespace QualiFrame.Tests.Tests.Unit
{
    [TestFixture]
    public class VideoRecordingManagerTests
    {
        [Test]
        public void Stop_WhenNotStarted_ShouldNotThrow()
        {
            Assert.DoesNotThrow((Action)(() => VideoRecordingManager.Stop()));
        }

        [Test]
        public void TryStart_WhenVideoRecordingDisabled_ShouldNotThrow()
        {
            Assert.DoesNotThrow((Action)(() => VideoRecordingManager.TryStart(null!)));
            Assert.DoesNotThrow((Action)(() => VideoRecordingManager.Stop()));
        }
    }
}
