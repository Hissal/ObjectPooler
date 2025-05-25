namespace ObjectPoolingPlus {
    public interface IPoolableObject<out T> : IPoolableObject {
        T Create();
    }

    public interface IPoolableObject {
        void OnGet();
        void OnRelease();
        void OnDestroy();
    }
}