using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Runia.Services.Core.Infrastructure.Network;

/// </summary>
/// сервис для выполнения HTTP-запросов с централизованной обработкой ошибок и логированием.
/// </summary>
public enum NetworkError
{
    None,
    HttpError,
    Timeout,
    NetworkUnavailable,
    DeserializationError,
    Unauthorized,
    Unknown
}

public sealed class Result<T>
{
    public bool IsSuccess { get; }
    public T? Data { get; }
    public NetworkError Error { get; }
    public string? ErrorMessage { get; }

    private Result(T? data)
    {
        IsSuccess = true;
        Data = data;
        Error = NetworkError.None;
    }

    private Result(NetworkError error, string? message = null)
    {
        IsSuccess = false;
        Data = default;
        Error = error;
        ErrorMessage = message;
    }

    public static Result<T> Success(T? data) => new(data);
    public static Result<T> Fail(NetworkError error, string? message = null) => new(error, message);
}

public sealed class BaseApiService(HttpClient httpClient, ILogger<BaseApiService> logger)
{
    private static readonly JsonSerializerOptions s_jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<Result<T?>> GetAsync<T>(string url, CancellationToken ct = default)
    {
        try
        {
            using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                return Result<T?>.Fail(NetworkError.Unauthorized, "Unauthorized");

            if (!response.IsSuccessStatusCode)
            {
                var msg = await ReadSafeErrorMessageAsync(response, ct);
                logger.LogWarning("GET {Url} returned {Status}: {Msg}", url, response.StatusCode, msg);
                return Result<T?>.Fail(NetworkError.HttpError, msg);
            }

            var data = await response.Content.ReadFromJsonAsync<T>(s_jsonOptions, ct);
            return Result<T?>.Success(data);
        }
        catch (OperationCanceledException ex) when (ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "GET cancelled: {Url}", url);
            return Result<T?>.Fail(NetworkError.Timeout, "Request timeout");
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "GET network error: {Url}", url);
            return Result<T?>.Fail(NetworkError.NetworkUnavailable, "Network unavailable");
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "GET deserialization error for type {Type}: {Url} - {Message}", typeof(T).Name, url, ex.Message);
            return Result<T?>.Fail(NetworkError.DeserializationError, $"Deserialization error for {typeof(T).Name}: {ex.Message}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GET unexpected error: {Url}", url);
            return Result<T?>.Fail(NetworkError.Unknown, ex.Message);
        }
    }

    public async Task<Result<TOut?>> PostAsync<TIn, TOut>(string url, TIn body, CancellationToken ct = default)
    {
        try
        {
            using var response = await httpClient.PostAsJsonAsync(url, body, s_jsonOptions, ct);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                return Result<TOut?>.Fail(NetworkError.Unauthorized, "Unauthorized");

            if (!response.IsSuccessStatusCode)
            {
                var msg = await ReadSafeErrorMessageAsync(response, ct);
                logger.LogWarning("POST {Url} returned {Status}: {Msg}", url, response.StatusCode, msg);
                return Result<TOut?>.Fail(NetworkError.HttpError, msg);
            }

            var data = await response.Content.ReadFromJsonAsync<TOut>(s_jsonOptions, ct);
            return Result<TOut?>.Success(data);
        }
        catch (OperationCanceledException ex) when (ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "POST cancelled: {Url}", url);
            return Result<TOut?>.Fail(NetworkError.Timeout, "Request timeout");
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "POST network error: {Url}", url);
            return Result<TOut?>.Fail(NetworkError.NetworkUnavailable, "Network unavailable");
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "POST deserialization error for type {Type}: {Url} - {Message}", typeof(TOut).Name, url, ex.Message);
            return Result<TOut?>.Fail(NetworkError.DeserializationError, $"Deserialization error for {typeof(TOut).Name}: {ex.Message}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "POST unexpected error: {Url}", url);
            return Result<TOut?>.Fail(NetworkError.Unknown, ex.Message);
        }
    }

    public async Task<Result<bool>> PostAsync<TIn>(string url, TIn body, CancellationToken ct = default)
    {
        try
        {
            using var response = await httpClient.PostAsJsonAsync(url, body, s_jsonOptions, ct);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                return Result<bool>.Fail(NetworkError.Unauthorized, "Unauthorized");

            if (!response.IsSuccessStatusCode)
            {
                var msg = await ReadSafeErrorMessageAsync(response, ct);
                logger.LogWarning("POST {Url} returned {Status}: {Msg}", url, response.StatusCode, msg);
                return Result<bool>.Fail(NetworkError.HttpError, msg);
            }

            return Result<bool>.Success(true);
        }
        catch (OperationCanceledException ex) when (ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "POST cancelled: {Url}", url);
            return Result<bool>.Fail(NetworkError.Timeout, "Request timeout");
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "POST network error: {Url}", url);
            return Result<bool>.Fail(NetworkError.NetworkUnavailable, "Network unavailable");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "POST unexpected error: {Url}", url);
            return Result<bool>.Fail(NetworkError.Unknown, ex.Message);
        }
    }

    public async Task<Result<TOut?>> PutAsync<TIn, TOut>(string url, TIn body, CancellationToken ct = default)
    {
        try
        {
            using var response = await httpClient.PutAsJsonAsync(url, body, s_jsonOptions, ct);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                return Result<TOut?>.Fail(NetworkError.Unauthorized, "Unauthorized");

            if (!response.IsSuccessStatusCode)
            {
                var msg = await ReadSafeErrorMessageAsync(response, ct);
                logger.LogWarning("PUT {Url} returned {Status}: {Msg}", url, response.StatusCode, msg);
                return Result<TOut?>.Fail(NetworkError.HttpError, msg);
            }

            var data = await response.Content.ReadFromJsonAsync<TOut>(s_jsonOptions, ct);
            return Result<TOut?>.Success(data);
        }
        catch (OperationCanceledException ex) when (ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "PUT cancelled: {Url}", url);
            return Result<TOut?>.Fail(NetworkError.Timeout, "Request timeout");
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "PUT network error: {Url}", url);
            return Result<TOut?>.Fail(NetworkError.NetworkUnavailable, "Network unavailable");
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "PUT deserialization error: {Url}", url);
            return Result<TOut?>.Fail(NetworkError.DeserializationError, ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PUT unexpected error: {Url}", url);
            return Result<TOut?>.Fail(NetworkError.Unknown, ex.Message);
        }
    }

    public async Task<Result<bool>> DeleteAsync(string url, CancellationToken ct = default)
    {
        try
        {
            using var response = await httpClient.DeleteAsync(url, ct);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                return Result<bool>.Fail(NetworkError.Unauthorized, "Unauthorized");

            if (!response.IsSuccessStatusCode)
            {
                var msg = await ReadSafeErrorMessageAsync(response, ct);
                logger.LogWarning("DELETE {Url} returned {Status}: {Msg}", url, response.StatusCode, msg);
                return Result<bool>.Fail(NetworkError.HttpError, msg);
            }

            return Result<bool>.Success(true);
        }
        catch (OperationCanceledException ex) when (ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "DELETE cancelled: {Url}", url);
            return Result<bool>.Fail(NetworkError.Timeout, "Request timeout");
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "DELETE network error: {Url}", url);
            return Result<bool>.Fail(NetworkError.NetworkUnavailable, "Network unavailable");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "DELETE unexpected error: {Url}", url);
            return Result<bool>.Fail(NetworkError.Unknown, ex.Message);
        }
    }

    private static async Task<string> ReadSafeErrorMessageAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            using var contentStream = await response.Content.ReadAsStreamAsync(ct);
            using var reader = new StreamReader(contentStream, System.Text.Encoding.UTF8);

            var buffer = new char[4096];
            int read = await reader.ReadAsync(buffer, 0, buffer.Length);

            if (read == 0) return "Empty response";

            string result = new(buffer, 0, read);
            if (!reader.EndOfStream)
            {
                result += "... [Truncated]";
            }

            return result;
        }
        catch
        {
            return "Could not read error message";
        }
    }
}