using UnityEngine;

/// <summary>
/// 指定したターゲットを一定の距離と角度から追いかけるカメラ。
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 12f, -8f);
    [SerializeField] private float followSpeed = 8f;
    [SerializeField] private Vector3 cameraRotation = new Vector3(55f, 0f, 0f);

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 targetPosition = target.position + offset;
        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            followSpeed * Time.deltaTime
        );

        transform.rotation = Quaternion.Euler(cameraRotation);
    }

    /// <summary>
    /// Inspectorを使わずにターゲットを設定したい場合に使用する。
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}
