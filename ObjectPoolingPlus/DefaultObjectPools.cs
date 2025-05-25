using System;
using System.Collections.Generic;
using UnityEngine.Pool;

namespace ObjectPoolingPlus {
    internal static class DefaultObjectPools {
        internal static IObjectPoolPlus<T> Create<T>() where T : class => typeof(T) switch {
            _ when typeof(IPoolableObject<T>).IsAssignableFrom(typeof(T)) =>
                (IObjectPoolPlus<T>)typeof(DefaultObjectPools)
                    .GetMethod(nameof(PoolableObjectPool), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                    .MakeGenericMethod(typeof(T))
                    .Invoke(null, new object[] { null }),
            _ when typeof(UnityEngine.Object).IsAssignableFrom(typeof(T)) =>
                (IObjectPoolPlus<T>)typeof(DefaultObjectPools)
                    .GetMethod(nameof(UnityObjectPool), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                    .MakeGenericMethod(typeof(T))
                    .Invoke(null, new object[] { null }),
            _ => AnyObjectPool<T>()
        };
        
        internal static IObjectPoolPlus<T> Create<T>(T prefab) where T : class => typeof(T) switch {
            _ when typeof(IPoolableObject<T>).IsAssignableFrom(typeof(T)) =>
                (IObjectPoolPlus<T>)typeof(DefaultObjectPools)
                    .GetMethod(nameof(PoolableObjectPool), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                    .MakeGenericMethod(typeof(T))
                    .Invoke(null, new object[] { prefab }),
            _ when typeof(UnityEngine.Object).IsAssignableFrom(typeof(T)) =>
                (IObjectPoolPlus<T>)typeof(DefaultObjectPools)
                    .GetMethod(nameof(UnityObjectPool), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                    .MakeGenericMethod(typeof(T))
                    .Invoke(null, new object[] { prefab }),
            _ => AnyObjectPool<T>()
        };
        
        internal static IObjectPoolPlus<T, TKey> Create<T, TKey>() where T : class => typeof(T) switch {
            _ => new KeyedObjectPool<T, TKey>()
        };
        
        internal static IObjectPoolPlus<T> PoolableObjectPool<T>(T prefab) where T : class, IPoolableObject<T>, new() =>
            new PoolableObjectPool<T>(prefab);
        
        internal static IObjectPoolPlus<T> UnityObjectPool<T>(T prefab) where T : UnityEngine.Object =>
            new UnityObjectPool<T>(prefab);
        
        internal static IObjectPoolPlus<T> AnyObjectPool<T>() where T : class =>
            new AnyObjectPool<T>();
    }

    public class AnyObjectPool<T> : IObjectPoolPlus<T> where T : class {
        IObjectPool<T> IObjectPoolPlus<T>.Pool { get; set; }

        public Action<T> OnCreate { get; set; }
        public Action<T> OnGet { get; set; }
        public Action<T> OnRelease { get; set; }
        public Action<T> OnDestroy { get; set; }

        T IObjectPoolPlus<T>.CreateObject() =>
            ObjectCreator.CreateObject<T>();

        void IObjectPoolPlus<T>.OnGetObject(T obj) {
            if (obj is IPoolableObject<T> poolable) {
                poolable.OnGet();
            }
        }

        void IObjectPoolPlus<T>.OnReleaseObject(T obj) {
            if (obj is IPoolableObject<T> poolable) {
                poolable.OnRelease();
            }
        }

        void IObjectPoolPlus<T>.OnDestroyObject(T obj) {
            switch (obj) {
                case IDisposable disposable:
                    disposable.Dispose();
                    break;
                case IPoolableObject<T> poolable:
                    poolable.OnDestroy();
                    break;
                case UnityEngine.Object unityObject:
                    UnityEngine.Object.Destroy(unityObject);
                    break;
            }
        }
    }
    
    public class UnityObjectPool<T> : IObjectPoolPlus<T> where T : UnityEngine.Object {
        protected T Prefab { get; set; }
        
        public UnityObjectPool(T prefab) {
            Prefab = prefab;
        }
        
        IObjectPool<T> IObjectPoolPlus<T>.Pool { get; set; }

        public Action<T> OnCreate { get; set; }
        public Action<T> OnGet { get; set; }
        public Action<T> OnRelease { get; set; }
        public Action<T> OnDestroy { get; set; }

        T IObjectPoolPlus<T>.CreateObject() {
            var obj = Prefab != null
                ? ObjectCreator.CreateObject(Prefab)
                : ObjectCreator.CreateObject<T>();
            
            Prefab ??= obj;
            return obj;
        }

        void IObjectPoolPlus<T>.OnGetObject(T obj) {
            switch (obj) {
                case UnityEngine.Component component:
                    component.gameObject.SetActive(true);
                    break;
                case UnityEngine.GameObject gameObject:
                    gameObject.SetActive(true);
                    break;
            }
            
            if (obj is IPoolableObject<T> poolable)
                poolable.OnGet();
        }

        void IObjectPoolPlus<T>.OnReleaseObject(T obj) {
            switch (obj) {
                case UnityEngine.Component component:
                    component.gameObject.SetActive(false);
                    break;
                case UnityEngine.GameObject gameObject:
                    gameObject.SetActive(false);
                    break;
            }
            
            if (obj is IPoolableObject<T> poolable)
                poolable.OnRelease();
        }

        void IObjectPoolPlus<T>.OnDestroyObject(T obj) {
            switch (obj) {
                case IPoolableObject<T> poolable:
                    poolable.OnDestroy();
                    break;
                case IDisposable disposable:
                    disposable.Dispose();
                    break;
                default:
                    UnityEngine.Object.Destroy(obj);
                    break;
            }
        }
    }
    
    public class PoolableObjectPool<T> : IObjectPoolPlus<T> where T : class, IPoolableObject<T>, new() {
        protected T Prefab { get; set; }
        public PoolableObjectPool(T prefab) {
            Prefab = prefab;
        }
        
        IObjectPool<T> IObjectPoolPlus<T>.Pool { get; set; }

        public Action<T> OnCreate { get; set; }
        public Action<T> OnGet { get; set; }
        public Action<T> OnRelease { get; set; }
        public Action<T> OnDestroy { get; set; }

        T IObjectPoolPlus<T>.CreateObject() {
            var obj = Prefab != null
                ? ObjectCreator.CreatePoolableObject(Prefab)
                : ObjectCreator.CreatePoolableObject<T>();

            Prefab ??= obj;
            return obj;
        }

        void IObjectPoolPlus<T>.OnGetObject(T obj) {
            obj.OnGet();
        }

        void IObjectPoolPlus<T>.OnReleaseObject(T obj) {
            obj.OnRelease();
        }

        void IObjectPoolPlus<T>.OnDestroyObject(T obj) {
            obj.OnDestroy();
        }
    }
    
    public class KeyedObjectPool<T, TKey> : IObjectPoolPlus<T, TKey> where T : class {
        Dictionary<TKey, IObjectPoolPlus<T>> IObjectPoolPlus<T, TKey>.Pools { get; set; }

        public Action<T> OnCreate { get; set; }
        public Action<T> OnGet { get; set; }
        public Action<T> OnRelease { get; set; }
        public Action<T> OnDestroy { get; set; }

        void IObjectPoolPlus<T, TKey>.OnCreateObject(TKey key, T obj) { }
        void IObjectPoolPlus<T, TKey>.OnGetObject(TKey key, T obj) { }
        void IObjectPoolPlus<T, TKey>.OnReleaseObject(TKey key, T obj) { }
        void IObjectPoolPlus<T, TKey>.OnDestroyObject(TKey key, T obj) { }
    }
}