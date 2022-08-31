namespace TreeStore.JsonFS
{
    public sealed class JObjectContentWriter : IContentWriter
    {
        private readonly ICmdletProvider provider;
        private readonly JObjectAdapter adapter;

        private JObject? newObject = null;

        public JObjectContentWriter(ICmdletProvider provider, JObjectAdapter adapter)
        {
            this.provider = provider;
            this.adapter = adapter;
        }

        public void Close()
        {
            if (this.provider is not IJsonFsRootNodeModification jsonFsRootNodeModification)
                throw new InvalidOperationException($"Provider(type='{this.provider.GetType()}' doesn't support modification");

            if (this.newObject is JObject jobject)
            {
                using var handle = jsonFsRootNodeModification.BeginModify();

                this.adapter.GetRequiredService<IClearItemContent>().ClearItemContent(this.provider);
                this.adapter.payload.Add(jobject);
            }
        }

        public void Dispose() => this.Close();

        public void Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();

        public IList Write(IList content)
        {
            IDisposable beginModify()
            {
                if (this.provider is IJsonFsRootNodeModification jsonFsRootNodeModification)
                    return jsonFsRootNodeModification.BeginModify();

                throw new InvalidOperationException($"Provider(type='{this.provider.GetType()}' doesn't support modification");
            }

            if (content.Count > 0)
            {
                if (content[0] is string str)
                {
                    using var handle = beginModify();

                    this.adapter.payload.RemoveAll();
                    foreach (var child in JObject.Parse(str).Children())
                        this.adapter.payload.Add(child);
                }
            }

            return content;
        }
    }
}