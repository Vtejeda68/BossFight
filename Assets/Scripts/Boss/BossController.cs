using UnityEngine;
using UnityEngine.InputSystem;

public class BossController : MonoBehaviour
{
    [Header("Referencias")]
    public Transform player;
    public Rigidbody2D rb;
    public Animator animator;
    public SpriteRenderer sprite;
    public BossHealth health;

    [Header("Movimiento (solo fase 1)")]
    public float walkSpeed = 3f;
    public float meleeRange = 3.5f;   // Distancia a la que el boss deja de caminar y ataca

    [Header("Ritmo (espera entre acciones)")]
    public float idleTimePhase1 = 1.1f;
    public float idleTimePhase2 = 0.9f;
    public float idleTimePhase3 = 0.7f;

    [Header("Fase 3 (injusta a propósito)")]
    [Range(0f, 1f)] public float phase3ChainChance = 0.6f;   // Probabilidad de encadenar otra acción sin pausa
    public float phase3TempoMultiplier = 0.75f;              // Avisos y recuperaciones al 75% del tiempo

    [Header("Dash vacío (fase 1)")]
    public float dashMinDistance = 5f;     // Solo dashea si el jugador está al menos a esta distancia
    [Range(0f, 1f)] public float dashChance = 0.6f;

    [Header("Jugador acorralado")]
    public float cornerDistance = 2.5f;    // El jugador está "en la pared" si está a menos de esto de un límite
    public float cornerBossDistance = 3.5f;// ...y el boss a menos de esto de él
    public float cornerTime = 0.7f;        // Segundos seguidos acorralado antes de reaccionar
    public float escapeDistance = 9f;      // Distancia a la que se teletransporta para huir (fase 2+)

    [Header("Hitboxes - Fase 1")]
    public AttackHitbox stabHitbox;
    public AttackHitbox slashHitbox;
    public AttackHitbox slamHitbox;

    [Header("Hitboxes - Fase 2")]
    public AttackHitbox sweepHitbox;       // Barrido a ras de suelo
    public AttackHitbox grabHitbox;        // Agarre
    public AttackHitbox windBurstHitbox;   // Ráfaga de alas
    public AttackHitbox chargeHitbox;      // Carga de hombro: caja corta sobre el cuerpo (distinta de la del stab)

    [Header("Lanza (fase 2)")]
    public GameObject spearPrefab;         // Opcional: si está vacío, la lanza se crea por código
    public Vector2 spearSpawnOffset = new Vector2(1.2f, 0.3f);   // Desde el centro del boss, hacia delante

    [Header("Arena y teletransporte")]
    public bool autoArenaBounds = true;    // Detecta solo las paredes (capa Ground) lanzando un rayo a cada lado
    public float arenaMinX = -8f;          // Solo se usan si no se detectan las paredes
    public float arenaMaxX = 8f;
    public float teleportOffset = 2f;          // Teletransporte con tajo: distancia a la que aparece del jugador
    public float repositionMinOffset = 2f;     // Teletransporte de reposición: distancia mínima...
    public float repositionMaxOffset = 4.5f;   // ...y máxima a la que aparece del jugador

    [Header("Suelo")]
    public LayerMask groundLayer;          // Suelo y paredes (la lanza se clava y el límite de arena se detecta con esta capa)

    [Header("Físicas")]
    public float bossMass = 1000f;   // Masa muy alta: el jugador choca contra el boss pero no puede moverlo

    [Header("Depuración")]
    public bool debugKeys = true;
    // Fase 1: 1 estocada, 2 tajo ascendente, 3 combo, 4 aplastamiento
    // Fase 3: T salto con persecución, Y pilares de luz, U doble teletransporte, I multitajo
    // V dash vacío, B tajo hacia atrás, E teletransporte de escape
    // Fase 2: 5 combo con retraso, 6 teletransporte con tajo, 7 barrido, 8 agarre, 9 ráfaga de alas,
    //         0 lanza, C carga de hombro, R teletransporte de reposición (+ ataque posterior)

    private Color baseColor = Color.white;
    private Collider2D col;
    private Collider2D playerCol;
    private Rigidbody2D playerRb;
    private float corneredTimer;
    private bool carryingPlayer;
    private float playerGravity;
    private float carryBaseY;
    private bool warnedBounds;

    public BossStateMachine StateMachine { get; private set; }
    public BossBrain Brain { get; private set; }
    public bool IsStunned { get; private set; }   // Para el sistema del jugador (p. ej. bonus de daño)

    // Espera entre acciones según la fase
    public float IdleTime
    {
        get
        {
            int p = CurrentPhase;
            return p <= 1 ? idleTimePhase1 : (p == 2 ? idleTimePhase2 : idleTimePhase3);
        }
    }

    public bool IsPlayerCornered => corneredTimer >= cornerTime;

    // Avisos y recuperaciones más cortos en la fase final
    public float TempoMultiplier => CurrentPhase >= 3 ? phase3TempoMultiplier : 1f;

    // Suelo bajo el boss (los pilares de luz nacen de aquí)
    public float GroundY => (col != null ? col : GetComponent<Collider2D>()).bounds.min.y;

    // Velocidad horizontal del jugador (para adelantarse a donde va a estar)
    public float PlayerVelocityX => playerRb != null ? playerRb.linearVelocity.x : 0f;

    void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        rb.mass = bossMass;
        if (animator == null) animator = GetComponent<Animator>();
        if (sprite == null) sprite = GetComponent<SpriteRenderer>();
        if (sprite != null) baseColor = sprite.color;
        col = GetComponent<Collider2D>();
        StateMachine = new BossStateMachine();
        Brain = new BossBrain(this);

        if (groundLayer.value == 0)
            Debug.LogWarning("BossController: 'Ground Layer' está vacío. El aplastamiento, la lanza y los límites de la arena no funcionarán bien.");
    }

    void Start()
    {
        if (player != null)
        {
            playerCol = player.GetComponent<Collider2D>();
            playerRb = player.GetComponent<Rigidbody2D>();
        }
        Brain.LogUnavailableAttacks();
        StateMachine.ChangeState(new BossIdleState(this));
    }

    void Update()
    {
        StateMachine.Tick();
        UpdateCornered();
        if (debugKeys) HandleDebugKeys();
    }

    void FixedUpdate() => StateMachine.FixedTick();

    // ---------- Depuración ----------

    // Teclas para probar cada ataque por separado, sin esperar al azar
    private void HandleDebugKeys()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (kb.digit1Key.wasPressedThisFrame) Force(0);
        else if (kb.digit2Key.wasPressedThisFrame) Force(1);
        else if (kb.digit3Key.wasPressedThisFrame) Force(2);
        else if (kb.digit4Key.wasPressedThisFrame) Force(3);
        else if (kb.digit5Key.wasPressedThisFrame) Force(4);
        else if (kb.digit6Key.wasPressedThisFrame) Force(5);
        else if (kb.digit7Key.wasPressedThisFrame) Force(6);
        else if (kb.digit8Key.wasPressedThisFrame) Force(7);
        else if (kb.digit9Key.wasPressedThisFrame) Force(8);
        else if (kb.digit0Key.wasPressedThisFrame) Force(9);
        else if (kb.cKey.wasPressedThisFrame) Force(10);
        else if (kb.tKey.wasPressedThisFrame) Force(11);
        else if (kb.yKey.wasPressedThisFrame) Force(12);
        else if (kb.uKey.wasPressedThisFrame) Force(13);
        else if (kb.iKey.wasPressedThisFrame) Force(14);
        else if (kb.vKey.wasPressedThisFrame) StateMachine.ChangeState(new BossDashState(this));
        else if (kb.bKey.wasPressedThisFrame && slashHitbox != null) StateMachine.ChangeState(new BossBackDashSlashState(this));
        else if (kb.eKey.wasPressedThisFrame) StateMachine.ChangeState(new BossEscapeTeleportState(this));
        else if (kb.rKey.wasPressedThisFrame)
        {
            Debug.Log("Boss: teletransporte de reposición forzado");
            StateMachine.ChangeState(new BossRepositionState(this));
        }
    }

    private void Force(int index)
    {
        BossState attack = Brain.Force(index);
        if (attack != null) StateMachine.ChangeState(attack);
    }

    // ---------- Consultas ----------

    // Fase actual (1 si todavía no hay BossHealth asignado)
    public int CurrentPhase => health != null ? health.CurrentPhase : 1;

    // Capas del jugador (se toman de la hitbox del tajo)
    public LayerMask PlayerLayers => slashHitbox != null ? slashHitbox.targetLayers : new LayerMask();

    // Hacia dónde mira el boss: -1 izquierda, 1 derecha
    public float FacingDirection => Mathf.Sign(transform.localScale.x);

    // Distancia horizontal al jugador
    public float DistanceToPlayer()
    {
        return Mathf.Abs(player.position.x - transform.position.x);
    }

    // ¿Está el jugador en el aire? (rayo corto hacia abajo desde sus pies contra la capa Ground)
    public bool PlayerIsAirborne()
    {
        if (playerCol == null || groundLayer.value == 0) return false;

        Bounds b = playerCol.bounds;
        Vector2 feet = new Vector2(b.center.x, b.min.y + 0.05f);
        return Physics2D.Raycast(feet, Vector2.down, 0.35f, groundLayer).collider == null;
    }

    // ¿Está el boss tocando el suelo? Detecta una franja fina bajo su collider
    public bool IsGrounded()
    {
        Bounds b = col.bounds;
        Vector2 center = new Vector2(b.center.x, b.min.y - 0.05f);
        Vector2 size = new Vector2(b.size.x * 0.9f, 0.1f);
        return Physics2D.OverlapBox(center, size, 0f, groundLayer) != null;
    }

    // Límites de la arena. Con 'Auto Arena Bounds' se detectan las paredes lanzando un rayo a cada lado
    // (las paredes tienen que estar en la capa Ground); si no se encuentran, se usan Arena Min/Max X.
    public void GetArenaBounds(out float minX, out float maxX)
    {
        minX = arenaMinX;
        maxX = arenaMaxX;
        if (!autoArenaBounds) return;

        Collider2D c = col != null ? col : GetComponent<Collider2D>();
        if (c == null || groundLayer.value == 0) return;

        Bounds b = c.bounds;
        float margin = b.extents.x + 0.1f;
        RaycastHit2D left = Physics2D.Raycast(b.center, Vector2.left, 500f, groundLayer);
        RaycastHit2D right = Physics2D.Raycast(b.center, Vector2.right, 500f, groundLayer);

        if (left.collider != null) minX = left.point.x + margin;
        if (right.collider != null) maxX = right.point.x - margin;

        if ((left.collider == null || right.collider == null) && Application.isPlaying && !warnedBounds)
        {
            warnedBounds = true;
            Debug.LogWarning("BossController: no se han detectado las dos paredes de la arena (ponlas en la capa Ground). Se usan Arena Min X / Max X.");
        }
    }

    // ¿Hay una pared (capa Ground) justo delante del boss en esa dirección? (rayo corto desde su costado)
    public bool IsWallAhead(float direction)
    {
        if (col == null || groundLayer.value == 0) return false;

        Bounds b = col.bounds;
        Vector2 origin = new Vector2(b.center.x + direction * b.extents.x, b.center.y);
        return Physics2D.Raycast(origin, new Vector2(direction, 0f), 0.25f, groundLayer).collider != null;
    }

    // Cuenta el tiempo que el jugador lleva contra una pared con el boss pegado a él
    private void UpdateCornered()
    {
        if (player == null) { corneredTimer = 0f; return; }

        GetArenaBounds(out float minX, out float maxX);
        float px = player.position.x;
        float bx = transform.position.x;

        bool nearLeft = px - minX < cornerDistance && bx > px;     // Pared a su izquierda, boss a su derecha
        bool nearRight = maxX - px < cornerDistance && bx < px;
        bool cornered = (nearLeft || nearRight) && DistanceToPlayer() <= cornerBossDistance;

        corneredTimer = cornered ? corneredTimer + Time.deltaTime : 0f;
    }

    // ---------- Agarrar y arrastrar al jugador (tajo hacia atrás) ----------
    // TODO integración: el sistema del jugador de tus compañeros debería tener un estado "agarrado"
    // (sin control, sin gravedad) y un método para moverlo. Mientras tanto se mueve su Rigidbody2D.

    public bool IsCarryingPlayer => carryingPlayer;

    // Lo agarra: sin gravedad y sin velocidad
    public void BeginCarryPlayer()
    {
        if (playerRb == null || carryingPlayer) return;
        carryingPlayer = true;
        playerGravity = playerRb.gravityScale;
        carryBaseY = playerRb.position.y;
        playerRb.gravityScale = 0f;
        playerRb.linearVelocity = Vector2.zero;
    }

    // Lo mantiene en la posición X indicada y algo elevado
    public void CarryPlayerTo(float x, float lift, float liftSpeed)
    {
        if (!carryingPlayer) return;
        float y = Mathf.MoveTowards(playerRb.position.y, carryBaseY + lift, liftSpeed * Time.fixedDeltaTime);
        playerRb.linearVelocity = Vector2.zero;
        playerRb.position = new Vector2(x, y);
    }

    // Lo suelta: recupera la gravedad y cae
    public void ReleasePlayer()
    {
        if (!carryingPlayer) return;
        carryingPlayer = false;
        playerRb.gravityScale = playerGravity;
        playerRb.linearVelocity = Vector2.zero;
    }

    // Separación horizontal para que el jugador quede pegado delante del boss sin solaparse
    public float GripOffset()
    {
        float own = col != null ? col.bounds.extents.x : 0.5f;
        float theirs = playerCol != null ? playerCol.bounds.extents.x : 0.5f;
        return own + theirs + 0.1f;
    }

    // ---------- Decisiones ----------

    // Qué hace el boss al acabar un ataque: en la fase 3 muchas veces encadena otra acción sin pausa;
    // si no, espera un momento en Idle
    public BossState NextAfterAttack()
    {
        if (CurrentPhase >= 3 && Random.value < phase3ChainChance)
            return DecideNextAction();
        return new BossIdleState(this);
    }

    // Qué hace el boss al terminar la espera: caminar (fase 1), teletransportarse o atacar
    public BossState DecideNextAction()
    {
        if (IsPlayerCornered)
        {
            corneredTimer = 0f;
            if (CurrentPhase == 1)
            {
                if (slashHitbox != null) return new BossBackDashSlashState(this);
            }
            else
            {
                return new BossEscapeTeleportState(this);
            }
        }

        if (CurrentPhase == 1)
        {
            if (DistanceToPlayer() > meleeRange)
            {
                if (DistanceToPlayer() >= dashMinDistance && Random.value < dashChance)
                    return new BossDashState(this);
                return new BossApproachState(this);
            }
            return PickNextAttack();
        }

        return Brain.DecideAction();
    }

    // Elige un ataque para la distancia actual (el cerebro tiene en cuenta alcance, uso reciente y patrones)
    public BossState PickNextAttack()
    {
        return Brain.PickAttack() ?? new BossIdleState(this);
    }

    // ---------- Utilidades para los ataques ----------

    // Aviso visual provisional: tiñe al boss de rojo mientras prepara un ataque
    public void SetTelegraph(bool on)
    {
        if (sprite != null) sprite.color = on ? Color.red : baseColor;
    }

    // Aturdido: el boss se tiñe de verde mientras está expuesto
    public void SetStunned(bool on)
    {
        IsStunned = on;
        if (sprite != null) sprite.color = on ? Color.green : baseColor;
    }

    // Muestra u oculta el sprite del boss (teletransporte)
    public void SetVisible(bool visible)
    {
        if (sprite != null) sprite.enabled = visible;
    }

    // Detiene el movimiento horizontal (mantiene la vertical)
    public void StopHorizontal()
    {
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    // Aparece al otro lado del jugador respecto a donde estaba, a 'offset' unidades de él.
    // Si ese lado no cabe (pared), aparece en el otro. Se mueven a la vez el Transform y el Rigidbody
    // para que FacePlayer() use ya la posición nueva.
    public void TeleportBehindPlayer(float offset)
    {
        GetArenaBounds(out float minX, out float maxX);

        float side = DirectionToPlayer();                       // Lado del jugador respecto al boss
        float target = player.position.x + side * offset;       // Al otro lado del jugador
        if (target < minX || target > maxX)
            target = player.position.x - side * offset;         // No cabe: se queda delante
        MoveToX(Mathf.Clamp(target, minX, maxX));
    }

    // Se teletransporta lejos del jugador: hacia el lado de la arena con más espacio
    public void TeleportAwayFromPlayer()
    {
        GetArenaBounds(out float minX, out float maxX);
        float centerX = (minX + maxX) * 0.5f;
        float dir = Mathf.Sign(centerX - player.position.x);
        if (dir == 0f) dir = 1f;
        MoveToX(Mathf.Clamp(player.position.x + dir * escapeDistance, minX, maxX));
    }

    // Mueve a la vez el Transform y el Rigidbody para que FacePlayer() use ya la posición nueva
    private void MoveToX(float x)
    {
        Vector3 pos = transform.position;
        pos.x = x;
        transform.position = pos;
        rb.position = new Vector2(x, rb.position.y);
        rb.linearVelocity = Vector2.zero;
    }

    // Lanza una lanza hacia 'direction' (-1 izquierda, 1 derecha)
    public void SpawnSpear(float direction)
    {
        Vector2 position = (Vector2)transform.position + new Vector2(spearSpawnOffset.x * direction, spearSpawnOffset.y);

        BossSpear spear;
        if (spearPrefab != null)
            spear = Instantiate(spearPrefab, position, Quaternion.identity).GetComponent<BossSpear>();
        else
            spear = BossSpear.CreateDefault(position);

        if (spear == null)
        {
            Debug.LogWarning("BossController: el 'Spear Prefab' no tiene el script BossSpear.");
            return;
        }

        spear.Launch(direction, PlayerLayers, groundLayer);
    }

    // Dirección horizontal hacia el jugador: -1 izquierda, 1 derecha
    public float DirectionToPlayer()
    {
        return Mathf.Sign(player.position.x - transform.position.x);
    }

    // Gira el sprite para mirar al jugador
    public void FacePlayer()
    {
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * DirectionToPlayer();
        transform.localScale = scale;
    }

    // Líneas amarillas con los límites de la arena que se están usando (al seleccionar el boss en la ventana Scene)
    void OnDrawGizmosSelected()
    {
        GetArenaBounds(out float minX, out float maxX);
        Gizmos.color = Color.yellow;
        float y = transform.position.y;
        Gizmos.DrawLine(new Vector3(minX, y - 4f, 0f), new Vector3(minX, y + 4f, 0f));
        Gizmos.DrawLine(new Vector3(maxX, y - 4f, 0f), new Vector3(maxX, y + 4f, 0f));
    }
}
