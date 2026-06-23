   using System;
   using System.Collections.Generic;
   using DG.Tweening;
   using Game.Scripts.Common.Infrastructure.Logger;
   using UnityEngine;
   using UnityEngine.AddressableAssets;
   using UnityEngine.UI;
   using Zenject;

   // In my experience, attempting to use inheritance or merge windows based on certain fields or properties
   // is not a good solution, as the windows may differ drastically from one another.
   // Therefore, apart from the basic components, all other windows are implementations of specific classes.
   
   public class SomePopupExample : BaseWindow
   {
      [SerializeField] private Text _titleText;
      [SerializeField] private Text _bodyText;
      [field: SerializeField] public Button SomeButton { get; private set; } // assign callbacks outside
      
      // Setup is called by service, that handles concrete window
      public void Setup(string titleText, string bodyText, Action callbackForSomeButton = null)
      {
         if(_titleText != null)
            _titleText.text = titleText;
         
         if(_bodyText != null)
            _bodyText.text = bodyText;
         
         if(SomeButton != null)
            SomeButton.onClick.AddListener(() => callbackForSomeButton?.Invoke());
      }

      protected override void Cleanup()
      {
         if(SomeButton != null)
            SomeButton.onClick.RemoveAllListeners();
         
         base.Cleanup();
      }

      protected override void Initialize() { }
   }

   public class BaseWindow : MonoBehaviour
   {
      // Added only fields for animation purposes, but i'd do another separated animation system, based on components, but not this time
      [SerializeField] private CanvasGroup _group;
      [SerializeField] private GameObject _animatedPanel;

      // DOTween for animation, classic
      private Tween _tween;
      
      public WindowType Type { get; protected set; }

      private void Awake() =>
         OnAwake();

      private void Start() =>
         Initialize();

      public void Show()
      {
         _group.alpha = 1f;
         _group.interactable = true;
         _group.blocksRaycasts = true;
         
         if (_animatedPanel)
            AnimateAppear();

         SubscribeUpdates();
      }

      public void Hide()
      {
         _group.alpha = 0f;
         _group.interactable = false;
         _group.blocksRaycasts = false;

         Cleanup();
         UnsubscribeUpdates();
      }

      private void AnimateAppear()
      {
         _tween?.Kill(complete: true);
         _tween = _animatedPanel.transform.DOPunchScale(Vector3.one * 0.08f, 0.2f, vibrato: 0, elasticity: 0f);
      }

      private void OnDestroy()
      {
         _tween?.Kill();
         UnsubscribeUpdates();
         Cleanup();
         Dispose();
      }

      protected virtual void OnAwake() { }
      protected virtual void Initialize() { }
      protected virtual void SubscribeUpdates() { }
      protected virtual void UnsubscribeUpdates() { }
      protected virtual void Cleanup() { }
      protected virtual void Dispose() { }
   }
   
   // Handles windows in application
   public sealed class WindowService : IDisposable
   {
      private static readonly Color32 LogColor = new(168, 201, 0, 255);
      private readonly WindowFactory _windowFactory;

      private readonly Dictionary<WindowType, BaseWindow> _windows = new(9);

      public WindowService(WindowFactory windowFactory)
      {
         _windowFactory = windowFactory;
      }

      public void Setup(Transform windowsParent)
      {
         _windowFactory.SetWindowsParent(windowsParent);

         PrewarmWindows();
      }

      private void PrewarmWindows()
      {
         _windows.Add(WindowType.SomePopup, _windowFactory.CreateWindow<SomePopupExample>(WindowType.SomePopup));

         foreach (BaseWindow window in _windows.Values)
            window.Hide();
      }

      public T Open<T>(WindowType windowType) where T : BaseWindow
      {
         var window = GetWindow<T>(windowType);
         
         if(window == null)
            throw new Exception($"Window prefab for type {windowType} does not exist");
         
         window.Show();

         DarkLogger.Log($"Window {windowType} opened", color: LogColor, tag: DarkLogTag.Gameplay);

         return window;
      }

      public void Close(WindowType windowType)
      {
         var window = GetWindow<BaseWindow>(windowType);
         if (window == null) return;
         
         window.Hide();
         DarkLogger.Log($"Window {windowType} closed", color: LogColor, tag: DarkLogTag.Gameplay);
      }

      private T GetWindow<T>(WindowType windowType) where T : BaseWindow
      {
         if (_windows.TryGetValue(windowType, out BaseWindow window))
            return window as T;

         return null;
      }

      public void Dispose()
      {
         foreach (BaseWindow window in _windows.Values)
            Close(window.Type);
      }
   }
   
   public enum WindowType
   {
      Unknown = 0,
    
      SomePopup = 1
   }
   
   // Creates windows by getting data from configs & instantiating them by DI (Zenject here, for example)
   // Instantiating with DI is crucial, because some windows could have dependencies for services, and be resolved with DI
   public sealed class WindowFactory
   {
      private readonly IStaticDataService _staticData;
      private readonly IInstantiator _instantiator;

      private Transform _windowsParent;

      public WindowFactory(IStaticDataService staticData, IInstantiator instantiator)
      {
         _staticData = staticData;
         _instantiator = instantiator;
      }

      public void SetWindowsParent(Transform parent) =>
         _windowsParent = parent;

      public T CreateWindow<T>(WindowType type) where T : BaseWindow
      {
         var prefab = _staticData.GetWindowByType(type) as T;
         return _instantiator.InstantiatePrefabForComponent<T>(prefab, _windowsParent);
      }

      private BaseWindow PrefabFor(WindowType type) =>
         _staticData.GetWindowByType(type);
   }
   
   [Serializable]
   public class WindowConfig : SerializationNameReceiver
   {
      public WindowType Type;
      public AssetReferenceGameObject Prefab;
    
      protected override string ReceiveName() =>
         Type.ToString();
   }
   
   [CreateAssetMenu(fileName = "WindowsConfig", menuName = "Game/Infrastructure/Windows Config", order = 2)]
   public class WindowsConfig : ScriptableObject
   {
      [field: SerializeField] public List<WindowConfig> WindowConfigs { get; private set; }
   }
   
   // Simple tool, that renames object in editor by some domain values
   public abstract class SerializationNameReceiver : ISerializationCallbackReceiver
   {
      [HideInInspector] public string Name;

      protected abstract string ReceiveName();

      public void OnBeforeSerialize() =>
         Name = ReceiveName();

      public void OnAfterDeserialize() {}
   }

   
   // 3.1 I would add Canvas and corresponging components if we're using windows based UI (each window separated by render),
   // or not (if windows or popups appear on top of root object),
   // CanvasGroup (for fade-in/fade-out completely), Custom component for handling windows (written above) 
   // and animation settings (separated component, but not this time)
   // And under root i'd add fade image (for fade-in/fade-out of background)




