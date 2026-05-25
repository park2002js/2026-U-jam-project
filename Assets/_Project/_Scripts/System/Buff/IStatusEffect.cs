using UnityEngine;

public interface IStatusEffect<T>
{
    string EffectName { get; }
    bool IsFinished { get; }

    void OnApply(T target);
    void OnTick(T target, float deltaTime);
    void OnRemove(T target);
}