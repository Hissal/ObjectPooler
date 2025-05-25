using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ObjectPoolingPlus {
    public class ObjectPooler : IDisposable {
        Transform pooledObjectsParent;

        readonly Dictionary<Type, IObjectPoolPlus> registeredPools = new();
        readonly Dictionary<Type, Dictionary<Type, IObjectPoolPlus>> registeredKeyedPools = new();
        
        public ObjectPooler() : this(new GameObject("Pooled Objects").transform) { }
        public ObjectPooler(Transform pooledObjectsParent) {
            this.pooledObjectsParent = pooledObjectsParent;
        }
        
        public IObjectPoolPlus<T> RegisterPool<T>(IObjectPoolPlus<T> pool) where T : class {
            IObjectPoolPlus<T>.RegisterPool(pool, this);
            
            if (registeredPools.ContainsKey(typeof(T))) {
                Debug.LogWarning($"Pool of type {typeof(T).Name} is already registered. Overwriting and clearing the existing pool.");
                registeredPools[typeof(T)].Clear(this);
                registeredPools[typeof(T)] = pool;
                return pool;
            }
            
            registeredPools.Add(typeof(T), pool);
            return pool;
        }

        public IObjectPoolPlus<T> CreatePool<T>(IObjectPoolPlus<T> pool = null) where T : class => 
            RegisterPool((pool ?? DefaultObjectPools.Create<T>()).CreatePool());
        
        public IObjectPoolPlus<T> GetPool<T>() where T : class =>
            IObjectPoolPlus<T>.GetFor(this) ?? CreatePool<T>();

        public T Get<T>() where T : class =>
            GetPool<T>().Get();

        public void Release<T>(T obj) where T : class =>
            GetPool<T>().Release(obj);
        
        public IObjectPoolPlus<TKey, T> RegisterPool<TKey, T>(IObjectPoolPlus<TKey, T> pool) where T : class {
            IObjectPoolPlus<TKey, T>.RegisterPool(pool, this);
            
            var poolDictionary = registeredKeyedPools.GetValueOrDefault(typeof(T));
            
            if (poolDictionary == null) {
                poolDictionary = new Dictionary<Type, IObjectPoolPlus>();
                registeredKeyedPools.Add(typeof(T), poolDictionary);
            }

            if (poolDictionary.ContainsKey(typeof(TKey))) {
                Debug.LogWarning($"Pool of type {typeof(T).Name} with key of type {typeof(TKey).Name} is already registered. Overwriting and clearing the existing pool.");
                poolDictionary[typeof(TKey)].Clear(this);
                poolDictionary[typeof(TKey)] = pool;
                return pool;
            }
            
            poolDictionary.Add(typeof(TKey), pool);
            return pool;
        }
        
        public IObjectPoolPlus<TKey, T> CreatePool<TKey, T>(IObjectPoolPlus<TKey, T> pool = null) where T : class => 
            RegisterPool(pool ?? DefaultObjectPools.Create<TKey, T>());

        public IObjectPoolPlus<T> CreatePool<TKey, T>(TKey key, IObjectPoolPlus<T> pool = null) where T : class => 
            GetPool<TKey, T>().CreatePool(key, pool ?? DefaultObjectPools.Create<T>());

        public IObjectPoolPlus<TKey, T> GetPool<TKey, T>() where T : class =>
            IObjectPoolPlus<TKey, T>.GetFor(this) ?? CreatePool<TKey, T>();
        public IObjectPoolPlus<T> GetPool<TKey, T>(TKey key) where T : class =>
            IObjectPoolPlus<TKey, T>.GetFor(this, key) ?? CreatePool<TKey, T>(key);
        
        public T Get<TKey, T>(TKey key) where T : class =>
            GetPool<TKey, T>(key).Get();
        public void Release<TKey, T>(TKey key, T obj) where T : class =>
            GetPool<TKey, T>(key).Release(obj);
        
        public bool HasPool<T>() where T : class => 
            registeredPools.ContainsKey(typeof(T));
        public bool HasPool<TKey, T>() where T : class => 
            registeredKeyedPools.TryGetValue(typeof(T), out var poolDictionary) && poolDictionary.ContainsKey(typeof(TKey));
        public bool HasPool<TKey, T>(TKey key) where T : class =>
            registeredKeyedPools.TryGetValue(typeof(T), out var poolDictionary) && 
            poolDictionary.TryGetValue(typeof(TKey), out var pool) && 
            pool is IObjectPoolPlus<TKey, T> keyedPool && keyedPool.HasKey(key);

        public void Clear<T>() where T : class {
            IObjectPoolPlus<T>.ClearFor(this);
            registeredPools.Remove(typeof(T));
        }

        public void Clear<TKey, T>() where T : class {
            IObjectPoolPlus<TKey, T>.ClearFor(this);
            
            if (!registeredKeyedPools.TryGetValue(typeof(T), out var poolDictionary))
                return;
            
            poolDictionary.Remove(typeof(TKey));
            if (poolDictionary.Count == 0)
                registeredKeyedPools.Remove(typeof(T));
        }

        public void Clear<TKey, T>(TKey key) where T : class {
            IObjectPoolPlus<TKey, T>.ClearFor(this, key);
            
            if (!registeredKeyedPools.TryGetValue(typeof(T), out var poolDictionary) || !poolDictionary.TryGetValue(typeof(TKey), out var pool))
                return;
            
            if (pool is IObjectPoolPlus<TKey, T> keyedPool && keyedPool.Pools.Count == 0)
                keyedPool.Pools.Remove(key);
        }
        
        public void Clear() {
            foreach (var pool in registeredPools.Values) {
                pool.Clear(this);
            }
            
            registeredPools.Clear();
            
            foreach (var keyedPool in registeredKeyedPools.Values) {
                foreach (var pool in keyedPool.Values) {
                    pool.Clear(this);
                }
                keyedPool.Clear();
            }
            registeredKeyedPools.Clear();
        }

        ~ObjectPooler() {
            Dispose();
        }
        public void Dispose() {
            Clear();
        }
    }
    
    public static class GlobalPooler {
        static ObjectPooler s_instance;

        static ObjectPooler Instance {
            get {
                if (s_instance == null)
                    Configure(new GameObject("Global Object Pooler").transform);
                
                return s_instance;
            }
            
            set => s_instance = value;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void CleanUp() {
            if (s_instance == null)
                return;
            
            s_instance.Dispose();
            s_instance = null;
        }

        public static void Configure(Transform pooledObjectsParent) {
            if (s_instance != null)
                Debug.LogWarning("Global Object Pooler is already configured. Reconfiguring will reset all pools.");
            
            s_instance?.Clear();
            s_instance = new ObjectPooler(pooledObjectsParent);
            Object.DontDestroyOnLoad(pooledObjectsParent.gameObject);
        }
        
        public static T Get<T>(T prefab) where T : class =>
            Instance.Get<T, T>(prefab);
        public static void Release<T>(T prefab, T obj) where T : class =>
            Instance.Release<T, T>(prefab, obj);

        public static IObjectPoolPlus<T> CreatePool<T>(IObjectPoolPlus<T> pool = null) where T : class =>
            Instance.CreatePool(pool);
        
        public static IObjectPoolPlus<T> GetPool<T>() where T : class =>
            Instance.GetPool<T>();

        public static T Get<T>() where T : class =>
            Instance.Get<T>();

        public static void Release<T>(T obj) where T : class =>
            Instance.Release(obj);
        
        public static IObjectPoolPlus<TKey, T> CreatePool<TKey, T>(IObjectPoolPlus<TKey, T> pool = null) where T : class =>
            Instance.CreatePool(pool);
        
        public static IObjectPoolPlus<T> CreatePool<TKey, T>(TKey key, IObjectPoolPlus<T> pool = null) where T : class =>
            Instance.CreatePool(key, pool);
        
        public static IObjectPoolPlus<T> GetPool<TKey, T>(TKey key) where T : class =>
            Instance.GetPool<TKey, T>(key);
        
        public static T Get<TKey, T>(TKey key) where T : class =>
            Instance.Get<TKey, T>(key);
        
        public static void Release<TKey, T>(TKey key, T obj) where T : class =>
            Instance.Release(key, obj);

        public static bool HasPool<T>() where T : class =>
            Instance.HasPool<T>();
        public static bool HasPool<TKey, T>() where T : class =>
            Instance.HasPool<TKey, T>();
        public static bool HasPool<TKey, T>(TKey key) where T : class =>
            Instance.HasPool<TKey, T>(key);
        
        public static void Clear<T>() where T : class =>
            Instance.Clear<T>();
        public static void Clear<TKey, T>() where T : class =>
            Instance.Clear<TKey, T>();
        public static void Clear<TKey, T>(TKey key) where T : class =>
            Instance.Clear<TKey, T>(key);
        
        public static void Clear() {
            Instance.Clear();
        }
    }
}