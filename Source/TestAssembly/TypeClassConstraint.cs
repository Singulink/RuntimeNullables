using System.Diagnostics.CodeAnalysis;

namespace TestAssembly;

public class TypeClassConstraint<T> where T : class
{
    public T Direct(T param)
    {
        return param;
    }

    public T? AllowNull([AllowNull] T param)
    {
        return param;
    }

    public void MaybeNull([MaybeNull] ref T param)
    {
        param = null;
    }

    public T? Nullable(T? param)
    {
        return param;
    }

    public void NullableRef(ref T? param)
    {
        param = null;
    }

    public class Nested
    {
        public T Direct(T param)
        {
            return param;
        }
    }
}