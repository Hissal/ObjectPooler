using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace ObjectPoolingPlus {
    public interface IObjectPoolPlus {
        void Clear(ObjectPooler pooler);
    }
    
    public interface IObjectPoolPlus<T> : IObjectPoolPlus where T : class {
        private static Dictionary<ObjectPooler, IObjectPoolPlus<T>> s_registeredPools;
        private static Dictionary<ObjectPooler, IObjectPoolPlus<T>> RegisteredPools => s_registeredPools ??= new Dictionary<ObjectPooler, IObjectPoolPlus<T>>();
        
        internal static IObjectPoolPlus<T> RegisterPool(IObjectPoolPlus<T> pool, ObjectPooler pooler) {
            RegisteredPools[pooler] = pool;
            return pool;
        }
        internal static IObjectPoolPlus<T> GetFor(ObjectPooler pooler) {
            RegisteredPools.TryGetValue(pooler, out var pool);
            return pool;
        }
        internal static void ClearFor(ObjectPooler pooler) {
            if (s_registeredPools == null || !s_registeredPools.TryGetValue(pooler, out var pool))
                return;
            
            pool.Clear();
            s_registeredPools.Remove(pooler);
        }

        void IObjectPoolPlus.Clear(ObjectPooler pooler) =>
            ClearFor(pooler);
        
        IObjectPool<T> Pool { get; protected set; }
        
        Action<T> OnCreate { get; set; }
        Action<T> OnGet { get; set; }
        Action<T> OnRelease { get; set; }
        Action<T> OnDestroy { get; set; }
        
        public IObjectPool<T> CreatePool() {
            Pool = new ObjectPool<T>(
                createFunc: () => {
                    var obj = CreateObject();
                    OnCreate?.Invoke(obj);
                    return obj;
                },
                actionOnGet: obj => {
                    OnGetObject(obj);
                    OnGet?.Invoke(obj);
                },
                actionOnRelease: obj => {
                    OnReleaseObject(obj);
                    OnRelease?.Invoke(obj);
                },
                actionOnDestroy: obj => {
                    OnDestroyObject(obj);
                    OnDestroy?.Invoke(obj);
                }
            );
            
            return Pool;
        }

        protected T CreateObject();
        protected void OnGetObject(T obj);
        protected void OnReleaseObject(T obj);
        protected void OnDestroyObject(T obj);
        
        T Get() => Pool.Get();
        PooledObject<T> Get(out T v) => Pool.Get(out v);
        void Release(T obj) => Pool.Release(obj);
        void Clear() => Pool.Clear();
    }
    
    public interface IObjectPoolPlus<TKey, T> : IObjectPoolPlus where T : class {
        private static Dictionary<ObjectPooler, IObjectPoolPlus<TKey, T>> s_registeredPools;
        private static Dictionary<ObjectPooler, IObjectPoolPlus<TKey, T>> RegisteredPools => s_registeredPools ??= new Dictionary<ObjectPooler, IObjectPoolPlus<TKey, T>>();

        internal static IObjectPoolPlus<TKey, T> RegisterPool(IObjectPoolPlus<TKey, T> pool, ObjectPooler pooler) {
            RegisteredPools[pooler] = pool;
            return pool;
        }
        internal static IObjectPoolPlus<TKey, T> GetFor(ObjectPooler pooler) {
            RegisteredPools.TryGetValue(pooler, out var pool);
            return pool;
        }

        internal static IObjectPoolPlus<T> GetFor(ObjectPooler pooler, TKey key) {
            RegisteredPools.TryGetValue(pooler, out var pool);
            return pool?.GetPool(key);
        }

        internal static void ClearFor(ObjectPooler pooler) {
            if (s_registeredPools == null || !s_registeredPools.TryGetValue(pooler, out var pool))
                return;
            
            pool.Clear();
            s_registeredPools.Remove(pooler);
        }
        internal static void ClearFor(ObjectPooler pooler, TKey key) {
            if (s_registeredPools == null || !s_registeredPools.TryGetValue(pooler, out var pool) || !pool.HasKey(key))
                return;
            
            pool.Clear(key);
            
            if (pool.Pools.Count == 0)
                s_registeredPools.Remove(pooler);
        }
        
        void IObjectPoolPlus.Clear(ObjectPooler pooler) =>
            ClearFor(pooler);

        Dictionary<TKey, IObjectPoolPlus<T>> Pools { get; set; }
        
        Action<T> OnCreate { get; set; }
        Action<T> OnGet { get; set; }
        Action<T> OnRelease { get; set; }
        Action<T> OnDestroy { get; set; }
        
        IObjectPool<T> CreatePool(TKey key, IObjectPoolPlus<T> pool) {
            Pools ??= new Dictionary<TKey, IObjectPoolPlus<T>>();
            
            if (Pools.TryGetValue(key, out var existingPool)) {
                Debug.LogWarning($"Object pool already exists for key: {key} - Clearing and replacing it.");
                existingPool.Clear();
            }

            pool.CreatePool();
            
            pool.OnCreate += obj => {
                OnCreateObject(key, obj);
                OnCreate?.Invoke(obj);
            };
            pool.OnGet += obj => {
                OnGetObject(key, obj);
                OnGet?.Invoke(obj);
            };
            pool.OnRelease += obj => {
                OnReleaseObject(key, obj);
                OnRelease?.Invoke(obj);
            };
            pool.OnDestroy += obj => {
                OnDestroyObject(key, obj);
                OnDestroy?.Invoke(obj);
            };
            
            Pools[key] = pool;
            return pool.Pool;
        }
        
        protected void OnCreateObject(TKey key, T obj);
        protected void OnGetObject(TKey key, T obj);
        protected void OnReleaseObject(TKey key, T obj);
        protected void OnDestroyObject(TKey key, T obj);
        
        T Get(TKey key) => Pools[key].Get();
        PooledObject<T> Get(TKey key, out T v) => Pools[key].Get(out v);
        void Release(TKey key, T obj) => Pools[key].Release(obj);
        void Clear(TKey key) {
            Pools[key].Clear();
            Pools.Remove(key);
        }

        void Clear() {
            if (Pools == null)
                return;
            
            foreach (var pool in Pools.Values)
                pool.Clear();
            
            Pools.Clear();
        }
        
        bool HasKey(TKey key) {
            return Pools != null && Pools.ContainsKey(key);
        }
        IObjectPoolPlus<T> GetPool(TKey key) {
            if (Pools == null || !Pools.TryGetValue(key, out var pool))
                throw new KeyNotFoundException($"No pool found for key: {key}");
            return pool;
        }
    }
}