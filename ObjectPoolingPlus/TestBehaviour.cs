using System;
using UnityEngine;

namespace ObjectPoolingPlus {
    [RequireComponent(typeof(Rigidbody))]
    public class TestBehaviour : MonoBehaviour {
        void Start() {
            // Example usage of the object pooling system
            var pooler = new ObjectPooler(transform);

            var rb = GetComponent<Rigidbody>();
            
            // Create a pool for Rigidbody objects
            var rbPool = pooler.CreatePool<Rigidbody>();
            rbPool.OnCreate += obj => Debug.Log($"Created: {obj}");
            rbPool.OnGet += obj => Debug.Log($"Got: {obj}");
            rbPool.OnRelease += obj => Debug.Log($"Released: {obj}");
            rbPool.OnDestroy += obj => Debug.Log($"Destroyed: {obj}");
            // Get a Rigidbody from the pool
            var pooledRb = rbPool.Get();
            Debug.Log($"Pooled Rigidbody: {pooledRb}");
            // Release the Rigidbody back to the pool
            rbPool.Release(pooledRb);
        }
    }
}