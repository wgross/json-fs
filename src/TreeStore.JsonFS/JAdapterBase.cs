namespace TreeStore.JsonFS;

public abstract class JAdapterBase : IServiceProvider
{
    #region IServiceProvider

    public object? GetService(Type serviceType)
    {
        if (this.GetType().IsAssignableTo(serviceType))
            return this;
        else return null;
    }

    #endregion IServiceProvider

    #region Write the JSON file after modification

    protected IDisposable BeginModify(ICmdletProvider cmdletProvider)
    {
        if (cmdletProvider is IJsonFsRootNodeModification jsonFsRootNodeModification)
            return jsonFsRootNodeModification.BeginModify();

        throw new InvalidOperationException($"Provider(type='{cmdletProvider.GetType()}' doesn't support modification");
    }

    #endregion Write the JSON file after modification

    #region Define value semantics

    protected static bool IsValueToken(JToken token) => token switch
    {
        JObject => false,
        JArray jarray => IsValueArray(jarray),

        _ => true
    };

    private static bool IsValueArray(JArray jarray)
    {
        if (!jarray.HasValues)
            return true; // empty array is value array until proven otherwise

        return IsValueToken(jarray.First());
    }

    #endregion Define value semantics

    #region Define child semantics

    protected static bool IsChildToken(JToken token) => !IsValueToken(token);

    #endregion Define child semantics
}