using Finora.App;

namespace Finora.UnitTests;

public sealed class ViewModelBaseTests
{
    [Fact]
    public async Task RunAsync_TogglesBusy_AndClearsPreviousError()
    {
        var probe = new ProbeViewModel();
        var states = new List<bool>();
        probe.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(ViewModelBase.IsBusy)) states.Add(probe.IsBusy);
        };

        await probe.RunProbeAsync(async () => await Task.Yield());

        Assert.Equal([true, false], states);
        Assert.False(probe.IsBusy);
        Assert.False(probe.HasError);
    }

    [Fact]
    public async Task RunAsync_ConvertsFailureIntoViewModelErrorState()
    {
        var probe = new ProbeViewModel();

        await probe.RunProbeAsync(() => throw new InvalidOperationException("Synthetic failure"));

        Assert.False(probe.IsBusy);
        Assert.True(probe.HasError);
        Assert.Equal("Synthetic failure", probe.ErrorMessage);
    }

    [Fact]
    public async Task RunAsync_IgnoresConcurrentInvocationWhileBusy()
    {
        var probe = new ProbeViewModel();
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var executions = 0;

        var first = probe.RunProbeAsync(async () =>
        {
            Interlocked.Increment(ref executions);
            entered.SetResult();
            await release.Task;
        });

        await entered.Task;
        await probe.RunProbeAsync(() =>
        {
            Interlocked.Increment(ref executions);
            return Task.CompletedTask;
        });
        release.SetResult();
        await first;

        Assert.Equal(1, executions);
    }

    [Fact]
    public async Task AsyncCommand_PreventsParallelExecution_AndRestoresCanExecute()
    {
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var executions = 0;
        var command = new AsyncCommand(async () =>
        {
            Interlocked.Increment(ref executions);
            entered.SetResult();
            await release.Task;
            completed.SetResult();
        });

        command.Execute(null);
        await entered.Task;
        Assert.False(command.CanExecute(null));

        command.Execute(null);
        release.SetResult();
        await completed.Task;
        await Task.Yield();

        Assert.Equal(1, executions);
        Assert.True(command.CanExecute(null));
    }

    [Fact]
    public void SetProperty_RaisesOnlyWhenValueChanges()
    {
        var probe = new ProbeViewModel();
        var notifications = 0;
        probe.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(ProbeViewModel.Value)) notifications++;
        };

        probe.Value = "one";
        probe.Value = "one";
        probe.Value = "two";

        Assert.Equal(2, notifications);
    }

    private sealed class ProbeViewModel : ViewModelBase
    {
        private string _value = string.Empty;
        public string Value { get => _value; set => SetProperty(ref _value, value); }
        public Task RunProbeAsync(Func<Task> action) => RunAsync(action);
    }
}
