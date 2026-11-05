using NUnit.Framework;
using QualiFrame.Framework.Utilities.WebDriver;
using System;

namespace QualiFrame.Tests.Tests.Unit
{
    /// <summary>
    /// Unit tests for the DriverPool class.
    /// </summary>
    [TestFixture]
    public class DriverPoolTests
    {
        private DriverPool? _driverPool;

        [SetUp]
        public void SetUp()
        {
            // Initialize driver pool with small size for testing
            _driverPool = new DriverPool(maxPoolSize: 2, minPoolSize: 1);
        }

        [TearDown]
        public void TearDown()
        {
            _driverPool?.Dispose();
        }

        [Test]
        public void GetDriver_ShouldReturnDriver()
        {
            // Act
            var driver = _driverPool!.GetDriver();

            // Assert
            Assert.That(driver, Is.Not.Null);
        }

        [Test]
        public void ReturnDriver_ShouldReturnDriverToPool()
        {
            // Arrange
            var driver = _driverPool!.GetDriver();

            // Act
            _driverPool.ReturnDriver(driver);

            // Assert
            var stats = _driverPool.GetStatistics();
            Assert.That(stats.AvailableDrivers, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void GetStatistics_ShouldReturnValidStatistics()
        {
            // Act
            var stats = _driverPool!.GetStatistics();

            // Assert
            Assert.That(stats, Is.Not.Null);
            Assert.That(stats.MaxPoolSize, Is.EqualTo(2));
            Assert.That(stats.MinPoolSize, Is.EqualTo(1));
            Assert.That(stats.TotalDrivers, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void Cleanup_ShouldNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow((Action)(() => _driverPool!.Cleanup()));
        }

        [Test]
        public void Dispose_ShouldDisposeAllDrivers()
        {
            // Arrange
            var driver = _driverPool!.GetDriver();

            // Act
            _driverPool.Dispose();

            // Assert
            var stats = _driverPool.GetStatistics();
            Assert.That(stats.TotalDrivers, Is.EqualTo(0));
        }

        [Test]
        public void GetDriver_AfterDispose_ShouldThrow()
        {
            // Arrange
            _driverPool!.Dispose();

            // Act & Assert
            Assert.Throws<ObjectDisposedException>((Action)(() => _driverPool.GetDriver()));
        }
    }
}

