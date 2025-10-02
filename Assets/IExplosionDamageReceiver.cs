using UnityEngine;

public interface IExplosionDamageReceiver
{
    /// <summary>
    /// 接受一次爆炸伤害。damage 已经按距离等衰减好了。
    /// center/radius 只是提供上下文（比如做特效、击退等）。
    /// </summary>
    void ApplyExplosionDamage(float damage, Vector3 center, float radius);
}
