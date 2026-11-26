using System;
using UnityEngine;

// Hitbox reutilizable para los ataques del boss.
// Se activa/desactiva desde el Animator (eventos de animación) o desde el código de un estado.
// También sirve como hitbox de contacto del cuerpo del boss (modo continuo, siempre activa).
[RequireComponent(typeof(Collider2D))]
public class AttackHitbox : MonoBehaviour
{
    [Header("Datos del ataque")]
    public int damage = 1;
    public float knockback = 5f;
    public bool parryable = true;          // Para el parry del dash divino
    public bool ignoresDashInvulnerability = false;   // Agarre: el dash con invulnerabilidad no lo esquiva
    public LayerMask targetLayers;         // Capa del jugador

    [Header("Daño por contacto (hitbox del cuerpo)")]
    public bool startActive = false;       // Activa desde el principio, sin llamar a Activate()
    public bool continuous = false;        // Vuelve a golpear mientras el jugador siga dentro
    public float rehitInterval = 1f;       // Segundos entre golpes en modo continuo

    // Para que otros sistemas (efectos, parry, sonido) reaccionen al golpe
    public event Action<Collider2D, AttackHitbox> OnHit;

    private Collider2D col;
    private bool hasHit;                   // Un solo golpe por activación (modo normal)
    private float nextHitTime;             // Próximo golpe permitido (modo continuo)

    void Awake()
    {
        col = GetComponent<Collider2D>();
        col.isTrigger = true;
        col.enabled = startActive;
    }

    public void Activate()
    {
        hasHit = false;
        nextHitTime = 0f;
        col.enabled = true;
    }

    public void Deactivate()
    {
        col.enabled = false;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        TryHit(other);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (continuous) TryHit(other);
    }

    private void TryHit(Collider2D other)
    {
        if ((targetLayers.value & (1 << other.gameObject.layer)) == 0) return;

        if (continuous)
        {
            if (Time.time < nextHitTime) return;
            nextHitTime = Time.time + rehitInterval;
        }
        else
        {
            if (hasHit) return;
            hasHit = true;
        }

        DealDamage(other);
        OnHit?.Invoke(other, this);
    }

    private void DealDamage(Collider2D target)
    {
        // TODO: AQUÍ DEBERÍA OCURRIR EL DAÑO.
        // Cuando exista el sistema de vida del jugador, llamar a su método de daño
        // pasando 'damage', 'knockback' y la dirección del golpe.
        float direction = Mathf.Sign(target.transform.position.x - transform.position.x);
        Debug.Log($"[PLACEHOLDER] Daño al jugador: {damage} (knockback {knockback}, dirección {direction}, parryable: {parryable}, ignora dash: {ignoresDashInvulnerability})");
    }
}
