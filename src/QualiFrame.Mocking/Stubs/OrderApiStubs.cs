using QualiFrame.Mocking.Generation;
using QualiFrame.Mocking.Server;

namespace QualiFrame.Mocking.Stubs;

/// <summary>
/// Reusable stubs for an Order API domain: success, validation, errors, delays.
/// </summary>
public static class OrderApiStubs
{
    public const string BasePath = "/api/orders";

    /// <summary>POST /api/orders - 201 created.</summary>
    public static void RegisterPostOrderSuccess(WireMockServerManager manager, string? responseJson = null)
    {
        var body = responseJson ?? @"{}";
        manager.RegisterStub(StubDefinitionFactory.CreateCustom(
            BasePath, "POST", 201,
            string.IsNullOrEmpty(responseJson) ? @"{""id"":""ord-1"",""status"":""Created"",""createdAt"":""2024-01-01T00:00:00Z""}" : responseJson,
            description: "Post order success"));
    }

    /// <summary>POST /api/orders - 400 validation failure.</summary>
    public static void RegisterPostOrderValidationFailure(WireMockServerManager manager)
    {
        manager.RegisterStub(StubDefinitionFactory.CreateCustom(
            BasePath, "POST", 400,
            @"{""error"":""Validation Failed"",""details"":[{""field"":""items"",""message"":""Required""}]}",
            description: "Post order validation failure"));
    }

    /// <summary>POST /api/orders - 401.</summary>
    public static void RegisterPostOrderUnauthorized(WireMockServerManager manager)
    {
        manager.RegisterStub(StubDefinitionFactory.CreateCustom(
            BasePath, "POST", 401, @"{""error"":""Unauthorized""}", description: "Post order 401"));
    }

    /// <summary>POST /api/orders - 500.</summary>
    public static void RegisterPostOrderServerError(WireMockServerManager manager)
    {
        manager.RegisterStub(StubDefinitionFactory.CreateCustom(
            BasePath, "POST", 500, @"{""error"":""Internal Server Error""}", description: "Post order 500"));
    }

    /// <summary>POST /api/orders - 201 with delay.</summary>
    public static void RegisterPostOrderDelayed(WireMockServerManager manager, int delayMs = 1500)
    {
        manager.RegisterStub(StubDefinitionFactory.CreateCustom(
            BasePath, "POST", 201,
            @"{""id"":""ord-1"",""status"":""Created""}",
            delayMs: delayMs, description: "Post order delayed"));
    }

    /// <summary>GET /api/orders/{id} - 200.</summary>
    public static void RegisterGetOrderSuccess(WireMockServerManager manager, string id = "ord-1")
    {
        manager.RegisterStub(StubDefinitionFactory.CreateCustom(
            $"{BasePath}/{id}", "GET", 200,
            @"{""id"":""ord-1"",""status"":""Created"",""items"":[]}",
            description: "Get order success"));
    }
}
