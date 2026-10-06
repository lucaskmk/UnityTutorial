using UnityEngine;

// Animação simples do galão: pulsa e balança para chamar atenção
public class GasCan : MonoBehaviour
{
    public float pulseSpeed = 4f;
    public float pulseAmount = 0.1f;

    Vector3 baseScale;
    float offset;

    void Start()
    {
        baseScale = transform.localScale;
        offset = Random.value * 10f;
    }

    void Update()
    {
        float t = Time.time * pulseSpeed + offset;
        transform.localScale = baseScale * (1f + Mathf.Sin(t) * pulseAmount);
        transform.rotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 0.5f) * 10f);
    }
}
