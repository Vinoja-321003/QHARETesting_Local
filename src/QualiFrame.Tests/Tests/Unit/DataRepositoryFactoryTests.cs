using NUnit.Framework;
using QualiFrame.Framework.Data.Repositories;
using System;

namespace QualiFrame.Tests.Tests.Unit
{
    /// <summary>
    /// Unit tests for the DataRepositoryFactory class.
    /// </summary>
    [TestFixture]
    public class DataRepositoryFactoryTests
    {
        [Test]
        public void CreateRepository_WithExcelType_ShouldReturnExcelRepository()
        {
            // Act
            var repository = DataRepositoryFactory.CreateRepository("Excel");

            // Assert
            Assert.That(repository, Is.Not.Null);
            Assert.That(repository, Is.InstanceOf<ExcelDataRepository>());
        }

        [Test]
        public void CreateRepository_WithCsvType_ShouldReturnCsvRepository()
        {
            // Act
            var repository = DataRepositoryFactory.CreateRepository("CSV");

            // Assert
            Assert.That(repository, Is.Not.Null);
            Assert.That(repository, Is.InstanceOf<CsvDataRepository>());
        }

        [Test]
        public void CreateRepository_WithJsonType_ShouldReturnJsonRepository()
        {
            // Act
            var repository = DataRepositoryFactory.CreateRepository("JSON");

            // Assert
            Assert.That(repository, Is.Not.Null);
            Assert.That(repository, Is.InstanceOf<JsonDataRepository>());
        }

        [Test]
        public void CreateRepository_WithSqlType_ShouldReturnSqlRepository()
        {
            var repository = DataRepositoryFactory.CreateRepository("SQL");

            Assert.That(repository, Is.Not.Null);
            Assert.That(repository, Is.InstanceOf<SqlDataRepository>());
        }

        [Test]
        public void CreateRepository_WithSqlServerAlias_ShouldReturnSqlRepository()
        {
            Assert.That(DataRepositoryFactory.CreateRepository("SQLSERVER"), Is.InstanceOf<SqlDataRepository>());
            Assert.That(DataRepositoryFactory.CreateRepository("mssql"), Is.InstanceOf<SqlDataRepository>());
        }

        [Test]
        public void CreateRepository_WithInvalidType_ShouldReturnDefaultExcelRepository()
        {
            // Act
            var repository = DataRepositoryFactory.CreateRepository("Invalid");

            // Assert - Should default to Excel
            Assert.That(repository, Is.Not.Null);
            Assert.That(repository, Is.InstanceOf<ExcelDataRepository>());
        }

        [Test]
        public void CreateRepository_WithCaseInsensitiveType_ShouldWork()
        {
            // Act
            var repository1 = DataRepositoryFactory.CreateRepository("excel");
            var repository2 = DataRepositoryFactory.CreateRepository("Excel");
            var repository3 = DataRepositoryFactory.CreateRepository("EXCEL");

            // Assert
            Assert.That(repository1, Is.InstanceOf<ExcelDataRepository>());
            Assert.That(repository2, Is.InstanceOf<ExcelDataRepository>());
            Assert.That(repository3, Is.InstanceOf<ExcelDataRepository>());
        }

        [Test]
        public void CreateRepository_WithNullType_ShouldReturnDefaultExcelRepository()
        {
            // Act
            var repository = DataRepositoryFactory.CreateRepository(null);

            // Assert - Should default to Excel
            Assert.That(repository, Is.Not.Null);
            Assert.That(repository, Is.InstanceOf<ExcelDataRepository>());
        }
    }
}

