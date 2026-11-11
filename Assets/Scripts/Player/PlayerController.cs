using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// WASD / 矢印キーでプレイヤーを移動させる。
/// Rigidbodyを使用し、物理演算と移動処理を分離する。
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 6f;

    private Rigidbody rb;
    private Vector2 moveInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        moveInput = Vector2.zero;

        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
        {
            moveInput.y += 1f;
        }

        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
        {
            moveInput.y -= 1f;
        }

        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
        {
            moveInput.x += 1f;
        }

        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
        {
            moveInput.x -= 1f;
        }

        moveInput = Vector2.ClampMagnitude(moveInput, 1f);
    }

    private void FixedUpdate()
    {
        Vector3 direction = new Vector3(moveInput.x, 0f, moveInput.y);
        Vector3 velocity = direction * moveSpeed;

        // Y方向の速度は重力などの物理演算に任せる。
        rb.linearVelocity = new Vector3(velocity.x, rb.linearVelocity.y, velocity.z);

        // 移動方向へプレイヤーの正面を向ける。
        if (direction.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }
}
