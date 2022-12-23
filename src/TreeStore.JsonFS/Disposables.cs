namespace TreeStore.JsonFS;

public static class Disposables
{
    private record class EmptyDisposable() : IDisposable
    {
        public void Dispose() { }
    }

    private record class DisposeFromAction(Action OnDispose) : IDisposable
    {
        public void Dispose() => this.OnDispose();
    }

    public static IDisposable Empty() => new EmptyDisposable();

    public static IDisposable FromAction(Action onDispose) => new DisposeFromAction(onDispose);
}