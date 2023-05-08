using Moq;
using TreeStore.Core.Providers;

namespace TreeStore.JsonFS.Test;

public static class MockExtensions
{
    public static (Mock<ICmdletProvider> provider, Mock<IDisposable> disopsable) ArrangeBeginModification(
        this (Mock<ICmdletProvider> provider, Mock<IDisposable> disposable) mocked)
    {
        mocked.provider
            .As<IJsonFsRootNodeModification>()
            .Setup(p => p.BeginModify())
            .Returns(mocked.disposable.Object);

        mocked.disposable
            .Setup(d => d.Dispose());

        return mocked;
    }
}