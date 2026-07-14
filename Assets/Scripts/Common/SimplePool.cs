using System;
using System.Collections.Generic;
using UnityEngine;

namespace CandyCrush.Common
{
    /// <summary>轻量组件对象池：取用时激活，归还时隐藏挂到池根下。</summary>
    public sealed class SimplePool<T> where T : Component
    {
        readonly Stack<T> _stack = new Stack<T>();
        readonly Transform _root;
        readonly Func<T> _factory;

        public int Count => _stack.Count;

        public SimplePool(Transform root, Func<T> factory, int prewarm = 0)
        {
            _root = root;
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            for (int i = 0; i < prewarm; i++)
                Release(_factory());
        }

        public T Rent()
        {
            T item = _stack.Count > 0 ? _stack.Pop() : _factory();
            if (item == null) item = _factory();
            item.gameObject.SetActive(true);
            return item;
        }

        public void Release(T item)
        {
            if (item == null) return;
            item.gameObject.SetActive(false);
            if (_root != null)
                item.transform.SetParent(_root, false);
            _stack.Push(item);
        }

        public void Clear(bool destroyInstances)
        {
            while (_stack.Count > 0)
            {
                var item = _stack.Pop();
                if (item == null) continue;
                if (destroyInstances)
                {
                    if (Application.isPlaying) UnityEngine.Object.Destroy(item.gameObject);
                    else UnityEngine.Object.DestroyImmediate(item.gameObject);
                }
            }
        }
    }
}
