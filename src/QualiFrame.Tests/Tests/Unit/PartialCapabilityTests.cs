using System;
using NUnit.Framework;
using QualiFrame.Framework.API.GraphQL;
using QualiFrame.Framework.API.Grpc;
using QualiFrame.Framework.API.WebSocket;
using QualiFrame.Framework.Messaging;
using QualiFrame.Framework.Observability;
using QualiFrame.Framework.Security;
using QualiFrame.Framework.Storage;
using QualiFrame.Framework.Utilities.Mobile;
using QualiFrame.Tests.Utilities;

namespace QualiFrame.Tests.Tests.Unit
{
    [TestFixture]
    public class PartialCapabilityTests
    {
        [Test]
        public void AppiumDriverFactory_CreateOptions_ShouldUseConfiguredPlatform()
        {
            var options = AppiumDriverFactory.CreateOptions();

            Assert.That(options, Is.Not.Null);
            Assert.That(options.PlatformName, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void GrpcChannelFactory_IsConfigured_WhenAddressMissing_ShouldBeFalse()
        {
            Assert.That(GrpcChannelFactory.IsConfigured(""), Is.False);
            Assert.Throws<InvalidOperationException>((Action)(() => GrpcChannelFactory.CreateChannel("")));
        }

        [Test]
        public void GrpcChannelFactory_CreateChannel_WithInvalidAddress_ShouldThrow()
        {
            Assert.Throws<InvalidOperationException>((Action)(() => GrpcChannelFactory.CreateChannel("not-a-uri")));
        }

        [Test]
        public void WebSocketTestClient_IsConfigured_WhenUrlMissing_ShouldBeFalse()
        {
            Assert.That(WebSocketTestClient.IsConfigured(""), Is.False);
        }

        [Test]
        public void KafkaMessageClient_Produce_WhenNotConfigured_ShouldThrow()
        {
            using var client = new KafkaMessageClient(bootstrapServers: "", topic: "t");
            Assert.That(KafkaMessageClient.IsConfigured(""), Is.False);
            Assert.ThrowsAsync<InvalidOperationException>((Func<Task>)(async () => client.ProduceAsync("k", "v")));
        }

        [Test]
        public void TelemetryBootstrap_WhenDisabled_Initialize_ShouldBeNoOp()
        {
            Assert.DoesNotThrow((Action)(() => TelemetryBootstrap.Initialize()));
            Assert.DoesNotThrow((Action)(() => TelemetryBootstrap.Shutdown()));
        }

        [Test]
        public void ArtifactStorageClient_WhenNotConfigured_ShouldReportFalse()
        {
            Assert.That(ArtifactStorageClient.IsS3Configured(), Is.False);
            Assert.That(ArtifactStorageClient.IsAzureBlobConfigured(), Is.False);
            Assert.ThrowsAsync<InvalidOperationException>((Func<Task>)(async () => ArtifactStorageClient.UploadToS3Async("missing.txt")));
            Assert.ThrowsAsync<InvalidOperationException>((Func<Task>)(async () => ArtifactStorageClient.UploadToAzureBlobAsync("missing.txt")));
        }

        [Test]
        public void SecurityTester_WhenZapDisabled_IsAvailable_ShouldBeFalse()
        {
            using var tester = new SecurityTester();
            Assert.That(tester.IsAvailable(), Is.False);
        }

        [Test]
        public void QueryBuilder_Build_ShouldIncludeRequestedFields()
        {
            var query = new QueryBuilder()
                .Operation("GetCountry")
                .NestedField("country", "code", "name")
                .Build();

            Assert.That(query, Does.Contain("query GetCountry"));
            Assert.That(query, Does.Contain("country"));
            Assert.That(query, Does.Contain("code"));
        }

        [Test]
        public void SyntheticTestDataFactory_ShouldGenerateDeterministicUser()
        {
            var first = SyntheticTestDataFactory.CreateUser(7);
            var second = SyntheticTestDataFactory.CreateUser(7);
            var anonymous = SyntheticTestDataFactory.CreateAnonymous<string>();

            Assert.That(first.Email, Is.EqualTo(second.Email));
            Assert.That(first.Email, Does.Contain("@"));
            Assert.That(anonymous, Is.Not.Null.And.Not.Empty);
        }
    }
}
