using System;
using Godot;
using Godot.Collections;

namespace SYNK33.spawner;

public partial class PooledSpawner : Node {
    [Export] long StartingPoolSize = 8;
    [Export] PackedScene Scene;

    private Array<Node> _pool = [];

    public T Spawn<T>() where T: Node {
        if (_pool.Count == 0) {
            _pool.Add(Scene.Instantiate<T>());
        }
        T instance = (T)_pool[_pool.Count - 1];
        _pool.RemoveAt(_pool.Count - 1);
        return instance;
    }
    public void Despawn<T>(T node) where T: Node {
        _pool.Add(node);
    }
}