namespace Reporting.Abstractions.Attributes;

using System;

/// <summary>
/// Marks a class as a report provider with a unique identifier.
/// Used by source generators to create registration stubs.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class ReportProviderAttribute : Attribute
{
    public ReportProviderAttribute(string id) => Id = id;

    /// <summary>Unique identifier for the report provider.</summary>
    public string Id { get; }

    /// <summary>Human-readable name for the report.</summary>
    public string? Name { get; set; }

    /// <summary>Version of the report provider.</summary>
    public string? Version { get; set; }

    /// <summary>Category for grouping reports (e.g., "Card", "Player", "Game").</summary>
    public string? Category { get; set; }
}

/// <summary>
/// Marks a class as a report data provider that supplies data for reports.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class ReportDataProviderAttribute : Attribute
{
    public ReportDataProviderAttribute(string id) => Id = id;

    /// <summary>Unique identifier for the data provider.</summary>
    public string Id { get; }

    /// <summary>Version of the data provider.</summary>
    public string? Version { get; set; }
}

/// <summary>
/// Marks a method or class as a report action that can be triggered.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class ReportActionAttribute : Attribute
{
    public ReportActionAttribute(string id) => Id = id;

    /// <summary>Unique identifier for the action.</summary>
    public string Id { get; }

    /// <summary>Display name shown in UI.</summary>
    public string? DisplayName { get; set; }
}

/// <summary>
/// Marks a class as providing a report template.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class ReportTemplateAttribute : Attribute
{
    public ReportTemplateAttribute(string id, string contentType)
    {
        Id = id;
        ContentType = contentType;
    }

    /// <summary>Unique identifier for the template.</summary>
    public string Id { get; }

    /// <summary>Content type (e.g., "text/markdown", "application/json").</summary>
    public string ContentType { get; }
}
