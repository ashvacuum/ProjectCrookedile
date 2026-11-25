using System.Collections.Generic;
using Crookedile.Utilities;
using NUnit.Framework;
using UnityEngine;

namespace Crookedile.Tests
{
    // A pool must never hand one object to two callers: two hand cards sharing a button left a gap
    // in the fan and stalled the deal.
    public class ObjectPoolTests
    {
        private GameObject _prefab;
        private readonly List<GameObject> _spawned = new List<GameObject>();

        [SetUp]
        public void SetUp()
        {
            _prefab = new GameObject("Pool Prefab");
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject spawned in _spawned)
                Object.DestroyImmediate(spawned);
            _spawned.Clear();
            Object.DestroyImmediate(_prefab);
        }

        [Test]
        public void GetPastTheInitialSizeHandsOutDistinctObjects()
        {
            var pool = new ObjectPool<Transform>(_prefab.transform, initialSize: 1);
            var handedOut = new HashSet<Transform>();
            for (int i = 0; i < 5; i++)
            {
                Transform obj = pool.Get();
                _spawned.Add(obj.gameObject);
                Assert.IsTrue(handedOut.Add(obj), $"Get #{i + 1} handed out an object already in use.");
            }
        }

        [Test]
        public void ReturningTwiceDoesNotHandTheObjectOutTwice()
        {
            var pool = new ObjectPool<Transform>(_prefab.transform, initialSize: 0);
            Transform obj = pool.Get();
            _spawned.Add(obj.gameObject);
            pool.Return(obj);
            pool.Return(obj);

            Transform first = pool.Get();
            Transform second = pool.Get();
            _spawned.Add(second.gameObject);
            Assert.AreSame(obj, first);
            Assert.AreNotSame(first, second);
        }
    }
}
