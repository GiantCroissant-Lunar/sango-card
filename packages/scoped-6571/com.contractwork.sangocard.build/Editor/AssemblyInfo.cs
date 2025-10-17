using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

#if UNITY_EDITOR
[assembly: InternalsVisibleTo("SangoCard.Build.Editor")]
#endif

#if UNITY_INCLUDE_TESTS
[assembly: InternalsVisibleTo("SangoCard.Build.Editor.Tests")]
[assembly: InternalsVisibleTo("SangoCard.Build.Tests")]
#endif
