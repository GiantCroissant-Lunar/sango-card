/// <summary>
/// Report build interface implementation for Build class.
///
/// ARCHITECTURE NOTE (R-CODE-090):
/// This partial class file contains ONLY the IReportBuild interface implementation.
/// The base NukeBuild inheritance is in Build.cs.
/// File naming: Build.ReportBuild.cs (interface name without 'I' prefix)
/// This separation improves code organization and maintainability.
/// </summary>
partial class Build : IReportBuild
{
    // Interface targets are provided by IReportBuild default members.
    // Override defaults to unify versioning and output structure.

    // Ensure report packages go under build/_artifacts/<EffectiveVersion>/report
    public string ReportVersionSuffix => EffectiveVersion;
}
