﻿namespace TreeStore.JsonFS;

public sealed class JArrayContentWriter(ICmdletProvider provider, JArrayAdapter jArrayAdapter) : IContentWriter
{
    private readonly JArrayAdapter adapter = jArrayAdapter;
    private readonly ICmdletProvider provider = provider;
    private readonly List<JObject> newArrayItems = new();

    public void Close()
    {
        if (this.provider is not IJsonFsRootNodeModification jsonFsRootNodeModification)
            throw new InvalidOperationException($"Provider(type='{this.provider.GetType()}' doesn't support modification");

        if (this.newArrayItems.Any())
        {
            using var handle = jsonFsRootNodeModification.BeginModify();

            this.adapter.GetRequiredService<IClearItemContent>().ClearItemContent(this.provider);

            this.newArrayItems.ForEach(ai => this.adapter.AppendNewArrrayItem(this.provider, ai));
        }
    }

    public void Dispose() => Close();

    public void Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();

    public IList Write(IList content)
    {
        foreach (var c in content)
        {
            if (c is string contentString)
            {
                this.newArrayItems.Add(JObject.Parse(contentString));
            }
        }

        return content;
    }
}