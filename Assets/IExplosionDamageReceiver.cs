using UnityEngine;

public interface IExplosionDamageReceiver
{
    /// <summary>
    /// ����һ�α�ը�˺���damage �Ѿ��������˥�����ˡ�
    /// center/radius ֻ���ṩ�����ģ���������Ч�����˵ȣ���
    /// </summary>
    void ApplyExplosionDamage(float damage, Vector3 center, float radius);
}
