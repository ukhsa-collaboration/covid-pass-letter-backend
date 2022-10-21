namespace CovidLetter.Backend.Common.Infrastructure.Barcodes;

public class TaskHelpers
{
    public static async Task<T> WithDelay<T>(TimeSpan delay, Func<Task<T>> action)
    {
        var task = action();
        await Task.WhenAll(task, Task.Delay(delay));
        return await task;
    }
}
