using System;
using UnityEngine;

namespace ObjectPoolingPlus {
    [RequireComponent(typeof(Rigidbody))]
    public class TestBehaviour : MonoBehaviour {
        void Start() {
            // GlobalPooler.CreatePool<int, TestClass>(1);
            // GlobalPooler.CreatePool<int, TestClass>(2);
            // GlobalPooler.CreatePool<int, TestClass>(3);
            // GlobalPooler.CreatePool<int, TestClass>(4);
            //
            // Debug.Log($"Has Pool 1: {GlobalPooler.HasPool<int, TestClass>(1)}");
            // Debug.Log($"Has Pool 2: {GlobalPooler.HasPool<int, TestClass>(2)}");
            // Debug.Log($"Has Pool 3: {GlobalPooler.HasPool<int, TestClass>(3)}");
            // Debug.Log($"Has Pool 4: {GlobalPooler.HasPool<int, TestClass>(4)}");
            // Debug.Log($"Has Pool 5: {GlobalPooler.HasPool<int, TestClass>(5)}");
            //
            // var obj1 = GlobalPooler.Get<int, TestClass>(1);
            // var obj2 = GlobalPooler.Get<int, TestClass>(2);
            // var obj3 = GlobalPooler.Get<int, TestClass>(3);
            // var obj4 = GlobalPooler.Get<int, TestClass>(4);
            //
            // Debug.Log($"Got objects: {obj1.InstanceId}, {obj2.InstanceId}, {obj3.InstanceId}, {obj4.InstanceId}");
            //
            // GlobalPooler.Release(1, obj1);
            // GlobalPooler.Release(2, obj2);
            // GlobalPooler.Release(3, obj3);
            // GlobalPooler.Release(4, obj4);
            //
            // GlobalPooler.Clear<int, TestClass>();
            //
            // Debug.Log("Cleared all pools for TestClass");
            // Debug.Log($"Has Pool 1: {GlobalPooler.HasPool<int, TestClass>(1)}");
            // Debug.Log($"Has Pool 2: {GlobalPooler.HasPool<int, TestClass>(2)}");
            // Debug.Log($"Has Pool 3: {GlobalPooler.HasPool<int, TestClass>(3)}");
            // Debug.Log($"Has Pool 4: {GlobalPooler.HasPool<int, TestClass>(4)}");
            // Debug.Log($"Has Pool 5: {GlobalPooler.HasPool<int, TestClass>(5)}");
        }

        void Update() { 
            if (Input.GetKeyDown(KeyCode.C)) {
                ClearAllPools();
            }
            if (Input.GetKeyDown(KeyCode.B)) {
                CreateBigPools();
            }
            if (Input.GetKeyDown(KeyCode.T)) {
                var obj = GlobalPooler.Get<int, TestClass>(1);
                Debug.Log("Got TestClass: " + obj.InstanceId);
                GlobalPooler.Release(obj);
            }
        }

        void CreateBigPools() {
            for (int i = 0; i < 1000; i++) {
                GlobalPooler.CreatePool<int, TestClass>(i);
            }
        }
        
        void ClearAllPools() {
            GlobalPooler.Clear();
            Debug.Log("Cleared all pools");
        }
    }
    
    public class TestPoolableObject : IPoolableObject<TestPoolableObject> {
        public TestPoolableObject Create() => new TestPoolableObject();

        public void OnGet() {
            Debug.Log("TestPoolableObject OnGet called");
        }

        public void OnRelease() {
            Debug.Log("TestPoolableObject OnRelease called");
        }

        public void OnDestroy() {
            Debug.Log("TestPoolableObject OnDestroy called");
        }
    }
    
    public class TestClass {
        static int s_instanceCount = 0;
        
        public readonly int InstanceId;
        public TestClass() {
            InstanceId = ++s_instanceCount;
            Debug.Log("Created TestClass instance with ID: " + InstanceId);
        }
    }
}