using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

// Simplified example of service, that handles configs such as Scriptables or other types
public interface IStaticDataService
{
   UniTask Initialize(CancellationToken token = default);
   BaseWindow GetWindowByType(WindowType type);
}

public class StaticDataService : IStaticDataService
{
   private readonly IAssetProvider _assetProvider;

   private Dictionary<WindowType, BaseWindow> _windowPrefabs = new();

   public StaticDataService(IAssetProvider assetProvider)
   {
      _assetProvider = assetProvider;
   }

   public async UniTask Initialize(CancellationToken token = default)
   {
      await UniTask.WhenAll
      (
         LoadWindows()
      ).AttachExternalCancellation(token);
   }

   public BaseWindow GetWindowByType(WindowType type)
   {
      return _windowPrefabs.TryGetValue(type, out BaseWindow window)
         ? window
         : throw new Exception($"Window prefab for type {type} does not exist");
   }

   private async UniTask LoadWindows()
   {
      var windows = await _assetProvider.Load<WindowsConfig>(ConfigsAddresses.WindowConfig);
      _windowPrefabs = new Dictionary<WindowType, BaseWindow>();

      foreach (WindowConfig config in windows.WindowConfigs)
      {
         var prefab = await _assetProvider.Load<BaseWindow>(config.Prefab);
         _windowPrefabs.Add(config.Type, prefab);
      }
   }
}

// Paths for configs in asset database
public static class ConfigsAddresses
{
   public const string WindowConfig = "Window Config";
}

// Simplified interface for asset providing and managing (removed unloading, specialized loading e.c.t)
public interface IAssetProvider
{
   UniTask InitializeAsync(CancellationToken token = default);
   UniTask<T> Load<T>(string key, CancellationToken token = default) where T : class;
   UniTask<TAsset> Load<TAsset>(AssetReference assetReference) where TAsset : MonoBehaviour;
   void ClearAssetReferenceCache();
}

// Using Addressables for handling assets
public class AssetProvider : IAssetProvider
{
   private readonly Dictionary<object, AsyncOperationHandle> _cachedObjects = new();
   private readonly Dictionary<string, AsyncOperationHandle> _assetsRequests = new();

   public async UniTask InitializeAsync(CancellationToken token = default)
   {
      await Addressables
         .InitializeAsync()
         .ToUniTask(cancellationToken: token);
   }
   
   public async UniTask<TAsset> Load<TAsset>(AssetReference assetReference) where TAsset : MonoBehaviour => 
      await LoadAndGetComponent<TAsset>(assetReference.AssetGUID);
   
   public async UniTask<T> LoadAndGetComponent<T>(string key) where T : MonoBehaviour
   {
      var prefab = await Load<GameObject>(key);
      if (prefab != null)
      {
         if (prefab.TryGetComponent(out T component))
            return component;
                
         Debug.LogError($" Failed to get component of type { typeof(T) } from prefab { key } ");
      }
      return default;
   }

   public async UniTask<T> Load<T>(AssetReference assetReference, CancellationToken token = default) where T : Object
   {
      if (!_cachedObjects.TryGetValue(assetReference.RuntimeKey, out AsyncOperationHandle handle))
      {
         handle = assetReference.LoadAssetAsync<GameObject>();
         _cachedObjects.Add(assetReference.RuntimeKey, handle);
      }

      await handle.ToUniTask(cancellationToken: token);
      var obj = handle.Result as GameObject;

      if (obj!.TryGetComponent(out T component) == false)
         Debug.LogError($"Failed to get component of type {typeof(T)} from prefab {component.name}");

      return component;
   }

   public async UniTask<T> Load<T>(string key, CancellationToken token = default) where T : class
   {
      if (!_assetsRequests.TryGetValue(key, out AsyncOperationHandle handle))
      {
         handle = Addressables.LoadAssetAsync<T>(key);
         _assetsRequests.Add(key, handle);
      }

      await handle.ToUniTask(cancellationToken: token);
      return handle.Result as T;
   }

   public async UniTask<T[]> LoadAll<T>(List<string> keys, CancellationToken token = default) where T : class
   {
      var tasks = new List<UniTask<T>>(keys.Count);
      tasks.AddRange(Enumerable.Select(keys, key => Load<T>(key, token)));
      return await UniTask.WhenAll(tasks);
   }

   public void ClearAssetReferenceCache()
   {
      foreach (AsyncOperationHandle handle in _cachedObjects.Values)
         Addressables.Release(handle);

      foreach (AsyncOperationHandle handle in _assetsRequests.Values)
         Addressables.Release(handle);

      _assetsRequests.Clear();
      _cachedObjects.Clear();
   }
}