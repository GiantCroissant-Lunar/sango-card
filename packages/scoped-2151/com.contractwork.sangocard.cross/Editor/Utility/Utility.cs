using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;

namespace SangoCard.Cross.Editor;

/// <summary>
/// Editor use utility functions.
/// </summary>
public static class Utility
{
    /// <summary>
    /// Check if the path is directory or not.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static bool IsDirectory(string path) => File
        .GetAttributes(path)
        .HasFlag(FileAttributes.Directory);

    /// <summary>
    /// Check if given name is hidden file or directory for Unity.
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public static bool IsHiddenFileOrDirectory(string name) => name[^1] == '~' || name[0] == '.';

    /// <summary>
    /// Load assets of type from the given folders.
    /// </summary>
    /// <param name="searchInFolders"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static IEnumerable<T> LoadAssetsOfType<T>(string[]? searchInFolders = default)
        where T : UnityEngine.Object
    {
        return AssetDatabase
            .FindAssets($"t:{typeof(T)}", searchInFolders)
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<T>)
            .Where(x => x);
    }

    /// <summary>
    /// Load asset of type from the given folders.
    /// </summary>
    /// <param name="searchInFolders"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T? LoadAssetOfType<T>(string[]? searchInFolders = default)
        where T : UnityEngine.Object
    {
        var guid = AssetDatabase
            .FindAssets($"t:{typeof(T)}", searchInFolders)
            .FirstOrDefault();

        return guid != null ?
            AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid)) : null;
    }
}
