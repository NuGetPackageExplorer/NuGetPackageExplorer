namespace NuGetPackageExplorer.Core.Async;

public static class OptionalDialogCoordinator
{
    public static async Task<T> WaitForResultAsync<T>(Task<T> workTask, Task dialogTask, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(workTask);
        ArgumentNullException.ThrowIfNull(dialogTask);

        var completed = await Task.WhenAny(workTask, dialogTask).ConfigureAwait(false);
        if (completed == workTask)
        {
            return await workTask.ConfigureAwait(false);
        }

        if (cancellationToken.IsCancellationRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();
        }

        return await workTask.ConfigureAwait(false);
    }
}
