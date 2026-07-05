#if NETSTANDARD2_1 || NETSTANDARD2_0

namespace System.Runtime.CompilerServices;

/// <summary>Polyfill for <c>init</c> accessors when targeting Unity / netstandard2.1.</summary>
internal static class IsExternalInit
{
}

#endif
