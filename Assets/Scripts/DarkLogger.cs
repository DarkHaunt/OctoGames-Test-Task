using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Game.Scripts.Common.Infrastructure.Logger
{
   public enum DarkLogTag
   {
      None = 0,
      
      Gameplay = 1,
      Firebase = 2,
      Tenjin = 3,
      System = 4,
      ADS = 5,
      FMOD = 6,
      UI = 7,
      
      Editor = 999,
   }
   
   // My logger for colored and specialized logs
   public static class DarkLogger
   {
      private static readonly ILogger Logger = Debug.unityLogger;

      [HideInCallstack]
      public static void Log(string message, Color? color = null, DarkLogTag tag = DarkLogTag.None, Object context = null)
      {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
         var final = ValidateFinalMessage(message, color, tag);
         Logger.Log(LogType.Log, message: final, context: context);
#endif
      }

      [HideInCallstack]
      public static void LogWarning(string message, Color? color = null, DarkLogTag tag = DarkLogTag.None, Object context = null)
      {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
         var final = ValidateFinalMessage(message, color, tag);
         Debug.LogWarning(final, context);
#endif
      }

      [HideInCallstack]         
      public static void LogError(string message,  Color? color = null, DarkLogTag tag = DarkLogTag.None, Object context = null)
      {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
         var final = ValidateFinalMessage(message, color, tag);
         Debug.LogError(final, context);
#endif
      }

      private static string ValidateFinalMessage(string message, Color? color, DarkLogTag tag)
      {
         var tagString = tag == DarkLogTag.None ? "" : $"[{tag}]";
         var colorPrefix = color == null ? "" : $"<color=#{ColorUtility.ToHtmlStringRGB(color.Value)}>";
         var colorSuffix = color == null ? "" : "</color>";

         return $"{colorPrefix}{tagString} {message}{colorSuffix}";
      }
   }
}