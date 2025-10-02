using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MonsterHealth : MonoBehaviour, IExplosionDamageReceiver
{
    [Header("HP")]
    public int maxHP = 200;
    public Slider hpSlider;             // Inspector ����
    public bool destroyOnDeath = true;

    int _hp; // �� ������

    void Awake()
    {
        _hp = Mathf.Max(1, maxHP);

        if (hpSlider)
        {
            // �� Slider �������̶ȹ�����������ʾ 0..maxHP �ġ���ʵ��ֵ��
            hpSlider.wholeNumbers = true;  // ֻ������
            hpSlider.minValue = 0;
            hpSlider.maxValue = maxHP;
            hpSlider.value = _hp;   // ֱ��д��ǰHP������0~1��һ����
        }
    }

    // ���������е� Explosive ����
    public void ApplyDamage(float dmg) => ApplyExplosionDamage(dmg, transform.position, 0f);

    public void ApplyExplosionDamage(float damage, Vector3 center, float radius)
    {
        if (_hp <= 0) return;

        // ͳһ����ȡ���������˺���Ȼ�������� HP ��Ѫ
        int amount = Mathf.Max(0, Mathf.CeilToInt(damage));
        _hp = Mathf.Max(0, _hp - amount);

        if (hpSlider) hpSlider.value = _hp; // ֱ��д������HP

        if (_hp <= 0 && destroyOnDeath)
        {
            Destroy(gameObject);
        }
    }
}
