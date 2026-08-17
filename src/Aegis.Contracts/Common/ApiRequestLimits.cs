namespace Aegis.Contracts.Common;

/// <summary>
/// Public v1 request limits shared by validation, application use cases, OpenAPI,
/// generated clients, and integration documentation.
/// </summary>
public static class ApiRequestLimits
{
    public const int DefaultPageSize = 50;
    public const int MaxPageSize = 100;
    public const int MaxBatchChecks = 1000;
    public const int MaxContinuationTokenLength = 512;
    public const int MaxResourceTypeFilterLength = 128;
}
