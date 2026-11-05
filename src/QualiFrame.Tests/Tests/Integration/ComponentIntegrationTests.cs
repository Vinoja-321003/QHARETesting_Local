using NUnit.Framework;
using OpenQA.Selenium;
using QualiFrame.Framework.Web.Components;
using System;

namespace QualiFrame.Tests.Tests.Integration
{
    /// <summary>
    /// Integration tests for web components.
    /// </summary>
    [TestFixture]
    public class ComponentIntegrationTests : IntegrationTestBase
    {
        [Test]
        public void HeaderComponent_CanBeCreated()
        {
            // Arrange & Act
            var header = new HeaderComponent(Driver);

            // Assert
            Assert.That(header, Is.Not.Null);
            Assert.That(header.Driver, Is.EqualTo(Driver));
        }

        [Test]
        public void FooterComponent_CanBeCreated()
        {
            // Arrange & Act
            var footer = new FooterComponent(Driver);

            // Assert
            Assert.That(footer, Is.Not.Null);
            Assert.That(footer.Driver, Is.EqualTo(Driver));
        }

        [Test]
        public void FormComponent_CanBeCreated()
        {
            // Arrange & Act
            var form = new FormComponent(Driver);

            // Assert
            Assert.That(form, Is.Not.Null);
            Assert.That(form.Driver, Is.EqualTo(Driver));
        }

        [Test]
        public void ModalComponent_CanBeCreated()
        {
            // Arrange & Act
            var modal = new ModalComponent(Driver);

            // Assert
            Assert.That(modal, Is.Not.Null);
            Assert.That(modal.Driver, Is.EqualTo(Driver));
        }

        [Test]
        public void TableComponent_CanBeCreated()
        {
            // Arrange & Act
            var table = new TableComponent(Driver);

            // Assert
            Assert.That(table, Is.Not.Null);
            Assert.That(table.Driver, Is.EqualTo(Driver));
        }

        [Test]
        public void DropdownComponent_CanBeCreated()
        {
            // Arrange & Act
            var dropdown = new DropdownComponent(Driver, By.Id("test-dropdown"));

            // Assert
            Assert.That(dropdown, Is.Not.Null);
            Assert.That(dropdown.Driver, Is.EqualTo(Driver));
        }
    }
}

