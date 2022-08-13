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

    protected static bool IsValueProperty(JProperty property) => IsValueToken(property.Value);

    protected static bool IsValueToken(JToken token) => token switch
    {
        JObject => false,
        JArray jarray => IsValueArray(jarray),

        _ => true
    };

    protected static bool IsValueArray(JArray jarray)
    {
        if (!jarray.HasValues)
            return true; // empty array is value array until proven otherwise

        return IsValueToken(jarray.First());
    }

    #endregion Define value semantics

    #region Define child semantics

    protected static bool IsChildProperty(JProperty property) => !IsValueProperty(property);

    protected static bool IsChildToken(JToken token) => !IsValueToken(token);

    #endregion Define child semantics

    #region Query properties holding child node semantics

    protected static IEnumerable<JProperty> ChildProperties(JObject jobject) => jobject.Properties().Where(IsChildProperty);

    #endregion Query properties holding child node semantics

    #region Query properties holding value semantics

    protected static IEnumerable<JProperty> ValueProperties(JObject jobject) => jobject.Properties().Where(IsValueProperty);

    #endregion Query properties holding value semantics

    #region Create clones

    /// <summary>
    /// A shallow clone contains all properties except the objects.
    /// </summary>
    protected static JObject CreateShallowObjectClone(JObject jobject) => new JObject(ValueProperties(jobject));

    protected static JObject CreateDeepObjectClone(JObject jobject) => (JObject)jobject.DeepClone();

    protected static JArray CreateShallowArrayClone(JArray jarray) => new JArray(jarray);

    protected static JArray CreateDeepArrayClone(JArray jarray) => (JArray)jarray.DeepClone();

    #endregion Create clones
}