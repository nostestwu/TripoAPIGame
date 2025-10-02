using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MonsterHealth : MonoBehaviour, IExplosionDamageReceiver
{
    [Header("HP")]
    public int maxHP = 200;
    public Slider hpSlider;             // Inspector 绑上
    public bool destroyOnDeath = true;

    int _hp; // ← 用整型

    void Awake()
    {
        _hp = Mathf.Max(1, maxHP);

        if (hpSlider)
        {
            // 让 Slider 按整数刻度工作，并且显示 0..maxHP 的“真实数值”
            hpSlider.wholeNumbers = true;  // 只走整数
            hpSlider.minValue = 0;
            hpSlider.maxValue = maxHP;
            hpSlider.value = _hp;   // 直接写当前HP（不是0~1归一化）
        }
    }

    // 兼容你现有的 Explosive 调用
    public void ApplyDamage(float dmg) => ApplyExplosionDamage(dmg, transform.position, 0f);

    public void ApplyExplosionDamage(float damage, Vector3 center, float radius)
    {
        if (_hp <= 0) return;

        // 统一向上取整到整数伤害，然后用整型 HP 扣血
        int amount = Mathf.Max(0, Mathf.CeilToInt(damage));
        _hp = Mathf.Max(0, _hp - amount);

        if (hpSlider) hpSlider.value = _hp; // 直接写入整数HP

        if (_hp <= 0 && destroyOnDeath)
        {
            Destroy(gameObject);
        }
    }
}
