using UnityEngine;

// Agarre: el dash con invulnerabilidad NO lo esquiva, solo saltando.
// Si la hitbox toca al jugador, lo "sujeta" un momento y le da un golpe final; si falla, el boss
// queda expuesto bastante más tiempo.
// Usa Hitbox_Grab (caja corta delante del boss).
public class BossGrabState : BossSequenceState
{
    private bool grabbed;

    public BossGrabState(BossController boss) : base(boss) { }

    protected override void BuildSteps()
    {
        AttackHitbox hb = boss.grabHitbox;
        hb.parryable = false;
        hb.ignoresDashInvulnerability = true;   // Aviso para el sistema del jugador (ver AttackHitbox)
        hb.OnHit += OnGrabHit;

        AddWindup(0.5f);

        // Intento de agarre: avanza un poco y se corta en cuanto toca al jugador
        AddStrike(hb, 0.25f, 1, 6f, onTick: () => { if (grabbed) FinishStep(); });

        // Agarre conseguido
        AddStep(0.6f,
            onlyIf: () => grabbed,
            onEnter: () =>
            {
                boss.StopHorizontal();
                // TODO: AQUÍ SE DEBE SUJETAR AL JUGADOR (bloquear su movimiento unos 0,6 s).
                // Hay que conectarlo con el control del jugador cuando se integre.
                Debug.Log("[PLACEHOLDER] Jugador agarrado: bloquear el control del jugador");
            });
        AddStrike(hb, 0.15f, 3, onlyIf: () => grabbed);   // Golpe final (reactiva la hitbox: vuelve a golpear)
        AddRecovery(0.6f, onlyIf: () => grabbed);

        // Agarre fallado: el boss queda aturdido (verde) y expuesto
        AddStun(1.1f, onlyIf: () => !grabbed);
    }

    private void OnGrabHit(Collider2D target, AttackHitbox hitbox)
    {
        grabbed = true;
    }

    protected override void Cleanup()
    {
        if (boss.grabHitbox != null)
        {
            boss.grabHitbox.OnHit -= OnGrabHit;
            boss.grabHitbox.Deactivate();
        }
    }
}
