using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;

    private Rigidbody2D rb;
    private AudioSource audioSource;
    private Vector2 movement;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>();
        GameController.Init();
    }

    void Update()
    {
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        // Sem normalizar, a diagonal seria ~1.41x mais rápida
        movement.Normalize();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SceneManager.LoadScene("Menu");
        }
    }

    void FixedUpdate()
    {
        rb.MovePosition(rb.position + movement * speed * Time.fixedDeltaTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Coletavel"))
        {
            Destroy(other.gameObject);
            audioSource.Play();
            GameController.Collect();

            if (GameController.IsGameOver)
            {
                // Espera o som terminar antes de trocar de cena
                Invoke(nameof(EndGame), 0.4f);
            }
        }
    }

    void EndGame()
    {
        SceneManager.LoadScene("EndGame");
    }
}
