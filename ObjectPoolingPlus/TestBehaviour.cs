using System;
using UnityEngine;

namespace ObjectPoolingPlus {
    [RequireComponent(typeof(Rigidbody))]
    public class TestBehaviour : MonoBehaviour {
        void Start() {
            // Example usage of the object pooling system
            
            // Create a pool for Rigidbody objects
            var rbPool = GlobalPooler.CreatePool<Rigidbody>();
            rbPool.OnCreate += obj => Debug.Log($"Created: {obj}");
            rbPool.OnGet += obj => Debug.Log($"Got: {obj}");
            rbPool.OnRelease += obj => Debug.Log($"Released: {obj}");
            rbPool.OnDestroy += obj => Debug.Log($"Destroyed: {obj}");
            // Get a Rigidbody from the pool
            var pooledRb = rbPool.Get();
            Debug.Log($"Pooled Rigidbody: {pooledRb}");
            // Release the Rigidbody back to the pool
            rbPool.Release(pooledRb);
            
            var pooledObject = GlobalPooler.Get<TestPoolableObject>();
            Debug.Log($"Pooled TestPoolableObject: {pooledObject}");
            GlobalPooler.Release(pooledObject);

            var pooledCSharpObject = GlobalPooler.Get<testClass>();
            Debug.Log($"Pooled testClass: {pooledCSharpObject}");
            GlobalPooler.Release(pooledCSharpObject);
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
    
    public class testClass {
        public void Test() {
            Debug.Log("TestClass Test Constructor called");
        }
    }
}