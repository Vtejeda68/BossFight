using UnityEngine;

// Dash vacío (fase 1): el boss dashea hacia el jugador sin hacer daño y, nada más terminar,
// encadena uno de sus ataques (sin pasar por la espera de Idle). Le da movilidad aparte de andar.
// No usa hitbox. El aviso del ataque siguiente lo da su propio windup.
public class BossDashState : BossSequenceState
{
    private const float DashSpeed = 16f;
    private const float MaxDashTime = 0.4f;
    private const float StopDistance = 2.2f;   // Se detiene al quedar a esta distancia del jugador

    public BossDashState(BossController boss) : base(boss) { }

    protected override void BuildSteps()
    {
        float dir = 1f;

        AddStep(MaxDashTime,
            onEnter: () =>
            {
                boss.FacePlayer();
                dir = boss.FacingDirection;
            },
            onTick: () =>
            {
                if (boss.DistanceToPlayer() <= StopDistance || boss.IsWallAhead(dir))
                    FinishStep();
            },
            onFixedTick: () => boss.rb.linearVelocity = new Vector2(dir * DashSpeed, boss.rb.linearVelocity.y),
            onExit: () => boss.StopHorizontal());
    }

    // Nada más acabar el dash, ataca
    protected override BossState GetNextState()
    {
        return boss.PickNextAttack();
    }
}
