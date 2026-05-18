namespace Runia.Services.Network;

/// <summary>
/// Кастомный результат API операции, содержит данные или ошибку.
/// </summary>
public class ApiResult<T>
{
    public bool IsSuccess { get; }
    public T? Data { get; }
    public string? ErrorMessage { get; }

    private ApiResult(T? data)
    {
        IsSuccess = true;
        Data = data;
        ErrorMessage = null;
    }

    private ApiResult(string errorMessage)
    {
        IsSuccess = false;
        Data = default;
        ErrorMessage = errorMessage;
    }

    public static ApiResult<T> Success(T? data) => new(data);
    public static ApiResult<T> Failure(string errorMessage) => new(errorMessage);
}

/// <summary>
/// Результат без данных, только статус и ошибка.
/// </summary>
public class ApiResult
{
    public bool IsSuccess { get; }
    public string? ErrorMessage { get; }

    private ApiResult(bool success, string? errorMessage = null)
    {
        IsSuccess = success;
        ErrorMessage = errorMessage;
    }

    public static ApiResult Success() => new(true);
    public static ApiResult Failure(string errorMessage) => new(false, errorMessage);
}
