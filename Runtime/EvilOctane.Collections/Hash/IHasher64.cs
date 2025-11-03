namespace EvilOctane.Collections
{
    public interface IHasher64<T>
    {
        ulong CalculateHash(in T value);
    }
}
