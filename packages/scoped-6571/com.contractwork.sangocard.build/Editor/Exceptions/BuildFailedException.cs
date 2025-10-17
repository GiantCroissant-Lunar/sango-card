// Copyright (c) GiantCroissant. All rights reserved.

namespace SangoCard.Build.Editor.Exceptions;

/// <summary>
/// BuildException is the build exception class.
/// </summary>
[JetBrains.Annotations.PublicAPI]
public class BuildFailedException : UnityEditor.Build.BuildFailedException
{
    public BuildFailedException(string message) : base(message)
    {
    }
}
