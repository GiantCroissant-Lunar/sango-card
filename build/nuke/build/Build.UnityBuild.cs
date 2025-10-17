using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;

/// <summary>
/// Unity build interface implementation for Build class.
///
/// ARCHITECTURE NOTE (R-CODE-090):
/// This partial class file contains ONLY the IUnityBuild interface implementation.
/// The base NukeBuild inheritance is in Build.cs.
/// File naming: Build.UnityBuild.cs (interface name without 'I' prefix)
/// This separation improves code organization and maintainability.
/// </summary>
partial class Build : IUnityBuild
{
    public AbsolutePath UnityProjectPath => RootDirectory / "projects" / "client";
    // Interface implementation is provided by IUnityBuild default interface members
    // Override default members here if custom implementation is needed
}
