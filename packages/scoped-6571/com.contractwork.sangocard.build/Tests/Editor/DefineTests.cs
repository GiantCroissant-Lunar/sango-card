using System.IO;
using NUnit.Framework;
using SangoCard.Build.Editor;
using UnityEditor.PackageManager;

namespace SangoCard.Build.Editor.Tests;

[TestFixture]
public class DefineTests
{
    [Test]
    public void PackageNameMatchesRegisteredPackageInfo()
    {
        const string manifestAssetPath = "Packages/com.contractwork.sangocard.build/package.json";

        var packageInfo = PackageInfo.FindForAssetPath(manifestAssetPath);

        Assert.That(packageInfo, Is.Not.Null, "Unable to resolve package info for the build package.");
        Assert.That(
            Define.PackageName,
            Is.EqualTo(packageInfo!.name),
            "Define.PackageName must mirror the manifest name so consuming packages can resolve it.");
    }

    [Test]
    public void PackageManifestExistsForDeclaredPackageName()
    {
        var manifestPath = Path.Combine("Packages", Define.PackageName, "package.json");

        Assert.That(
            File.Exists(manifestPath),
            Is.True,
            $"Expected manifest at '{manifestPath}'.");
    }
}
