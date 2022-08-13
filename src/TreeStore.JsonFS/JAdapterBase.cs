namespace TreeStore.JsonFS
{
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
    }
}