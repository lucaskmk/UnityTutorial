using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;
    public float acceleration = 30f;

    [Header("Óleo")]
    public float oilSpeedMultiplier = 0.6f;
    public float oilAcceleration = 3f;

    [Header("Dano")]
    public float invulnerableTime = 2f;

    [Header("Referências")]
    public Transform body;
    public SpriteRenderer bodyRenderer;
    public AudioSource engineSource;
    public AudioClip crashClip;

    private Rigidbody2D rb;
    private AudioSource audioSource;
    private InputAction moveAction;
    private Vector2 movement;
    private int oilContacts;
    private float invulnerableTimer;

    public bool IsMoving => rb.linearVelocity.sqrMagnitude > 0.5f;
    public bool IsInvulnerable => invulnerableTimer > 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>();
        // Move já tem WASD, setas, analógico e direcional do controle (InputSystem_Actions)
        moveAction = InputSystem.actions.FindAction("Player/Move");
        moveAction.Enable();
    }

    void Update()
    {
        movement = GameController.IsPlaying && Time.timeScale > 0f ? moveAction.ReadValue<Vector2>() : Vector2.zero;

        // Sem normalizar, a diagonal seria ~1.41x mais rápida
        if (movement.sqrMagnitude > 1f) movement.Normalize();

        // O carro aponta para onde está andando (o sprite olha para cima)
        Vector2 velocity = rb.linearVelocity;
        if (velocity.sqrMagnitude > 0.2f)
        {
            float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg - 90f;
            body.rotation = Quaternion.Lerp(body.rotation, Quaternion.Euler(0, 0, angle), 15f * Time.deltaTime);
        }

        if (engineSource != null)
        {
            float t = velocity.magnitude / speed;
            engineSource.pitch = Mathf.Lerp(0.8f, 1.6f, t);
            engineSource.volume = Mathf.Lerp(0.12f, 0.3f, t);
        }

        if (invulnerableTimer > 0f)
        {
            invulnerableTimer -= Time.deltaTime;
            bodyRenderer.enabled = invulnerableTimer <= 0f || Mathf.Repeat(invulnerableTimer, 0.2f) > 0.1f;
        }
    }

    void FixedUpdate()
    {
        bool onOil = oilContacts > 0;
        float maxSpeed = speed * (onOil ? oilSpeedMultiplier : 1f);
        float accel = onOil ? oilAcceleration : acceleration;
        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, movement * maxSpeed, accel * Time.fixedDeltaTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Coletavel"))
        {
            if (!GameController.IsPlaying) return;
            Destroy(other.gameObject);
            audioSource.Play();
            GameController.Collect();
            LevelManager.Instance.OnGasCollected();
        }
        else if (other.CompareTag("Oleo"))
        {
            oilContacts++;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Oleo"))
            oilContacts = Mathf.Max(0, oilContacts - 1);
    }

    // Chamado pela polícia ao encostar no jogador
    public bool TryHit(Vector2 from)
    {
        if (IsInvulnerable || !GameController.IsPlaying) return false;

        invulnerableTimer = invulnerableTime;
        GameController.lives--;
        audioSource.PlayOneShot(crashClip);
        CameraShake.Shake(0.3f, 0.25f);

        // Empurrão para longe da viatura
        Vector2 push = ((Vector2)transform.position - from).normalized;
        rb.linearVelocity = push * speed * 1.2f;
        return true;
    }
}
