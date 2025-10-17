/// <summary>
/// .NET build interface implementation for Build class.
///
/// ARCHITECTURE NOTE (R-CODE-090):
/// This partial class file contains ONLY the IDotNetBuild interface implementation.
/// The base NukeBuild inheritance is in Build.cs.
/// File naming: Build.DotNetBuild.cs (interface name without 'I' prefix)
/// This separation improves code organization and maintainability.
/// </summary>
partial class Build : IDotNetBuild
{
    // Interface targets are provided by IDotNetBuild default members.
    // Override defaults here if needed for repo-specific behavior.
}
