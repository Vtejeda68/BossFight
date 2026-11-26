using UnityEngine;

// Pilar de luz: aparece como una columna roja translúcida (aviso), y pasado 'warnTime' se vuelve
// luminoso y hace daño durante 'activeTime'. Se construye solo por código, sin prefab.
public class BossPillar : MonoBehaviour
{
    private AttackHitbox hitbox;
    private SpriteRenderer visual;
    private float warnTime;
    private float activeTime;
    private float timer;
    private bool active;

    private static Sprite whiteSprite;

    // basePos = punto del suelo donde cae el pilar (el pilar sube desde ahí 'height' unidades)
    public static BossPillar Spawn(Vector2 basePos, float width, float height, float warnTime,
                                   float activeTime, int damage, LayerMask playerLayers)
    {
        GameObject go = new GameObject("BossPillar");
        go.transform.position = new Vector3(basePos.x, basePos.y + height * 0.5f, 0f);

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(width, height);
        box.isTrigger = true;

        AttackHitbox hb = go.AddComponent<AttackHitbox>();
        hb.damage = damage;
        hb.knockback = 6f;
        hb.targetLayers = playerLayers;

        GameObject v = new GameObject("Visual");
        v.transform.SetParent(go.transform, false);
        v.transform.localScale = new Vector3(width, height, 1f);
        SpriteRenderer sr = v.AddComponent<SpriteRenderer>();
        sr.sprite = WhiteSprite();
        sr.sortingOrder = 5;
        sr.color = new Color(1f, 0.15f, 0.1f, 0.2f);

        BossPillar pillar = go.AddComponent<BossPillar>();
        pillar.hitbox = hb;
        pillar.visual = sr;
        pillar.warnTime = warnTime;
        pillar.activeTime = activeTime;
        pillar.timer = warnTime;
        return pillar;
    }

    void Update()
    {
        timer -= Time.deltaTime;

        if (!active)
        {
            // El aviso se va haciendo más intenso hasta el golpe
            float t = 1f - Mathf.Clamp01(timer / Mathf.Max(0.01f, warnTime));
            visual.color = new Color(1f, 0.15f, 0.1f, Mathf.Lerp(0.2f, 0.55f, t));

            if (timer <= 0f)
            {
                active = true;
                timer = activeTime;
                visual.color = new Color(1f, 0.95f, 0.6f, 0.9f);
                hitbox.Activate();
            }
        }
        else if (timer <= 0f)
        {
            Destroy(gameObject);
        }
    }

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
