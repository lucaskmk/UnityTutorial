using UnityEngine;

public class CameraShake : MonoBehaviour
{
    static CameraShake instance;

    Vector3 basePosition;
    float timer;
    float magnitude;

    void Awake()
    {
        instance = this;
        basePosition = transform.position;
    }

    public static void Shake(float duration, float strength)
    {
        if (instance == null) return;
        instance.timer = duration;
        instance.magnitude = strength;
    }

    void LateUpdate()
    {
        if (timer > 0f)
        {
            timer -= Time.deltaTime;
            transform.position = basePosition + (Vector3)(Random.insideUnitCircle * magnitude);
        }
        else
        {
            transform.position = basePosition;
        }
    }
}
