using SangoCard.Build.Tool.Core.Models;

namespace SangoCard.Build.Tool.Messages;

/// <summary>
/// Message published when cache is populated.
/// </summary>
/// <param name="ItemCount">Number of items added to cache.</param>
/// <param name="SourcePath">Source path where items were copied from.</param>
public record CachePopulatedMessage(int ItemCount, string SourcePath);

/// <summary>
/// Message published when an item is added to cache.
/// </summary>
/// <param name="Item">The cache item that was added.</param>
public record CacheItemAddedMessage(CacheItem Item);

/// <summary>
/// Message published when an item is removed from cache.
/// </summary>
/// <param name="ItemPath">Path of the item that was removed.</param>
public record CacheItemRemovedMessage(string ItemPath);

/// <summary>
/// Message published when cache is cleaned.
/// </summary>
/// <param name="RemovedCount">Number of items removed.</param>
public record CacheCleanedMessage(int RemovedCount);
