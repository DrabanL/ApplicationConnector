namespace RabanSoft.ApplicationConnector.DataTransformers
{
    public interface IDataTransformer
    {
        byte[] TransformIn(byte[] data, int offset, int count);

        byte[] TransformOut(byte[] data, int offset, int count);
    }
}
