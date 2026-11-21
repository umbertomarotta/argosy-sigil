// SPDX-License-Identifier: MPL-2.0
//
// Stand-ins for language-support types .NET Framework 4.6.2 does not ship, so the binding's C# 9-12 syntax
// (init-only properties, records, named tuples) compiles for it. They only exist for this target.
#if NET462
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property |
                    AttributeTargets.ReturnValue | AttributeTargets.Class | AttributeTargets.Struct)]
    internal sealed class TupleElementNamesAttribute : Attribute
    {
        public TupleElementNamesAttribute(string[] transformNames) { TransformNames = transformNames; }
        public string[] TransformNames { get; }
    }
}

namespace System
{
    public struct ValueTuple<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;
        public ValueTuple(T1 item1, T2 item2) { Item1 = item1; Item2 = item2; }
    }
}
#endif
