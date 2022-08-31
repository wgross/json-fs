namespace TreeStore.JsonFS
{
    public class JObjectContentReader : IContentReader
    {
        private readonly JObject jObject;
        private long currentPosition = 0;

        public JObjectContentReader(JObject jobject)
        {
            this.jObject = jobject;
        }

        public void Close()
        {
        }

        public void Dispose() => Close();

        /// <summary>
        /// The JObject is one response item. A second call returns no lines which end the pulling of lines.
        /// </summary>
        public IList Read(long readCount)
        {
            if (this.currentPosition > 0)
                return new List<string>();

            this.currentPosition = readCount;
            return new List<string>() { this.jObject.ToString() };
        }

        /// <summary>
        /// Seek isn't implemented. It is used by the Get-Content -Tail which is only accessible for file system operations
        /// </summary>
        public void Seek(long offset, SeekOrigin origin) => throw new NotImplementedException("Only with FS Provider");
    }
}