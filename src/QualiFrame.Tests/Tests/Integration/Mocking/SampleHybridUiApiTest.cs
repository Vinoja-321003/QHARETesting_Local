using NUnit.Framework;
using QualiFrame.Mocking.HybridTesting;
using QualiFrame.Mocking.Stubs;
using System.Net.Http;

namespace QualiFrame.Tests.Integration.Mocking;

[TestFixture]
[Category("Mocking")]
public class SampleHybridUiApiTest
{
    [Test]
    public void ExecuteMockedUiFlow_SuccessResponse_DrivesUiSuccessState()
    {
        var orchestrator = new HybridTestOrchestrator();

        string? capturedBody = null;
        int? capturedStatus = null;

        orchestrator.ExecuteMockedUiFlow(
            registerStubs: manager => UserApiStubs.RegisterGetUserSuccess(manager, "1"),
            testAction: baseUrl =>
            {
                using var client = new HttpClient { BaseAddress = new Uri(baseUrl) };
                var response = client.GetAsync("/api/users/1").GetAwaiter().GetResult();
                capturedStatus = (int)response.StatusCode;
                capturedBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                Assert.That(response.IsSuccessStatusCode);
                Assert.That(capturedBody, Does.Contain("John").Or.Contain("id").Or.Contain("email"));
            });

        Assert.That(capturedStatus, Is.EqualTo(200));
    }

    [Test]
    public void ExecuteMockedUiFlow_404Response_DrivesNotFoundState()
    {
        var orchestrator = new HybridTestOrchestrator();

        orchestrator.ExecuteMockedUiFlow(
            registerStubs: manager => UserApiStubs.RegisterGetUserNotFound(manager, "999"),
            testAction: baseUrl =>
            {
                using var client = new HttpClient { BaseAddress = new Uri(baseUrl) };
                var response = client.GetAsync("/api/users/999").GetAwaiter().GetResult();
                Assert.That((int)response.StatusCode, Is.EqualTo(404));
                var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                Assert.That(body, Does.Contain("Not Found").Or.Contain("error"));
            });
    }

    [Test]
    public void ExecuteMockedUiFlow_500Response_DrivesErrorBanner()
    {
        var orchestrator = new HybridTestOrchestrator();

        orchestrator.ExecuteMockedUiFlow(
            registerStubs: manager => UserApiStubs.RegisterGetUserServerError(manager),
            testAction: baseUrl =>
            {
                using var client = new HttpClient { BaseAddress = new Uri(baseUrl) };
                var response = client.GetAsync("/api/users/1").GetAwaiter().GetResult();
                Assert.That((int)response.StatusCode, Is.EqualTo(500));
            });
    }

    [Test]
    public void ExecuteMockedUiFlow_DelayedResponse_AllowsLoaderSpinnerAssertion()
    {
        var orchestrator = new HybridTestOrchestrator();

        orchestrator.ExecuteMockedUiFlow(
            registerStubs: manager => UserApiStubs.RegisterGetUserDelayed(manager, 500),
            testAction: baseUrl =>
            {
                using var client = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(5) };
                var response = client.GetAsync("/api/users/1").GetAwaiter().GetResult();
                Assert.That(response.IsSuccessStatusCode);
            });
    }
}
