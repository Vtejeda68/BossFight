using UnityEngine;

// Lanza del boss. Vuela en horizontal, se clava al tocar el suelo o una pared (capa Ground),
// parpadea en rojo y explota en un área.
//
// Se construye sola por código (cuerpo, colliders, hitboxes y un visual simple), así que no hace
// falta crear ningún prefab. Si quieres una lanza propia, crea un prefab con este script (y, si
// quieres, tu sprite como hijo) y arrástralo al campo 'Spear Prefab' del BossController.
public class BossSpear : MonoBehaviour
{
    [Header("Vuelo")]
    public float speed = 16f;
    public float maxFlightTime = 3f;     // Si no choca con nada, desaparece pasado este tiempo
    public int flightDamage = 1;

    [Header("Explosión")]
    public float fuseTime = 0.9f;        // Tiempo clavada antes de explotar
    public float explosionRadius = 1.8f;
    public float explosionTime = 0.25f;  // Tiempo que la explosión hace daño
    public int explosionDamage = 2;

    private enum SpearState { Flying, Stuck, Exploding }

    private SpearState state = SpearState.Flying;
    private Rigidbody2D rb;
    private AttackHitbox flightHitbox;
    private AttackHitbox explosionHitbox;
    private SpriteRenderer spearVisual;
    private SpriteRenderer explosionVisual;
    private Color spearColor;
    private LayerMask groundLayers;
    private float timer;

    private static Sprite whiteSprite;

    public static BossSpear CreateDefault(Vector2 position)
    {
        GameObject go = new GameObject("BossSpear");
        go.transform.position = position;
        return go.AddComponent<BossSpear>();   // Awake construye el resto
    }

    void Awake()
    {
        // Cuerpo cinemático: se mueve por velocidad y solo sirve para detectar contactos
        rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        // Visual de la lanza (si el prefab no trae sprite, se crea una barra simple)
        spearVisual = GetComponentInChildren<SpriteRenderer>();
        if (spearVisual == null)
        {
            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(transform, false);
            visual.transform.localScale = new Vector3(1.6f, 0.18f, 1f);
            spearVisual = visual.AddComponent<SpriteRenderer>();
            spearVisual.sprite = WhiteSprite();
            spearVisual.color = new Color(0.9f, 0.9f, 0.95f);
        }
        spearColor = spearVisual.color;

        // Contacto mientras vuela (hijo propio, para que no reciba los eventos de la explosión)
        GameObject flight = new GameObject("Flight");
        flight.transform.SetParent(transform, false);
        BoxCollider2D body = flight.AddComponent<BoxCollider2D>();
        body.size = new Vector2(1.6f, 0.25f);
        body.isTrigger = true;
        flightHitbox = flight.AddComponent<AttackHitbox>();
        flightHitbox.damage = flightDamage;
        flightHitbox.Activate();

        // Explosión: apagada hasta que la lanza explote
        GameObject explosion = new GameObject("Explosion");
        explosion.transform.SetParent(transform, false);
        CircleCollider2D circle = explosion.AddComponent<CircleCollider2D>();
        circle.radius = explosionRadius;
        circle.isTrigger = true;
        explosionHitbox = explosion.AddComponent<AttackHitbox>();
        explosionHitbox.damage = explosionDamage;
        explosionHitbox.knockback = 8f;

        GameObject explosionDisplay = new GameObject("ExplosionVisual");
        explosionDisplay.transform.SetParent(explosion.transform, false);
        explosionDisplay.transform.localScale = Vector3.one * explosionRadius * 2f;
        explosionVisual = explosionDisplay.AddComponent<SpriteRenderer>();
        explosionVisual.sprite = WhiteSprite();
        explosionVisual.color = new Color(1f, 0.5f, 0.1f, 0.5f);
        explosionVisual.enabled = false;
    }

    // Lanza la lanza hacia 'direction' (-1 izquierda, 1 derecha)
    public void Launch(float direction, LayerMask playerLayers, LayerMask groundLayers)
    {
        this.groundLayers = groundLayers;
        flightHitbox.targetLayers = playerLayers;
        explosionHitbox.targetLayers = playerLayers;

        direction = Mathf.Sign(direction);
        transform.localScale = new Vector3(direction, 1f, 1f);
        rb.linearVelocity = new Vector2(direction * speed, 0f);

        state = SpearState.Flying;
        timer = 0f;
    }

    void Update()
    {
        switch (state)
        {
            case SpearState.Flying:
                timer += Time.deltaTime;
                if (timer >= maxFlightTime) Destroy(gameObject);
                break;

            case SpearState.Stuck:
                // Parpadea en rojo para avisar de la explosión
                spearVisual.color = Color.Lerp(spearColor, Color.red, Mathf.PingPong(Time.time * 8f, 1f));
                timer -= Time.deltaTime;
                if (timer <= 0f) Explode();
                break;

            case SpearState.Exploding:
                timer -= Time.deltaTime;
                if (timer <= 0f) Destroy(gameObject);
                break;
        }
    }

    // Recibe los contactos de todos sus colliders hijos (comparten el mismo Rigidbody2D)
    void OnTriggerEnter2D(Collider2D other)
    {
        if (state != SpearState.Flying) return;
        if ((groundLayers.value & (1 << other.gameObject.layer)) == 0) return;

        Stick();
    }

    private void Stick()
    {
        state = SpearState.Stuck;
        rb.linearVelocity = Vector2.zero;
        flightHitbox.Deactivate();
        timer = fuseTime;
    }

    private void Explode()
    {
        state = SpearState.Exploding;
        spearVisual.enabled = false;
        explosionVisual.enabled = true;
        explosionHitbox.damage = explosionDamage;
        explosionHitbox.Activate();
        timer = explosionTime;
    }

    // Sprite blanco de 1x1 unidades generado por código (para no depender de ningún asset)
    private static Sprite WhiteSprite()
    {
        if (whiteSprite == null)
        {
            Texture2D tex = Texture2D.whiteTexture;
            whiteSprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height),
                                        new Vector2(0.5f, 0.5f), tex.width);
        }
        return whiteSprite;
    }
}
