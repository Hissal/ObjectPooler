using UnityEngine;

namespace ObjectPoolingPlus {
    public class ObjectPooler {
        Transform pooledObjectsParent;

        public ObjectPooler() : this(new GameObject("Pooled Objects").transform) { }
        public ObjectPooler(Transform pooledObjectsParent) {
            this.pooledObjectsParent = pooledObjectsParent;
        }

        public IObjectPoolPlus<T> CreatePool<T>(IObjectPoolPlus<T> pool = null) where T : class {
            pool ??= DefaultObjectPools.Create<T>();
            
            if (pool.Pool == null)
                pool.CreatePool();
            
            IObjectPoolPlus<T>.RegisterPool(this, pool);
            return pool;
        }
        public IObjectPoolPlus<T> GetPool<T>() where T : class =>
            IObjectPoolPlus<T>.GetFor(this) ?? CreatePool<T>();

        public T Get<T>() where T : class =>
            GetPool<T>().Get();

        public void Release<T>(T obj) where T : class =>
            GetPool<T>().Release(obj);
        
        public IObjectPoolPlus<T, TKey> CreatePool<T, TKey>(IObjectPoolPlus<T, TKey> pool) where T : class =>
            IObjectPoolPlus<T, TKey>.RegisterPool(this, pool);

        public IObjectPoolPlus<T> CreatePool<T, TKey>(TKey key, IObjectPoolPlus<T> pool = null) where T : class {
            var keyedPool = IObjectPoolPlus<T, TKey>.GetFor(this) ?? DefaultObjectPools.Create<T, TKey>();
            
            if (pool?.Pool == null) {
                pool ??= DefaultObjectPools.Create<T>();
                keyedPool.CreatePool(key, pool);
            }
            
            IObjectPoolPlus<T, TKey>.RegisterPool(this, keyedPool);
            return pool;
        }
        public IObjectPoolPlus<T> GetPool<T, TKey>(TKey key) where T : class =>
            IObjectPoolPlus<T, TKey>.GetFor(this, key) ?? CreatePool<T, TKey>(key);
        
        public T Get<T, TKey>(TKey key) where T : class =>
            GetPool<T, TKey>(key).Get();
        public void Release<T, TKey>(TKey key, T obj) where T : class =>
            GetPool<T, TKey>(key).Release(obj);
        
        public void Clear() {
            
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

        public static void Configure(Transform pooledObjectsParent) {
            if (s_instance != null)
                Debug.LogWarning("Global Object Pooler is already configured. Reconfiguring will reset all pools.");
            
            s_instance?.Clear();
            s_instance = new ObjectPooler(pooledObjectsParent);
            Object.DontDestroyOnLoad(pooledObjectsParent.gameObject);
        }
        
        public static IObjectPoolPlus<T> CreatePool<T>(IObjectPoolPlus<T> pool = null) where T : class =>
            Instance.CreatePool(pool);
        
        public static IObjectPoolPlus<T> GetPool<T>() where T : class =>
            Instance.GetPool<T>();

        public static T Get<T>() where T : class =>
            Instance.Get<T>();

        public static void Release<T>(T obj) where T : class =>
            Instance.Release(obj);
        
        public static IObjectPoolPlus<T, TKey> CreatePool<T, TKey>(IObjectPoolPlus<T, TKey> pool) where T : class =>
            Instance.CreatePool(pool);
        
        public static IObjectPoolPlus<T> CreatePool<T, TKey>(TKey key, IObjectPoolPlus<T> pool = null) where T : class =>
            Instance.CreatePool(key, pool);
        
        public static IObjectPoolPlus<T> GetPool<T, TKey>(TKey key) where T : class =>
            Instance.GetPool<T, TKey>(key);
        
        public static T Get<T, TKey>(TKey key) where T : class =>
            Instance.Get<T, TKey>(key);
        
        public static void Release<T, TKey>(TKey key, T obj) where T : class =>
            Instance.Release<T, TKey>(key, obj);
        
        public static void Clear() {
            Instance.Clear();
        }
    }
}