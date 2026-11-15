using Hemo.Application.Authentication;
using Hemo.Application.Cqrs;

namespace Hemo.Api.Security;

/// <summary>Explicit one-time CLI entrypoint; no EF, password hashing or SQL in API.</summary>
public static class AdminBootstrap
{
    public static async Task RunAsync(
        IRequestHandler<BootstrapSystemAdministratorCommand, Outcome<bool>> handler,
        IConfiguration config, CancellationToken ct = default)
    {
        var result = await handler.Handle(new BootstrapSystemAdministratorCommand(
            config["HEMO_BOOTSTRAP_USERNAME"],
            config["HEMO_BOOTSTRAP_DISPLAY_NAME"],
            config["HEMO_BOOTSTRAP_PASSWORD"]), ct);
        if (!result.IsSuccess)
            throw new InvalidOperationException(
                "Bootstrap refused or invalid configuration: " + result.Error +
                ". Supply HEMO_BOOTSTRAP_USERNAME, _DISPLAY_NAME, _PASSWORD (16+ characters).");
    }
}
