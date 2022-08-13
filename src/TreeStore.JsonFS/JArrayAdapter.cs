using System.Globalization;

namespace TreeStore.JsonFS
{
    public sealed class JArrayAdapter : JAdapterBase,
        // ItemCmdletProvider
        IGetItem, ISetItem,
        // ContainerCmdletProvider
        IGetChildItem, IRemoveChildItem, INewChildItem
    {
        internal readonly JArray payload;

        public JArrayAdapter(JArray payload) => this.payload = payload;

        #region IGetItem

        PSObject IGetItem.GetItem(ICmdletProvider provider) => new PSObject();

        #endregion IGetItem

        #region ISetItem

        void ISetItem.SetItem(ICmdletProvider provider, object? value)
        {
            using var handle = this.BeginModify(provider);

            if (value is JArray jarray && IsChildToken(jarray))
            {
                this.payload.Clear();

                foreach (var child in jarray)
                    this.payload.Add(child);
            }
            else if (value is string json)
            {
                var jarrayFromString = JArray.Parse(json);

                this.payload.Clear();

                foreach (var child in jarrayFromString)
                    this.payload.Add(child);
            }
        }

        #endregion ISetItem

        #region IGetChildItem

        bool IGetChildItem.HasChildItems(ICmdletProvider provider)
            => this.payload.Where(jt => IsChildToken(jt)).Any();

        IEnumerable<ProviderNode> IGetChildItem.GetChildItems(ICmdletProvider provider)
            => this.payload.Select((jt, idx) => this.CreateChildNode(provider, jt, idx));

        private ProviderNode CreateChildNode(ICmdletProvider provider, JToken value, int index)
            => new ContainerNode(provider, index.ToString(CultureInfo.InvariantCulture), this.CreateChildAdapter(value));

        private IServiceProvider CreateChildAdapter(JToken value) => value switch
        {
            JArray jarray => new JArrayAdapter(jarray),

            JObject jobject => new JObjectAdapter(jobject),

            _ => throw new NotImplementedException($"Can't create adapter for token(type='{value.GetType()}')")
        };

        #endregion IGetChildItem

        #region IRemoveChildItem

        void IRemoveChildItem.RemoveChildItem(ICmdletProvider provider, string childName, bool recurse)
        {
            if (!int.TryParse(childName, out var index))
                return;

            if (index >= this.payload.Count)
                return;

            var childNode = this.CreateChildAdapter(this.payload[index]);

            if (!recurse && (childNode.GetRequiredService<IGetChildItem>()?.HasChildItems(provider) ?? false))
                return;

            var childItem = this.payload[index];

            using var handle = this.BeginModify(provider);

            this.payload.RemoveAt(index);
        }

        #endregion IRemoveChildItem

        #region INewChildItem

        /// <inheritdoc/>
        object? INewChildItem.NewChildItemParameters(string childName, string itemTypeName, object newItemValue)
            => new NewChildItemParameters();

        /// <inheritdoc/>
        NewChildItemResult INewChildItem.NewChildItem(ICmdletProvider provider, string? childName, string? itemTypeName, object? newItemValue)
        {
            var newValue = this.EvaluateNewItemValue(itemTypeName, newItemValue);

            if (string.IsNullOrEmpty(childName))
            {
                return AppendNewArrrayItem(provider, newValue);
            }

            if (!int.TryParse(childName, out var index))
            {
                return new NewChildItemResult(false, null, null);
            }

            if (provider.DynamicParameters is NewChildItemParameters parameters)
            {
                if (index < this.payload.Count && parameters.Insert.ToBool())
                {
                    return this.InsertNewArrayItem(provider, index, newValue);
                }
            }

            return new NewChildItemResult(false, null, null);
        }

        private JToken EvaluateNewItemValue(string? itemTypeName, object? newItemValue)
        {
            return newItemValue switch
            {
                string stringValue => JObject.Parse(stringValue),

                _ => new JObject()
            };
        }

        private NewChildItemResult AppendNewArrrayItem(ICmdletProvider provider, JToken newItemValue)
        {
            using var handle = this.BeginModify(provider);

            this.payload.Add(newItemValue);

            return new NewChildItemResult(true, (this.payload.Count - 1).ToString(CultureInfo.InvariantCulture), this.CreateChildAdapter(newItemValue));
        }

        private NewChildItemResult InsertNewArrayItem(ICmdletProvider provider, int index, JToken newItemValue)
        {
            using var handle = this.BeginModify(provider);

            this.payload.Insert(index, newItemValue);

            return new NewChildItemResult(true, index.ToString(CultureInfo.InvariantCulture), this.CreateChildAdapter(newItemValue));
        }

        #endregion INewChildItem
    }
}