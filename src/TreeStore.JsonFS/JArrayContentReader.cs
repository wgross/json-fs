namespace TreeStore.JsonFS
{
    public class JArrayContentReader : IContentReader
    {
        private readonly JArray jArray;
        private int currentPosition;

        public JArrayContentReader(JArray jobject)
        {
            this.jArray = jobject;
            this.currentPosition = 0;
        }

        public void Close()
        {
        }

        public void Dispose() => Close();

        public IList Read(long readCount)
        {
            if (this.currentPosition >= this.jArray.Count)
                return new List<string>();

            var result = this.jArray.Skip(this.currentPosition).Take((int)readCount).Select(i => i.ToString()).ToList();
            this.currentPosition += (int)readCount;
            return result;
        }

        public void Seek(long offset, SeekOrigin origin) => throw new NotImplementedException("Only with FS Provider");
    }
}