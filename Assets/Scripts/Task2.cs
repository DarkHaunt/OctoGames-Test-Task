using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Scripts.Common.Infrastructure.Logger;
using Newtonsoft.Json;
using UnityEngine;
using Formatting = Newtonsoft.Json.Formatting;

/// <summary>
///  Pre-saved keys for path loading
/// </summary>
public static class SaveKeysData
{
   public static readonly List<string> AllKeys = new() { PlayerProgressKey, SettingsKey };

   public const string PlayerProgressKey = "Progress";
   public const string SettingsKey = "Settings";
}

/// <summary>
/// API for serving savings and loading data
/// </summary>
public interface ISaver
{
   UniTask SaveData<T>(string relativePath, T data, CancellationToken ct);
   UniTask<T> LoadData<T>(string relativePath, CancellationToken ct);
}

/// <summary>
/// Service for saving and loading data by using JSON file system with persistent data path
/// </summary>
public class JsonSaver : ISaver
{
   public async UniTask SaveData<T>(string relativePath, T data, CancellationToken ct)
   {
      string path = GetPath(relativePath);
      try
      {
         if (File.Exists(path))
            File.Delete(path);

         await using FileStream stream = File.Create(path);
         stream.Close();


#if UNITY_EDITOR
         var formatting = Formatting.Indented;
#else
            var formatting = Formatting.None;
#endif
         await File.WriteAllTextAsync(path, JsonConvert.SerializeObject(data, formatting), ct);
      }
      catch (OperationCanceledException ec)
      {
         DarkLogger.Log($"Saving file {path} was canceled", color: Color.red, tag: DarkLogTag.System);
      }
      catch (Exception e)
      {
         Debug.LogError($"Enable to save data due to: {e.Message} {e.StackTrace}");
      }
   }

   public async UniTask<T> LoadData<T>(string relativePath, CancellationToken ct)
   {
      string path = GetPath(relativePath);
      if (!File.Exists(path))
      {
         DarkLogger.Log($"Cannot load file at {path}. File doesn't exist", color: Color.yellow, tag: DarkLogTag.System);
         return default;
      }

      try
      {
         string data = await File.ReadAllTextAsync(path, ct).AsUniTask();
         return JsonConvert.DeserializeObject<T>(data);
      }
      catch (OperationCanceledException ec)
      {
         DarkLogger.Log($"Loading file {path} was canceled", color: Color.red, tag: DarkLogTag.System);
         return default;
      }
      catch (Exception e)
      {
         DarkLogger.Log($"Failed to load data due to: {e.Message} {e.StackTrace}", color: Color.red, tag: DarkLogTag.System);
         return default;
      }
   }

   private static string GetPath(string relativePath) =>
      Application.persistentDataPath + "/" + relativePath + ".json";

   public static void ClearAllData()
   {
      foreach (string path in SaveKeysData.AllKeys
                  .Select(GetPath)
                  .Where(File.Exists))
      {
         File.Delete(path);
         DarkLogger.Log($"Delete file at path {path}", color: new Color(0.9f, 0.54f, 0.11f, 1f), tag: DarkLogTag.System);
      }
   }
}