using System.Collections;

namespace Glimpse.API.Library;

public struct SizedCollection<T> : IEnumerable<T>
{
    public IEnumerable<T> Collection;

    public uint Count;

    public SizedCollection(IEnumerable<T> collection, uint count)
    {
        Collection = collection;
        Count = count;
    }

    public IEnumerator<T> GetEnumerator()
        => Collection.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}