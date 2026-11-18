using UnityEngine;

/// <summary>
/// 回転オーブの接触判定を担当する。
/// </summary>
public class OrbitingOrbHitbox : MonoBehaviour
{
    private OrbitingOrbAttack owner;

    public void Initialize(OrbitingOrbAttack attack)
    {
        owner = attack;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (owner != null) owner.HandleOrbContact(other);
    }

    private void OnTriggerStay(Collider other)
    {
        if (owner != null) owner.HandleOrbContact(other);
    }
}
