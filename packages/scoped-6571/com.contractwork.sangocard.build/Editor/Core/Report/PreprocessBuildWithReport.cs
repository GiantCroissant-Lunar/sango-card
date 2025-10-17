using System.IO;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SangoCard.Build.Editor.Report;

internal class PreprocessBuildWithReport : IPreprocessBuildWithReport
{
#pragma warning disable IDE1006
    public int callbackOrder => 100;
#pragma warning restore IDE1006

    private ILogger<PreprocessBuildWithReport>? _logger;

    private ILogger<PreprocessBuildWithReport> Log =>
        _logger ??= new NullLogger<PreprocessBuildWithReport>();

    public void OnPreprocessBuild(BuildReport report)
    {
        var loggerFactory = Splat.Locator.Current.GetService(typeof(ILoggerFactory)) as ILoggerFactory;
        _logger = loggerFactory?.CreateLogger<PreprocessBuildWithReport>() ?? new NullLogger<PreprocessBuildWithReport>();

        var javaHome = System.Environment.GetEnvironmentVariable("JAVA_HOME");
        var path = System.Environment.GetEnvironmentVariable("PATH");
        Log.LogDebug("[Build] JAVA_HOME: {JavaHome}", javaHome ?? "(not set)");
        Log.LogDebug("[Build] PATH: {Path}", path);
    }
}
