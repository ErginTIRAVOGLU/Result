using System.Net;

namespace ErginWebDev.Result;

/// <summary>
/// Base class for Result pattern implementation with string-based errors.
/// </summary>
public abstract record ResultBase
{
    /// <summary>
    /// Indicates whether the operation was successful.
    /// </summary>
    public bool Success { get; init; }
    
    /// <summary>
    /// Indicates whether the operation failed.
    /// </summary>
    public bool IsFailure => !Success;
    
    /// <summary>
    /// Optional message describing the result.
    /// </summary>
    public string? Message { get; init; }
    
    /// <summary>
    /// Collection of error messages. Empty for successful results.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; }
    
    /// <summary>
    /// HTTP status code associated with the result. Defaults to 200 OK for success, 400 BadRequest for failures.
    /// </summary>
    public HttpStatusCode StatusCode { get; init; } = HttpStatusCode.OK;

    protected ResultBase(
        bool success,
        string? message,
        IEnumerable<string>? errors,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var errorList = errors?.ToList() ?? new List<string>();

        if (success && errorList.Count > 0)
            throw new InvalidOperationException("Success result cannot contain errors.");

        Success = success;
        Message = message;
        Errors = errorList;
        StatusCode = statusCode;
    }

    protected ResultBase()
    {
        Errors = new List<string>();
    }

    protected ResultBase(ResultBase original)
    {
        Success = original.Success;
        Message = original.Message;
        Errors = original.Errors;
        StatusCode = original.StatusCode;
    }
}


/// <summary>
/// Base class for Result pattern implementation with generic error types.
/// </summary>
/// <typeparam name="TError">The type of error objects.</typeparam>
public abstract record ResultBase<TError>
{
    /// <summary>
    /// Indicates whether the operation was successful.
    /// </summary>
    public bool Success { get; init; }
    
    /// <summary>
    /// Indicates whether the operation failed.
    /// </summary>
    public bool IsFailure => !Success;
    
    /// <summary>
    /// Optional message describing the result.
    /// </summary>
    public string? Message { get; init; }
    
    /// <summary>
    /// Collection of error objects. Empty for successful results.
    /// </summary>
    public IReadOnlyList<TError> Errors { get; init; }
    
    /// <summary>
    /// HTTP status code associated with the result. Defaults to 200 OK for success, 400 BadRequest for failures.
    /// </summary>
    public HttpStatusCode StatusCode { get; init; } = HttpStatusCode.OK;

    protected ResultBase(
        bool success,
        string? message,
        IEnumerable<TError>? errors,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var errorList = errors?.ToList() ?? new List<TError>();

        if (success && errorList.Count > 0)
            throw new InvalidOperationException("Success result cannot contain errors.");

        Success = success;
        Message = message;
        Errors = errorList;
        StatusCode = statusCode;
    }

    protected ResultBase()
    {
        Errors = new List<TError>();
    }
}


/// <summary>
/// Represents the result of an operation without return data.
/// </summary>
public sealed record Result : ResultBase
{
    private Result(bool success, string? message, IEnumerable<string>? errors, HttpStatusCode statusCode)
        : base(success, message, errors, statusCode) { }

    public Result() : base() { }

    public Result(ResultBase original) : base(original) { }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="message">Optional success message.</param>
    /// <param name="statusCode">HTTP status code. Defaults to 200 OK.</param>
    /// <returns>A successful Result.</returns>
    public static new Result Success(string? message = null, HttpStatusCode statusCode = HttpStatusCode.OK)
        => new(true, message, null, statusCode);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="message">Required error message.</param>
    /// <param name="errors">Optional collection of detailed error messages.</param>
    /// <param name="statusCode">HTTP status code. Defaults to 400 BadRequest.</param>
    /// <returns>A failed Result.</returns>
    public static Result Fail(
        string message,
        IEnumerable<string>? errors = null,
        HttpStatusCode statusCode = HttpStatusCode.BadRequest)
        => new(false, message, errors, statusCode);

    /// <summary>
    /// Executes an action and returns Success if no exception occurs, otherwise returns Fail.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <param name="errorMessage">Custom error message if action fails.</param>
    /// <returns>Success if action completes, Fail if exception occurs.</returns>
    public static Result Try(Action action, string? errorMessage = null)
    {
        try
        {
            action();
            return Success();
        }
        catch (Exception ex)
        {
            return Fail(errorMessage ?? ex.Message, new[] { ex.ToString() });
        }
    }

    /// <summary>
    /// Validates multiple conditions and aggregates errors.
    /// </summary>
    /// <param name="validations">Array of validation functions.</param>
    /// <returns>Success if all validations pass, Fail with aggregated errors otherwise.</returns>
    public static Result Validate(params Func<Result>[] validations)
    {
        var errors = new List<string>();
        string? firstMessage = null;

        foreach (var validation in validations)
        {
            var result = validation();
            if (result.IsFailure)
            {
                firstMessage ??= result.Message;
                errors.AddRange(result.Errors);
            }
        }

        return errors.Count == 0 
            ? Success("All validations passed") 
            : Fail(firstMessage ?? "Validation failed", errors, HttpStatusCode.UnprocessableEntity);
    }

    /// <summary>
    /// Executes the appropriate function based on the result state.
    /// </summary>
    /// <typeparam name="TResult">The return type.</typeparam>
    /// <param name="onSuccess">Function to execute if successful.</param>
    /// <param name="onFailure">Function to execute if failed.</param>
    /// <returns>The result of the executed function.</returns>
    public TResult Match<TResult>(Func<TResult> onSuccess, Func<IReadOnlyList<string>, TResult> onFailure)
        => base.Success ? onSuccess() : onFailure(Errors);

    /// <summary>
    /// Returns a new Result with the specified status code.
    /// </summary>
    /// <param name="statusCode">The new status code.</param>
    /// <returns>A new Result with updated status code.</returns>
    public Result WithStatusCode(HttpStatusCode statusCode)
        => new(base.Success, Message, Errors, statusCode);

    /// <summary>
    /// Returns a new Result with the specified message.
    /// </summary>
    /// <param name="message">The new message.</param>
    /// <returns>A new Result with updated message.</returns>
    public Result WithMessage(string? message)
        => new(base.Success, message, Errors, StatusCode);
}


/// <summary>
/// Represents the result of an operation with typed return data.
/// </summary>
/// <typeparam name="T">The type of data returned on success.</typeparam>
public sealed record Result<T> : ResultBase
{
    /// <summary>
    /// The data payload. Available when Success is true.
    /// </summary>
    public T? Data { get; init; }

    private Result(bool success, string? message, T? data, IEnumerable<string>? errors, HttpStatusCode statusCode)
        : base(success, message, errors, statusCode)
    {
        Data = data;
    }

    public Result() : base() { }

    /// <summary>
    /// Creates a successful result with data.
    /// </summary>
    /// <param name="data">The data to return.</param>
    /// <param name="message">Optional success message.</param>
    /// <param name="statusCode">HTTP status code. Defaults to 200 OK.</param>
    /// <returns>A successful Result with data.</returns>
    public static new Result<T> Success(
        T? data = default,
        string? message = null,
        HttpStatusCode statusCode = HttpStatusCode.OK)
        => new(true, message, data, null, statusCode);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="message">Required error message.</param>
    /// <param name="errors">Optional collection of detailed error messages.</param>
    /// <param name="statusCode">HTTP status code. Defaults to 400 BadRequest.</param>
    /// <returns>A failed Result.</returns>
    public static Result<T> Fail(
        string message,
        IEnumerable<string>? errors = null,
        HttpStatusCode statusCode = HttpStatusCode.BadRequest)
        => new(false, message, default, errors, statusCode);

    /// <summary>
    /// Executes a function and returns Success with result if no exception occurs, otherwise returns Fail.
    /// </summary>
    /// <param name="func">The function to execute.</param>
    /// <param name="errorMessage">Custom error message if function fails.</param>
    /// <returns>Success with data if function completes, Fail if exception occurs.</returns>
    public static Result<T> Try(Func<T> func, string? errorMessage = null)
    {
        try
        {
            var data = func();
            return Success(data);
        }
        catch (Exception ex)
        {
            return Fail(errorMessage ?? ex.Message, new[] { ex.ToString() });
        }
    }

    /// <summary>
    /// Transforms the data if the result is successful.
    /// </summary>
    /// <typeparam name="TNew">The new data type.</typeparam>
    /// <param name="mapper">Function to transform the data.</param>
    /// <returns>A new Result with transformed data, or the original failure.</returns>
    public Result<TNew> Map<TNew>(Func<T, TNew> mapper)
    {
        if (IsFailure)
            return Result<TNew>.Fail(Message!, Errors, StatusCode);

        try
        {
            var newData = mapper(Data!);
            return Result<TNew>.Success(newData, Message, StatusCode);
        }
        catch (Exception ex)
        {
            return Result<TNew>.Fail(ex.Message, new[] { ex.ToString() });
        }
    }

    /// <summary>
    /// Chains another Result-returning operation if this result is successful.
    /// </summary>
    /// <typeparam name="TNew">The new data type.</typeparam>
    /// <param name="binder">Function that returns a new Result.</param>
    /// <returns>The result of the binder function, or the original failure.</returns>
    public Result<TNew> Bind<TNew>(Func<T, Result<TNew>> binder)
    {
        if (IsFailure)
            return Result<TNew>.Fail(Message!, Errors, StatusCode);

        try
        {
            return binder(Data!);
        }
        catch (Exception ex)
        {
            return Result<TNew>.Fail(ex.Message, new[] { ex.ToString() });
        }
    }

    /// <summary>
    /// Executes the appropriate function based on the result state.
    /// </summary>
    /// <typeparam name="TResult">The return type.</typeparam>
    /// <param name="onSuccess">Function to execute if successful.</param>
    /// <param name="onFailure">Function to execute if failed.</param>
    /// <returns>The result of the executed function.</returns>
    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<IReadOnlyList<string>, TResult> onFailure)
        => base.Success ? onSuccess(Data!) : onFailure(Errors);

    /// <summary>
    /// Returns a new Result with the specified status code.
    /// </summary>
    /// <param name="statusCode">The new status code.</param>
    /// <returns>A new Result with updated status code.</returns>
    public Result<T> WithStatusCode(HttpStatusCode statusCode)
        => new(base.Success, Message, Data, Errors, statusCode);

    /// <summary>
    /// Returns a new Result with the specified message.
    /// </summary>
    /// <param name="message">The new message.</param>
    /// <returns>A new Result with updated message.</returns>
    public Result<T> WithMessage(string? message)
        => new(base.Success, message, Data, Errors, StatusCode);

    /// <summary>
    /// Implicitly converts data to a successful Result.
    /// </summary>
    /// <param name="data">The data to wrap.</param>
    public static implicit operator Result<T>(T data) => Success(data);
}


/// <summary>
/// Represents the result of an operation with typed return data and typed errors.
/// </summary>
/// <typeparam name="TData">The type of data returned on success.</typeparam>
/// <typeparam name="TError">The type of error objects.</typeparam>
public sealed record Result<TData, TError> : ResultBase<TError>
{
    /// <summary>
    /// The data payload. Available when Success is true.
    /// </summary>
    public TData? Data { get; init; }

    private Result(bool success, string? message, TData? data, IEnumerable<TError>? errors, HttpStatusCode statusCode)
        : base(success, message, errors, statusCode)
    {
        Data = data;
    }

    public Result() : base() { }

    /// <summary>
    /// Creates a successful result with data.
    /// </summary>
    /// <param name="data">The data to return.</param>
    /// <param name="message">Optional success message.</param>
    /// <param name="statusCode">HTTP status code. Defaults to 200 OK.</param>
    /// <returns>A successful Result with data.</returns>
    public static new Result<TData, TError> Success(
        TData? data = default,
        string? message = null,
        HttpStatusCode statusCode = HttpStatusCode.OK)
        => new(true, message, data, null, statusCode);

    /// <summary>
    /// Creates a failed result with typed errors.
    /// </summary>
    /// <param name="message">Required error message.</param>
    /// <param name="errors">Optional collection of typed error objects.</param>
    /// <param name="statusCode">HTTP status code. Defaults to 400 BadRequest.</param>
    /// <returns>A failed Result.</returns>
    public static Result<TData, TError> Fail(
        string message,
        IEnumerable<TError>? errors = null,
        HttpStatusCode statusCode = HttpStatusCode.BadRequest)
        => new(false, message, default, errors, statusCode);

    /// <summary>
    /// Executes a function and returns Success with result if no exception occurs, otherwise returns Fail.
    /// </summary>
    /// <param name="func">The function to execute.</param>
    /// <param name="errorMessage">Custom error message if function fails.</param>
    /// <param name="errorFactory">Factory to create typed error from exception.</param>
    /// <returns>Success with data if function completes, Fail if exception occurs.</returns>
    public static Result<TData, TError> Try(
        Func<TData> func, 
        string? errorMessage = null,
        Func<Exception, TError>? errorFactory = null)
    {
        try
        {
            var data = func();
            return Success(data);
        }
        catch (Exception ex)
        {
            var errors = errorFactory != null 
                ? new[] { errorFactory(ex) } 
                : Array.Empty<TError>();
            return Fail(errorMessage ?? ex.Message, errors);
        }
    }

    /// <summary>
    /// Transforms the data if the result is successful.
    /// </summary>
    /// <typeparam name="TNewData">The new data type.</typeparam>
    /// <param name="mapper">Function to transform the data.</param>
    /// <returns>A new Result with transformed data, or the original failure.</returns>
    public Result<TNewData, TError> Map<TNewData>(Func<TData, TNewData> mapper)
    {
        if (IsFailure)
            return Result<TNewData, TError>.Fail(Message!, Errors, StatusCode);

        try
        {
            var newData = mapper(Data!);
            return Result<TNewData, TError>.Success(newData, Message, StatusCode);
        }
        catch (Exception ex)
        {
            return Result<TNewData, TError>.Fail(ex.Message, statusCode: HttpStatusCode.InternalServerError);
        }
    }

    /// <summary>
    /// Chains another Result-returning operation if this result is successful.
    /// </summary>
    /// <typeparam name="TNewData">The new data type.</typeparam>
    /// <param name="binder">Function that returns a new Result.</param>
    /// <returns>The result of the binder function, or the original failure.</returns>
    public Result<TNewData, TError> Bind<TNewData>(Func<TData, Result<TNewData, TError>> binder)
    {
        if (IsFailure)
            return Result<TNewData, TError>.Fail(Message!, Errors, StatusCode);

        try
        {
            return binder(Data!);
        }
        catch (Exception ex)
        {
            return Result<TNewData, TError>.Fail(ex.Message, statusCode: HttpStatusCode.InternalServerError);
        }
    }

    /// <summary>
    /// Executes the appropriate function based on the result state.
    /// </summary>
    /// <typeparam name="TResult">The return type.</typeparam>
    /// <param name="onSuccess">Function to execute if successful.</param>
    /// <param name="onFailure">Function to execute if failed.</param>
    /// <returns>The result of the executed function.</returns>
    public TResult Match<TResult>(Func<TData, TResult> onSuccess, Func<IReadOnlyList<TError>, TResult> onFailure)
        => base.Success ? onSuccess(Data!) : onFailure(Errors);

    /// <summary>
    /// Returns a new Result with the specified status code.
    /// </summary>
    /// <param name="statusCode">The new status code.</param>
    /// <returns>A new Result with updated status code.</returns>
    public Result<TData, TError> WithStatusCode(HttpStatusCode statusCode)
        => new(base.Success, Message, Data, Errors, statusCode);

    /// <summary>
    /// Returns a new Result with the specified message.
    /// </summary>
    /// <param name="message">The new message.</param>
    /// <returns>A new Result with updated message.</returns>
    public Result<TData, TError> WithMessage(string? message)
        => new(base.Success, message, Data, Errors, StatusCode);

    /// <summary>
    /// Implicitly converts data to a successful Result.
    /// </summary>
    /// <param name="data">The data to wrap.</param>
    public static implicit operator Result<TData, TError>(TData data) => Success(data);
}

