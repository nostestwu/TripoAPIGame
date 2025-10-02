using UnityEngine;
using UnityEngine.UI;
using System;

[DisallowMultipleComponent]
public class MonsterHealth : MonoBehaviour, IExplosionDamageReceiver
{
    [Header("HP")]
    public int maxHP = 200;
    public Slider hpSlider;
    public bool destroyOnDeath = true;

    public event Action onDeath; // ← 新增：死亡事件

    int _hp;

    void Awake()
    {
        _hp = Mathf.Max(1, maxHP);
        if (hpSlider)
        {
            hpSlider.wholeNumbers = true;
            hpSlider.minValue = 0;
            hpSlider.maxValue = maxHP;
            hpSlider.value = _hp;
        }
    }

    public void ApplyDamage(float dmg) => ApplyExplosionDamage(dmg, transform.position, 0f);

    public void ApplyExplosionDamage(float damage, Vector3 center, float radius)
    {
        if (_hp <= 0) return;
        int amount = Mathf.Max(0, Mathf.CeilToInt(damage));
        _hp = Mathf.Max(0, _hp - amount);
        if (hpSlider) hpSlider.value = _hp;

        if (_hp <= 0)
        {
            onDeath?.Invoke();  // ← 通知 BossController
            if (destroyOnDeath) Destroy(gameObject);
        }
    }
}
