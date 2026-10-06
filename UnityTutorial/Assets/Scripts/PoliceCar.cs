using System.Collections.Generic;
using UnityEngine;

public enum PoliceMode { Patrol, Chaser }

// Viatura de polícia:
//  - Patrol: anda entre pontos fixos; se vir o jogador por perto, começa a perseguir
//            e volta a patrulhar se perder o jogador de vista.
//  - Chaser: sempre persegue o jogador.
// O caminho pelas ruas é calculado com BFS na grade da cidade (CityMap).
public class PoliceCar : MonoBehaviour
{
    public PoliceMode mode = PoliceMode.Patrol;
    public Vector2[] patrolPoints;

    [Header("Velocidade")]
    public float patrolSpeed = 2.5f;
    public float chaseSpeed = 3.4f;
    public float speedPerGas = 0.08f; // fica mais rápida a cada galão coletado
    public float maxChaseSpeed = 4.6f;

    [Header("Visão")]
    public float sightRange = 4.5f;
    public float loseRange = 7.5f;
    public float loseTime = 3f;

    [Header("Referências")]
    public Transform body;
    public SpriteRenderer glowRed;
    public SpriteRenderer glowBlue;
    public AudioSource siren;

    enum State { Patrol, Chase, Stunned }

    const float Radius = 0.3f;
    const float RepathInterval = 0.2f;

    State state;
    int patrolIndex;
    float repathTimer;
    float stunTimer;
    float lostTimer;
    readonly List<Vector2Int> path = new List<Vector2Int>();
    Rigidbody2D rb;
    Transform player;

    public bool IsChasing => state == State.Chase;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        player = GameObject.FindWithTag("Player").transform;
        state = mode == PoliceMode.Chaser ? State.Chase : State.Patrol;
        if (patrolPoints != null && patrolPoints.Length > 0)
            patrolIndex = ClosestPatrolPoint();
    }

    void FixedUpdate()
    {
        if (!GameController.IsPlaying) return;

        Vector2 pos = rb.position;
        float playerDist = Vector2.Distance(pos, player.position);

        switch (state)
        {
            case State.Stunned:
                stunTimer -= Time.fixedDeltaTime;
                if (stunTimer <= 0f) state = State.Chase;
                return;

            case State.Patrol:
                if (playerDist < sightRange && CityMap.LineClear(pos, player.position, 0.05f))
                {
                    state = State.Chase;
                    lostTimer = 0f;
                }
                break;

            case State.Chase:
                if (mode == PoliceMode.Patrol)
                {
                    lostTimer = playerDist > loseRange ? lostTimer + Time.fixedDeltaTime : 0f;
                    if (lostTimer > loseTime)
                    {
                        state = State.Patrol;
                        patrolIndex = ClosestPatrolPoint();
                    }
                }
                break;
        }

        Vector2 target;
        float speed;
        if (state == State.Chase)
        {
            target = player.position;
            speed = Mathf.Min(maxChaseSpeed, chaseSpeed + speedPerGas * GameController.collected);
        }
        else
        {
            target = patrolPoints[patrolIndex];
            speed = patrolSpeed;
            if (Vector2.Distance(pos, target) < 0.4f)
            {
                patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
                target = patrolPoints[patrolIndex];
            }
        }

        repathTimer -= Time.fixedDeltaTime;
        if (repathTimer <= 0f)
        {
            repathTimer = RepathInterval;
            CityMap.FindPath(CityMap.NearestRoad(pos), CityMap.NearestRoad(target), path);
        }

        Vector2 next = NextWaypoint(pos, target);
        Vector2 dir = next - pos;
        float step = speed * Time.fixedDeltaTime;
        if (dir.magnitude <= step)
        {
            rb.MovePosition(next);
        }
        else
        {
            dir.Normalize();
            rb.MovePosition(pos + dir * step);
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
            body.rotation = Quaternion.Lerp(body.rotation, Quaternion.Euler(0, 0, angle), 10f * Time.fixedDeltaTime);
        }
    }

    // Pega a célula mais distante do caminho que dá para alcançar em linha reta (suaviza as curvas)
    Vector2 NextWaypoint(Vector2 pos, Vector2 target)
    {
        if (path.Count <= 1) return target;
        if (CityMap.LineClear(pos, target, Radius)) return target;

        Vector2 best = CityMap.CellToWorld(path[1]);
        for (int i = 2; i < Mathf.Min(path.Count, 6); i++)
        {
            Vector2 p = CityMap.CellToWorld(path[i]);
            if (CityMap.LineClear(pos, p, Radius)) best = p;
            else break;
        }
        return best;
    }

    int ClosestPatrolPoint()
    {
        int best = 0;
        for (int i = 1; i < patrolPoints.Length; i++)
            if (Vector2.Distance(transform.position, patrolPoints[i]) < Vector2.Distance(transform.position, patrolPoints[best]))
                best = i;
        return best;
    }

    void Update()
    {
        // Giroflex: pisca rápido perseguindo, devagar patrulhando
        bool chasing = state == State.Chase && GameController.IsPlaying;
        float freq = chasing ? 6f : 1.5f;
        bool redOn = Mathf.Repeat(Time.time * freq, 1f) < 0.5f;
        float strength = chasing ? 0.75f : 0.3f;
        glowRed.color = new Color(1f, 0.15f, 0.15f, redOn ? strength : 0.05f);
        glowBlue.color = new Color(0.2f, 0.4f, 1f, redOn ? 0.05f : strength);

        float dist = Vector2.Distance(transform.position, player.position);
        float targetVolume = chasing ? Mathf.Lerp(0.35f, 0.04f, dist / 16f) : 0f;
        siren.volume = Mathf.MoveTowards(siren.volume, targetVolume, Time.deltaTime);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || state == State.Stunned) return;
        var playerMovement = other.GetComponent<PlayerMovement>();
        if (playerMovement.TryHit(transform.position))
        {
            // A viatura fica parada um pouco depois de pegar o jogador, para ele poder fugir
            state = State.Stunned;
            stunTimer = 1.5f;
        }
    }
}
