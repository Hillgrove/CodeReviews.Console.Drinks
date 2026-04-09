namespace DrinksInfo.Hillgrove.Common;

record Result<T>(ResultStatus Status, T? Data = default, string? Error = null)
{
    public bool IsSuccess => Status == ResultStatus.Success;
    public bool IsCancelled => Status == ResultStatus.Cancelled;
    public bool IsError => Status == ResultStatus.Error;

    public static Result<T> Success(T data) => new(ResultStatus.Success, data);

    public static Result<T> Cancelled() => new(ResultStatus.Cancelled);

    public static Result<T> Failure(string error) => new(ResultStatus.Error, default, error);
}
