namespace RabanSoft.ApplicationConnector.DataTransformers
{
    public abstract class XorDataTransformer : IDataTransformer
    {
        public abstract byte[] SecretKey { get; set; }

        byte[] IDataTransformer.TransformIn(byte[] data, int offset, int count) => ProcessData(data, offset, count);

        byte[] IDataTransformer.TransformOut(byte[] data, int offset, int count) => ProcessData(data, offset, count);

        internal virtual byte[] ProcessData(byte[] data, int offset, int count)
        {
            var outData = new byte[count];
            for (int i = 0; i < count; ++i)
                outData[i] = (byte)(data[i + offset] ^ SecretKey[i % SecretKey.Length]);
            return outData;
        }
    }
}
