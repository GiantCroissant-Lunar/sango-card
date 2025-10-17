// Copyright (c) GiantCroissant. All rights reserved.

using System;

namespace SangoCard.Cross.Editor.Module.Attributes;

/// <summary>
/// OrderedEditorModulePhaseAttribute is used to mark methods that should be executed during specific phases of the module loading and unloading process.
/// The order of execution is determined by the Order property.
/// </summary>
[AttributeUsage(
    AttributeTargets.Method,
    AllowMultiple = false,
    Inherited = false)]
public sealed partial class OrderedEditorModulePhaseAttribute : Attribute
{
    public OrderedEditorModulePhaseAttribute(
        int order,
        Phase phase)
    {
        Order = order;
        PhaseType = phase;
    }

    /// <summary>
    /// The phase of loading and unloading the module.
    /// </summary>
    public enum Phase
    {
        LoadBegin = 0,
        LoadEnd = 1,
        UnloadBegin = 2,
        UnloadEnd = 3,
    }

    public int Order { get; }

    public Phase PhaseType { get; }

    public override string ToString()
    {
        return $"Order: {Order} Phase: {PhaseType}";
    }
}
