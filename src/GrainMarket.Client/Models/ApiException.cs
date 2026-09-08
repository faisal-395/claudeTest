namespace GrainMarket.Client.Models;

/// <summary>Thrown by ApiClient when the API returns a non-success status; carries the
/// problem+json "title" (and any per-field validation errors) so pages can show one clear
/// message instead of a raw HTTP error.</summary>
public class ApiException : Exception
{
    public int StatusCode { get; }
    public Dictionary<string, string[]>? Errors { get; }

    public ApiException(int statusCode, string message, Dictionary<string, string[]>? errors = null) : base(message)
    {
        StatusCode = statusCode;
        Errors = errors;
    }
}
