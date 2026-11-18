using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(EnemyHealth))]
public class BossAI : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 1.8f;
    [SerializeField] private float attackInterval = 4.5f;
    [SerializeField] private float attackRadius = 4.5f;
    [SerializeField] private int attackDamage = 20;
    [SerializeField] private float warningDuration = 1.1f;

    private Rigidbody rb;
    private Transform target;
    private float nextAttackTime;
    private bool isAttacking;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        nextAttackTime = Time.time + 2f;
    }

    public void SetTarget(Transform newTarget) => target = newTarget;

    private void FixedUpdate()
    {
        if (target == null || isAttacking)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            return;
        }

        Vector3 direction = target.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f) return;
        direction.Normalize();
        rb.linearVelocity = new Vector3(direction.x * moveSpeed, rb.linearVelocity.y, direction.z * moveSpeed);
        transform.rotation = Quaternion.LookRotation(direction);
    }

    private void Update()
    {
        if (target != null && !isAttacking && Time.time >= nextAttackTime)
        {
            StartCoroutine(GroundSlam());
            nextAttackTime = Time.time + attackInterval;
        }
    }

    private IEnumerator GroundSlam()
    {
        isAttacking = true;
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        Vector3 center = target.position;

        GameObject warning = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        warning.name = "BossAttackWarning";
        warning.transform.position = new Vector3(center.x, 0.08f, center.z);
        warning.transform.localScale = new Vector3(attackRadius * 2f, 0.025f, attackRadius * 2f);
        Collider warningCollider = warning.GetComponent<Collider>();
        if (warningCollider != null) Destroy(warningCollider);
        Renderer warningRenderer = warning.GetComponent<Renderer>();
        if (warningRenderer != null) warningRenderer.material.color = new Color(1f, 0.08f, 0.05f, 0.55f);

        float timer = 0f;
        while (timer < warningDuration)
        {
            timer += Time.deltaTime;
            float pulse = 0.85f + Mathf.Sin(timer * 18f) * 0.1f;
            warning.transform.localScale = new Vector3(attackRadius * 2f * pulse, 0.025f, attackRadius * 2f * pulse);
            yield return null;
        }

        Collider[] hits = Physics.OverlapSphere(center, attackRadius);
        foreach (Collider hit in hits)
        {
            PlayerHealth health = hit.GetComponentInParent<PlayerHealth>();
            if (health != null)
            {
                health.TakeDamage(attackDamage);
                break;
            }
        }

        Destroy(warning);
        isAttacking = false;
    }
}