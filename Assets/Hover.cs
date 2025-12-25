using UnityEngine;

public class Hover : MonoBehaviour
{
    [SerializeField] float hoverAmplitude = 0.1f;     // peak offset
    [SerializeField] float hoverSpeed = 2.0f;         // oscillation rate

    Vector3 baseLocalPos;

    void Awake()
    {
        baseLocalPos = transform.localPosition;
    }

    void LateUpdate()
    {
        if (hoverAmplitude <= 0f || hoverSpeed <= 0f) return;

        float yOffset = Mathf.Sin(Time.time * hoverSpeed) * hoverAmplitude;

        transform.localPosition = new Vector3(
            baseLocalPos.x,
            baseLocalPos.y + yOffset,
            baseLocalPos.z
        );
    }
}
