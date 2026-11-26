using UnityEngine;

// Salto con persecución (fase 3): salta, se queda en el aire siguiendo al jugador por encima
// (aviso en rojo), y cae en picado con una onda de impacto. Más rápido y agresivo que el
// aplastamiento de la fase 1. Usa Hitbox_Slam.
public class BossChaseJumpState : BossSequenceState
{
    private const float RiseTime = 0.25f;
    private const float RiseSpeed = 15f;
    private const float ChaseTime = 0.9f;
    private const float ChaseGain = 6f;        // Cuánto corrige hacia el jugador
    private const float ChaseMaxSpeed = 11f;
    private const float FallSpeed = 32f;
    private const float MaxFallTime = 1.5f;
    private const float ImpactTime = 0.3f;
    private const int Damage = 2;

    private float originalGravity;
    private float fallTimer;

    public BossChaseJumpState(BossController boss) : base(boss) { }

    protected override void BuildSteps()
    {
        AddWindup(0.3f);

        AddStep(RiseTime,
            onEnter: () =>
            {
                originalGravity = boss.rb.gravityScale;
                boss.rb.gravityScale = 0f;            // Control manual del movimiento vertical
            },
            onFixedTick: () => boss.rb.linearVelocity = new Vector2(boss.DirectionToPlayer() * ChaseMaxSpeed, RiseSpeed));

        AddStep(ChaseTime,                            // Persigue al jugador desde el aire
            onEnter: () => boss.SetTelegraph(true),
            onTick: () => boss.FacePlayer(),
            onFixedTick: () =>
            {
                float dx = boss.player.position.x - boss.transform.position.x;
                float vx = Mathf.Clamp(dx * ChaseGain, -ChaseMaxSpeed, ChaseMaxSpeed);
                boss.rb.linearVelocity = new Vector2(vx, 0f);
            });

        AddStep(MaxFallTime,                          // Cae en picado
            onEnter: () =>
            {
                boss.SetTelegraph(false);
                fallTimer = 0f;
            },
            onTick: () =>
            {
                fallTimer += Time.deltaTime;
                bool landed = boss.IsGrounded() ||
                              (fallTimer > 0.1f && Mathf.Abs(boss.rb.linearVelocity.y) < 0.5f);
                if (landed) FinishStep();
            },
            onFixedTick: () => boss.rb.linearVelocity = new Vector2(0f, -FallSpeed),
            onExit: () =>
            {
                boss.rb.linearVelocity = Vector2.zero;
                boss.rb.gravityScale = originalGravity;
            });

        AddStrike(boss.slamHitbox, ImpactTime, Damage);   // Onda de impacto
        AddRecovery(0.5f);
    }

    protected override void Cleanup()
    {
        boss.rb.gravityScale = originalGravity;
        if (boss.slamHitbox != null) boss.slamHitbox.Deactivate();
    }
}
