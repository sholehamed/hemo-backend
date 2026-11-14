namespace Hemo.Application.Cqrs;

/// <summary>Application result independent of HTTP status codes.</summary>
public sealed record Outcome<T>(T? Value, string? Error)
{
    public bool IsSuccess => Error is null;
    public static Outcome<T> Ok(T value) => new(value, null);
    public static Outcome<T> Fail(string code) => new(default, code);
}
