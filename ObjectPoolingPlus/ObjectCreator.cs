using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ObjectPoolingPlus {
    public static class ObjectCreator {
        public static T CreateObject<T>() where T : class => typeof(T) switch {
            _ when typeof(IPoolableObject<T>).IsAssignableFrom(typeof(T)) => 
                UnRestrictedObjectCreator.CreatePoolableObject<T>(),
            _ when typeof(Component).IsAssignableFrom(typeof(T)) => 
                UnRestrictedObjectCreator.CreateComponent<T>(),
            _ when typeof(GameObject).IsAssignableFrom(typeof(T)) => 
                (T)(object)CreateGameObject(),
            _ => Activator.CreateInstance<T>()
        };
        
        public static T CreateObject<T>(T prefab) where T : class => prefab switch {
            IPoolableObject<T> when typeof(Component).IsAssignableFrom(typeof(T)) =>
                UnRestrictedObjectCreator.CreatePoolableComponent(prefab),
            IPoolableObject<T> => 
                UnRestrictedObjectCreator.CreatePoolableObject(prefab),
            Object unityObject => (T)(object)CreateUnityObject(unityObject),
            _ => throw new InvalidOperationException($"Could not create object of type {typeof(T).Name}.")
        };

        public static T CreateUnityObject<T>(T prefab) where T : Object => prefab switch {
            GameObject gameObjectPrefab => (T)(object)CreateGameObject(gameObjectPrefab),
            Component componentPrefab => (T)(object)CreateComponent(componentPrefab),
            _ => throw new InvalidOperationException($"Could not create object of type {typeof(T).Name}.")
        };
        
        public static GameObject CreateGameObject() {
            var obj = new GameObject("Empty_GameObject[CreatedByPool]");
            obj.SetActive(true);
            return obj;
        }
        public static GameObject CreateGameObject(GameObject prefab) {
            prefab.SetActive(false);
            var obj = Object.Instantiate(prefab);
            prefab.SetActive(true);
            
            return obj;
        }
        
        public static T CreateComponent<T>(T prefab) where T : Component =>
            UnRestrictedObjectCreator.CreateComponent(prefab);
        public static T CreateComponent<T>() where T : Component =>
            UnRestrictedObjectCreator.CreateComponent<T>();

        public static T CreatePoolableObject<T>(T poolableObject) where T : class, IPoolableObject<T> => 
            UnRestrictedObjectCreator.CreatePoolableObject(poolableObject);
        public static T CreatePoolableObject<T>() where T : class, IPoolableObject<T>, new() =>
            UnRestrictedObjectCreator.CreatePoolableObject<T>();

        public static T CreatePoolableComponent<T>() where T : Component, IPoolableObject<T> =>
            UnRestrictedObjectCreator.CreatePoolableComponent<T>();

        public static T CreatePoolableComponent<T>(T prefab) where T : Component, IPoolableObject<T> =>
            UnRestrictedObjectCreator.CreatePoolableComponent(prefab);
    }
    
    internal static class UnRestrictedObjectCreator {
        static Dictionary<Type, IPoolableObject> s_unSpecifiedPoolableObjects;
        static Dictionary<Type, IPoolableObject> s_unSpecifiedPoolableComponents;
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void ClearCache() {
            if (s_unSpecifiedPoolableObjects != null)
                for (var i = s_unSpecifiedPoolableObjects.Count - 1; i >= 0; i--) {
                    var kvp = s_unSpecifiedPoolableObjects.ElementAt(i);
                    if (kvp.Value is IDisposable disposable)
                        disposable.Dispose();
                }
            
            if (s_unSpecifiedPoolableComponents != null)
                for (var i = s_unSpecifiedPoolableComponents.Count - 1; i >= 0; i--) {
                    var kvp = s_unSpecifiedPoolableComponents.ElementAt(i);
                    if (kvp.Value is Component component)
                        Object.Destroy(component);
                }
            
            s_unSpecifiedPoolableObjects?.Clear();
            s_unSpecifiedPoolableComponents?.Clear();
        }
        
        internal static T CreateComponent<T>(T prefab) where T : class {
            if (prefab is not Component prefabComponent)
                throw new InvalidOperationException($"Could not create component of type {typeof(T).Name} from prefab.");
            
            prefabComponent.gameObject.SetActive(false);
            var component = Object.Instantiate(prefabComponent);
            prefabComponent.gameObject.SetActive(true);
            return component as T ?? throw new InvalidOperationException($"Could not create component of type {typeof(T).Name} from prefab.");
        }
        internal static T CreateComponent<T>() where T : class {
            try {
                var obj = new GameObject($"{typeof(T).Name}[CreatedByPool]");
                var component = obj.AddComponent(typeof(T));
                obj.SetActive(true);
                return component as T ?? throw new InvalidOperationException($"Could not create component of type {typeof(T).Name}.");
            }
            catch (Exception e) {
                throw new InvalidOperationException($"Could not create component of type {typeof(T).Name}.", e);
            }
        }

        internal static T CreatePoolableObject<T>(T poolableObject) where T : class => typeof(T) switch {
            _ when typeof(Component).IsAssignableFrom(typeof(T)) => 
                CreatePoolableComponent(poolableObject),
            _ => ((IPoolableObject<T>)poolableObject).Create()
        };
        
        internal static T CreatePoolableObject<T>() where T : class {
            if (typeof(Component).IsAssignableFrom(typeof(T)))
                return CreatePoolableComponent<T>();
            
            s_unSpecifiedPoolableObjects ??= new Dictionary<Type, IPoolableObject>();
            s_unSpecifiedPoolableObjects.TryGetValue(typeof(T), out var poolableObject);
            
            if (poolableObject != null)
                return CreatePoolableObject((T)poolableObject);
            
            var obj = Activator.CreateInstance<T>();
            s_unSpecifiedPoolableObjects.Add(typeof(T), (IPoolableObject)obj);
            return CreatePoolableObject(obj);
        }

        internal static T CreatePoolableComponent<T>() where T : class {
            s_unSpecifiedPoolableComponents ??= new Dictionary<Type, IPoolableObject>();
            s_unSpecifiedPoolableComponents.TryGetValue(typeof(T), out var poolableObject);
            
            if (poolableObject != null)
                return ((IPoolableObject<T>)poolableObject).Create();
            
            var obj = new GameObject($"{typeof(T).Name}_IPoolableObject[CreatedByPool]");
            var component = obj.AddComponent(typeof(T));
            obj.SetActive(false);
            
            s_unSpecifiedPoolableComponents.Add(typeof(T), (IPoolableObject)component);
            return ((IPoolableObject<T>)component).Create();
        }

        internal static T CreatePoolableComponent<T>(T prefab) where T : class =>
            ((IPoolableObject<T>)prefab).Create();
    }
}