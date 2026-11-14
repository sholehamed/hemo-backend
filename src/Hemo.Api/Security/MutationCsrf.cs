using Microsoft.AspNetCore.Antiforgery;

namespace Hemo.Api.Security;

/// <summary>All cookie-authenticated mutating JSON endpoints require antiforgery validation.</summary>
public static class MutationCsrf
{
    public static async Task<bool> ValidAsync(HttpContext http, IAntiforgery anti)
    {
        try { await anti.ValidateRequestAsync(http); return true; }
        catch (AntiforgeryValidationException) { return false; }
    }
}
