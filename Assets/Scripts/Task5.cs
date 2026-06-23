using System;
using System.Collections.Generic;
using System.Linq;
using Game.Scripts.Common.Infrastructure.Logger;
using UnityEngine;
using Object = UnityEngine.Object;

// Base entity component. It is not abstract, because it is only responsible for State handling, not business logic
public class Entity : MonoBehaviour
{
   public EntityState State { get; private set; } = EntityState.Uninitialized;

   // Internal, if we're using assdef separation, to avoid users manipulate with satus by themselves
   internal void SetState(EntityState state) => State = state;
}

public sealed class EntityService : IDisposable
{
   // Mapping entities
   private readonly HashSet<Entity> _entities = new();

   public void Register(Entity entity)
   {
      if (entity == null) 
         throw new ArgumentNullException(nameof(entity));
      
      if (!_entities.Add(entity))
         throw new InvalidOperationException($"Entity '{entity.name}' is already registered.");
      
      DarkLogger.Log($"Entity '{entity.name}' registered.", Color.green);
   }

   public void Unregister(Entity entity)
   {
      Validate(entity);
      _entities.Remove(entity);
      entity.SetState(EntityState.Destroyed);
      
      var name = entity.name;
      Object.Destroy(entity.gameObject);
      
      DarkLogger.Log($"Entity '{name}' unregistered.", Color.red);
   }

   public void Enable(Entity entity)
   {
      Validate(entity);
      entity.SetState(EntityState.Active);
      entity.gameObject.SetActive(true); // idk if it is needed to handle, but there it is
      
      DarkLogger.Log($"Entity '{entity.name}' enabled.", Color.yellow);
   }

   public void Disable(Entity entity)
   {
      Validate(entity);
      entity.SetState(EntityState.Disabled);
      entity.gameObject.SetActive(false);
      
      DarkLogger.Log($"Entity '{entity.name}' disabled.", Color.yellow);
   }

   // Api for getting entities by active status. Could also use predicates Func or Specification pattern
   public IEnumerable<Entity> GetEntities(bool activeOnly = false) =>
      activeOnly
         ? _entities.Where(e => e.State == EntityState.Active)
         : _entities;

   // Destroy all entities on dispose, to avoid leaks
   public void Dispose()
   {
      foreach (Entity entity in _entities)
      {
         entity.SetState(EntityState.Destroyed);
         Object.Destroy(entity.gameObject);
      }
      _entities.Clear();
   }

   // Extra validation for avoiding double track entity
   private void Validate(Entity entity)
   {
      if (!_entities.Contains(entity))
         throw new InvalidOperationException($"Entity '{entity.name}' is not registered.");
   }
}

public enum EntityState
{
   Unknown = 0,
   
   Uninitialized = 1,
   Active = 2,
   Disabled = 3,
   Destroyed = 4
}