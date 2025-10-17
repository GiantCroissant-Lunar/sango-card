using System.IO;
using NUnit.Framework;
using SangoCard.Cross.Editor;
using UnityEditor.PackageManager;

namespace SangoCard.Cross.Editor.Tests;

[TestFixture]
public class DefineTests
{
    [Test]
    public void PackageNameMatchesRegisteredPackageInfo()
    {
        // Package manifest path always lives under the canonical package name
        const string manifestAssetPath = "Packages/com.contractwork.sangocard.cross/package.json";

        var packageInfo = PackageInfo.FindForAssetPath(manifestAssetPath);

        Assert.That(packageInfo, Is.Not.Null, "Unable to resolve package info for the cross package.");
        Assert.That(
            Define.PackageName,
            Is.EqualTo(packageInfo!.name),
            "Define.PackageName must mirror the manifest name so PackageInfo lookups succeed.");
    }

    [Test]
    public void SettingsDirectoryExistsForDeclaredPackageName()
    {
        var settingsPath = Path.Combine(
            "Packages",
            Define.PackageName,
            "Package Resources",
            "settings");

        Assert.That(
            Directory.Exists(settingsPath),
            Is.True,
            $"Expected shared settings folder at '{settingsPath}'.");
    }
}
