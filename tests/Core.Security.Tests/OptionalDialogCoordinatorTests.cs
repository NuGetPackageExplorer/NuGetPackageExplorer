using NuGetPackageExplorer.Core.Async;

namespace Core.Security.Tests;

public sealed class OptionalDialogCoordinatorTests
{
    [Fact]
    public async Task WaitForResultAsyncReturnsWorkResultWhenWorkFinishesFirst()
    {
        using var cts = new CancellationTokenSource();
        var workTask = Task.FromResult("package");
        var dialogTask = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously).Task;

        var result = await OptionalDialogCoordinator.WaitForResultAsync(workTask, dialogTask, cts.Token);

        Assert.Equal("package", result);
    }

    [Fact]
    public async Task WaitForResultAsyncReturnsWorkResultWhenDialogClosesWithoutCancellation()
    {
        using var cts = new CancellationTokenSource();
        var workTaskSource = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var dialogTaskSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var resultTask = OptionalDialogCoordinator.WaitForResultAsync(workTaskSource.Task, dialogTaskSource.Task, cts.Token);

        dialogTaskSource.SetResult();
        workTaskSource.SetResult("package");

        var result = await resultTask;

        Assert.Equal("package", result);
    }

    [Fact]
    public async Task WaitForResultAsyncThrowsWhenDialogClosesAfterUserCancellation()
    {
        using var cts = new CancellationTokenSource();
        var workTaskSource = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var dialogTaskSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var resultTask = OptionalDialogCoordinator.WaitForResultAsync(workTaskSource.Task, dialogTaskSource.Task, cts.Token);

        await cts.CancelAsync();
        dialogTaskSource.SetResult();

        await Assert.ThrowsAsync<OperationCanceledException>(() => resultTask);
    }
}
