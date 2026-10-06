using UnityEngine;

// Carro decorativo do menu: anda em linha reta e volta do outro lado da tela
public class MenuCarLoop : MonoBehaviour
{
    public float speed = 4f;
    public float minX = -13f;
    public float maxX = 13f;

    void Update()
    {
        transform.position += Vector3.right * speed * Time.deltaTime;
        if (transform.position.x > maxX)
            transform.position = new Vector3(minX, transform.position.y, transform.position.z);
    }
}
