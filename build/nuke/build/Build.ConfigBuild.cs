using Nuke.Common;

/// <summary>
/// Config build interface implementation for Build class.
///
/// ARCHITECTURE NOTE (R-CODE-090):
/// This partial class file contains ONLY the IConfigBuild interface implementation.
/// The base NukeBuild inheritance is in Build.cs.
/// File naming: Build.ConfigBuild.cs (interface name without 'I' prefix)
/// This separation improves code organization and maintainability.
/// </summary>
partial class Build : IConfigBuild
{
    // Interface implementation is provided by IConfigBuild default interface members
    // Override default members here if custom implementation is needed
}
