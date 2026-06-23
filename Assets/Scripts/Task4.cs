using System.Collections.Generic;
using System.Linq;
using Game.Scripts.Common.Infrastructure.Logger;
using R3;
using UnityEngine;
using UnityEngine.UI;

public class CharactersViewOLD : MonoBehaviour
{
   // 1. List of components, that we`re going to use is missing
   [SerializeField] private List<Transform> _characters;
   // 2. Missing Text component
   
   void FixedUpdate()
   {
      float totalValue = 0f;
      foreach (Transform characterTransform in _characters)
      {
         // To problem 1
         Character character =
            characterTransform.gameObject.GetComponents<Character>();
         totalValue += character != null ? character.Value : 0f;
      }

      string text = string.Format(
         "Characters: {0} Avg value: {1}",
         _characters.Length, // incorrect API usage
         _characters.Length / totalValue // incorrect logic, and no check for zero dividing
      );
      // To problem 2, no null check
      gameObject.GetComponent<Text>().text = text;
      Debug.Log(text); // logging every frame, in production too
   }
}

// Better solution it is using reactive approach, and i`ll be using R3 for ready production implementation
// but if it's too complicated for project, you've could use simple events in c#
public class CharactersViewNEW : MonoBehaviour
{
   private readonly CompositeDisposable _disposable = new();
   
   [SerializeField] private List<Character> _characters = new();
   [SerializeField] private Text _text;

   private Observable<float> CharactersValue => _characters
      .Where(c => c != null)
      .Select(c => c.ValueChanged)
      .Merge();

   // Usually, I initialize UI in separated service, where i create it, but for simplicity i`ll initialize it here
   private void Awake() =>
      Init();

   public void Init() =>
      CalculateAvgValue();

   // Sub to all changes of values
   public void Enable()
   {
      CharactersValue
         .Subscribe(_ => CalculateAvgValue())
         .AddTo(_disposable);
   }

   // Usub
   public void Disable() =>
      _disposable?.Dispose();

   // To avoid leaks, for last time try to unsub on destroy
   private void OnDestroy() =>
      Disable();

   private void CalculateAvgValue()
   {
      var totalValue = _characters
         .Where(c => c != null)
         .Sum(c => c.CurrentValue);

      var avg = _characters.Count == 0 ? 0 : totalValue / _characters.Count; // need to check for 0 dividing 
      string text = $"Characters: {_characters.Count} / {avg}";
      
      if (_text != null)
         _text.text = text;
      else
         DarkLogger.LogWarning($"{nameof(CharactersViewNEW)} could not find text component", color: Color.yellow, tag: DarkLogTag.UI);
      
      // Logger, that disabled in production
      DarkLogger.Log(text, color: Color.white, tag: DarkLogTag.UI);
   }
}

public class Character : MonoBehaviour
{
   // Using reactive property, that tracks changes of value and notifies observers
   private readonly ReactiveProperty<float> _value = new(0f);
   public ReadOnlyReactiveProperty<float> ValueChanged => _value;
   public float CurrentValue => _value.Value;

   public void ChangeValue(float newValue) =>
      _value.Value = newValue;
}