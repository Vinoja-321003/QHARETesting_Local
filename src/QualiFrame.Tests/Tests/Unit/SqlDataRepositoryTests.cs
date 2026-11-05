using NUnit.Framework;
using QualiFrame.Framework.Core.Exceptions;
using QualiFrame.Framework.Data.Repositories;

namespace QualiFrame.Tests.Tests.Unit
{
    [TestFixture]
    public class SqlDataRepositoryTests
    {
        [Test]
        public void GetTestData_WithoutConnectionString_ShouldThrowTestDataException()
        {
            var repository = new SqlDataRepository();

            var ex = Assert.Throws<TestDataException>((Action)(() => repository.GetTestData("TC-1")));

            Assert.That(ex, Is.Not.Null);
            Assert.That(ex!.Message, Does.Contain("DatabaseConnectionString"));
        }

        [Test]
        public void GetAllTestData_WithInvalidTableName_ShouldThrowTestDataException()
        {
            var repository = new SqlDataRepository();

            var ex = Assert.Throws<TestDataException>((Action)(() => repository.GetAllTestData("Test Data; DROP TABLE")));

            Assert.That(ex, Is.Not.Null);
            Assert.That(ex!.Message, Does.Contain("Invalid SQL"));
        }

        [Test]
        public void TestDataExists_WithoutConnectionString_ShouldReturnFalse()
        {
            var repository = new SqlDataRepository();

            Assert.That(repository.TestDataExists("TC-1"), Is.False);
        }
    }
}
