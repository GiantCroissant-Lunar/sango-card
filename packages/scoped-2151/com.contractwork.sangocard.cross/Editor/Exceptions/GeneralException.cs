// Copyright (c) GiantCroissant. All rights reserved.

namespace SangoCard.Cross.Editor.Exceptions;

/// <summary>
/// GeneralException is the base class for all exceptions thrown by the Yokan framework.
/// </summary>
[JetBrains.Annotations.PublicAPI]
public class GeneralException : System.Exception
{
    public GeneralException()
    {
    }

    public GeneralException(string message) : base(message)
    {
    }

    public GeneralException(
        string message,
        System.Exception innerException) : base(message, innerException)
    {
    }
}
