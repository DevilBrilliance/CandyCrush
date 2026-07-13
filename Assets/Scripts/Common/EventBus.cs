using System;
using System.Collections.Generic;

namespace CandyCrush.Common
{
    /// <summary>轻量事件总线。表现层订阅，行为层/流程层发布。</summary>
    public static class EventBus
    {
        static readonly Dictionary<Type, List<Delegate>> _map = new Dictionary<Type, List<Delegate>>();

        public static void Subscribe<T>(Action<T> handler)
        {
            var t = typeof(T);
            if (!_map.TryGetValue(t, out var list))
            {
                list = new List<Delegate>();
                _map[t] = list;
            }
            list.Add(handler);
        }

        public static void Unsubscribe<T>(Action<T> handler)
        {
            if (_map.TryGetValue(typeof(T), out var list))
                list.Remove(handler);
        }

        public static void Publish<T>(T evt)
        {
            if (!_map.TryGetValue(typeof(T), out var list)) return;
            // 拷贝防订阅中途修改
            var copy = list.ToArray();
            for (int i = 0; i < copy.Length; i++)
                ((Action<T>)copy[i])?.Invoke(evt);
        }

        public static void Clear() => _map.Clear();
    }

    public readonly struct LevelWinEvent
    {
        public readonly int Remaining;
        public LevelWinEvent(int remaining) { Remaining = remaining; }
    }

    public readonly struct ObjectiveChangedEvent
    {
        public readonly int Remaining;
        public ObjectiveChangedEvent(int remaining) { Remaining = remaining; }
    }
}
