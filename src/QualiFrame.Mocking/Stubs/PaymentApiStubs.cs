using QualiFrame.Mocking.Generation;
using QualiFrame.Mocking.Server;

namespace QualiFrame.Mocking.Stubs;

/// <summary>
/// Reusable stubs for a Payment API domain: success, failures, delays.
/// </summary>
public static class PaymentApiStubs
{
    public const string BasePath = "/api/payments";

    public static void RegisterPostPaymentSuccess(WireMockServerManager manager, string? responseJson = null)
    {
        var body = responseJson ?? @"{""transactionId"":""tx-1"",""status"":""Completed"",""amount"":99.99}";
        manager.RegisterStub(StubDefinitionFactory.CreateCustom(
            BasePath, "POST", 200, body, description: "Post payment success"));
    }

    public static void RegisterPostPaymentDeclined(WireMockServerManager manager)
    {
        manager.RegisterStub(StubDefinitionFactory.CreateCustom(
            BasePath, "POST", 402, @"{""error"":""Payment Declined"",""code"":""INSUFFICIENT_FUNDS""}", description: "Payment declined"));
    }

    public static void RegisterPostPaymentValidationFailure(WireMockServerManager manager)
    {
        manager.RegisterStub(StubDefinitionFactory.CreateCustom(
            BasePath, "POST", 400, @"{""error"":""Validation Failed"",""details"":[{""field"":""cardNumber""}]}", description: "Payment validation failure"));
    }

    public static void RegisterPostPaymentUnauthorized(WireMockServerManager manager)
    {
        manager.RegisterStub(StubDefinitionFactory.CreateCustom(
            BasePath, "POST", 401, @"{""error"":""Unauthorized""}", description: "Payment 401"));
    }

    public static void RegisterPostPaymentServerError(WireMockServerManager manager)
    {
        manager.RegisterStub(StubDefinitionFactory.CreateCustom(
            BasePath, "POST", 500, @"{""error"":""Internal Server Error""}", description: "Payment 500"));
    }

    public static void RegisterPostPaymentDelayed(WireMockServerManager manager, int delayMs = 3000)
    {
        manager.RegisterStub(StubDefinitionFactory.CreateCustom(
            BasePath, "POST", 200,
            @"{""transactionId"":""tx-1"",""status"":""Completed""}",
            delayMs: delayMs, description: "Payment delayed"));
    }
}
