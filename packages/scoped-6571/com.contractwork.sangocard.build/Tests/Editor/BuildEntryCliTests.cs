using System;
using NUnit.Framework;

namespace SangoCard.Build.Editor.Tests;

[TestFixture]
public class BuildEntryCliTests
{
    [Test]
    public void TryParseArguments_MissingRequiredOptionsProducesErrors()
    {
        var parsed = BuildEntry.TryParseArguments(
            Array.Empty<string>(),
            out _,
            out var error);

        Assert.That(parsed, Is.False);
        Assert.That(error, Does.Contain("Missing required option"));
    }

    [Test]
    public void TryParseArguments_ParsesValidArguments()
    {
        var args = new[]
        {
            "--outputPath", "Builds/out",
            "--buildVersion", "1.2.3",
            "--buildProfileName", "Windows",
            "--buildPurpose", "UnityPlayer",
            "--buildTarget", "StandaloneWindows64"
        };

        var success = BuildEntry.TryParseArguments(
            args,
            out var parsed,
            out var error);

        Assert.That(success, Is.True);
        Assert.That(error, Is.Empty);
        Assert.That(parsed.OutputPath, Is.EqualTo("Builds/out"));
        Assert.That(parsed.BuildVersion, Is.EqualTo("1.2.3"));
        Assert.That(parsed.BuildProfileName, Is.EqualTo("Windows"));
        Assert.That(parsed.BuildPurpose, Is.EqualTo("UnityPlayer"));
        Assert.That(parsed.BuildTarget, Is.EqualTo("StandaloneWindows64"));
    }
}
