using System.Linq;

namespace RabanSoft.ApplicationConnector.DataTransformers
{
    class DefaultDataTransformer : IDataTransformer
    {
        byte[] IDataTransformer.TransformIn(byte[] data, int offset, int count) => data.Skip(offset).Take(count).ToArray();
        byte[] IDataTransformer.TransformOut(byte[] data, int offset, int count) => data.Skip(offset).Take(count).ToArray();
    }
}
