namespace TreeStore.JsonFS;

public interface IJsonFsRootNodeModification
{
    public IDisposable BeginModify();
}