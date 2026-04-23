namespace SharedKernel.Application;

public sealed class PendingIntegrationEvents : IPendingIntegrationEvents
{
    private readonly List<Func<CancellationToken, Task>> _pending = [];

    public void Enqueue(Func<CancellationToken, Task> publish) => _pending.Add(publish);

    public IReadOnlyList<Func<CancellationToken, Task>> GetPending() => _pending.AsReadOnly();
}
