namespace TreeStore.JsonFS
{
    public sealed class JArrayAdapter : JAdapterBase
    {
        private readonly JArray payload;

        public JArrayAdapter(JArray payload) => this.payload = payload;
    }
}