using System;
using System.Linq;
using System.Reflection;
using UnityEditor;

namespace SangoCard.Cross.Editor.Module;

using SangoCard.Cross.Editor.Module.Attributes;

[InitializeOnLoad]
public static class OrderedEditorModulePhaseInvoker
{
    static OrderedEditorModulePhaseInvoker()
    {
        // Always unregister first to avoid duplicates
        EditorApplication.quitting -= OnEditorQuitting;
        EditorApplication.quitting += OnEditorQuitting;

        // Defer phase calls until the Editor is ready
        EditorApplication.delayCall += () =>
        {
            try
            {
                InvokeOrdered(OrderedEditorModulePhaseAttribute.Phase.LoadBegin);
                InvokeOrdered(OrderedEditorModulePhaseAttribute.Phase.LoadEnd);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Failed in Ordered Phase Initialization: {ex}");
            }
        };
    }

    public static void Unload()
    {
        InvokeOrdered(OrderedEditorModulePhaseAttribute.Phase.UnloadBegin);
        InvokeOrdered(OrderedEditorModulePhaseAttribute.Phase.UnloadEnd);
    }

    private static void OnEditorQuitting()
    {
        Unload();
        EditorApplication.quitting -= OnEditorQuitting;
    }

    private static void InvokeOrdered(OrderedEditorModulePhaseAttribute.Phase phase)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.FullName.StartsWith("UnityEditor") && !a.IsDynamic);
        var methods = assemblies
            .SelectMany(a =>
            {
                try
                {
                    return a.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    UnityEngine.Debug.LogWarning($"Partial failure in GetTypes() from {a.FullName}");
                    foreach (var loaderEx in ex.LoaderExceptions)
                    {
                        UnityEngine.Debug.LogError(loaderEx);
                    }
                    return ex.Types.Where(t => t != null);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"Unhandled error in GetTypes() from {a.FullName}: {ex}");
                    return Array.Empty<Type>();
                }
            })
            .SelectMany(t => t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            .Select(m => new
            {
                Method = m,
                Attr = m.GetCustomAttribute<OrderedEditorModulePhaseAttribute>()
            })
            .Where(x => x.Attr != null && x.Attr.PhaseType == phase)
            .OrderBy(x => x.Attr.Order)
            .ToList();

        foreach (var item in methods)
        {
            try
            {
                item.Method.Invoke(null, null);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Failed to invoke {item.Method.DeclaringType.FullName}.{item.Method.Name}: {ex}");
            }
        }
    }
}
