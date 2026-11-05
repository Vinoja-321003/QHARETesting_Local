using QualiFrame.Mocking.Generation;
using QualiFrame.Mocking.Server;

namespace QualiFrame.Mocking.Stubs;

/// <summary>
/// Reusable stubs for a User API domain: success, validation errors, auth failures, errors, delays.
/// </summary>
public static class UserApiStubs
{
    public const string BasePath = "/api/users";

    /// <summary>GET /api/users/{id} - 200 with user payload.</summary>
    public static void RegisterGetUserSuccess(WireMockServerManager manager, string id = "1", string? responseJson = null)
    {
        var body = responseJson ?? $@"{{""id"":{id},""name"":""John Doe"",""email"":""john@example.com"",""active"":true}}";
        manager.RegisterStub(StubDefinitionFactory.CreateCustom(
            $"{BasePath}/{id}", "GET", 200, body, description: "Get user success"));
    }

    /// <summary>GET /api/users/{id} - 404.</summary>
    public static void RegisterGetUserNotFound(WireMockServerManager manager, string id = "999")
    {
        manager.RegisterStub(StubDefinitionFactory.CreateCustom(
            $"{BasePath}/{id}", "GET", 404, @"{""error"":""Not Found"",""message"":""User not found""}", description: "Get user 404"));
    }

    /// <summary>GET /api/users/{id} - 401.</summary>
    public static void RegisterGetUserUnauthorized(WireMockServerManager manager)
    {
        manager.RegisterStub(StubDefinitionFactory.CreateCustom(
            $"{BasePath}/{{id}}", "GET", 401, @"{""error"":""Unauthorized""}", description: "Get user 401"));
    }

    /// <summary>GET /api/users/{id} - 500.</summary>
    public static void RegisterGetUserServerError(WireMockServerManager manager)
    {
        manager.RegisterStub(StubDefinitionFactory.CreateCustom(
            $"{BasePath}/{{id}}", "GET", 500, @"{""error"":""Internal Server Error""}", description: "Get user 500"));
    }

    /// <summary>GET /api/users/{id} - 200 with delay (e.g. for loader/spinner tests).</summary>
    public static void RegisterGetUserDelayed(WireMockServerManager manager, int delayMs = 2000)
    {
        manager.RegisterStub(StubDefinitionFactory.CreateCustom(
            $"{BasePath}/{{id}}", "GET", 200,
            @"{""id"":1,""name"":""John Doe"",""email"":""john@example.com""}",
            delayMs: delayMs, description: "Get user delayed"));
    }

    /// <summary>Registers all user API stubs (success + error variants) for a path pattern. Use one variant per test.</summary>
    public static void RegisterAllVariants(WireMockServerManager manager)
    {
        RegisterGetUserSuccess(manager);
        RegisterGetUserNotFound(manager, "999");
        RegisterGetUserUnauthorized(manager);
        RegisterGetUserServerError(manager);
        RegisterGetUserDelayed(manager, 500);
    }
}
