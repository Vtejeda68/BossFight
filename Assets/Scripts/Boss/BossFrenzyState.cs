using UnityEngine;

// Multitajo frenético (fase 3): cinco tajos seguidos avanzando hacia el jugador, casi sin pausa
// entre ellos. El último es más fuerte. Usa Hitbox_Slash.
public class BossFrenzyState : BossSequenceState
{
    private const int Hits = 5;
    private const float HitTime = 0.18f;
    private const float GapTime = 0.07f;
    private const float AdvanceSpeed = 7f;
    private const float MinGap = 1.8f;          // No se mete dentro del jugador (no lo empuja)

    public BossFrenzyState(BossController boss) : base(boss) { }

    protected override void BuildSteps()
    {
        AttackHitbox hb = boss.slashHitbox;

        AddWindup(0.3f);

        for (int i = 0; i < Hits; i++)
        {
            int damage = i == Hits - 1 ? 2 : 1;
            float dir = 1f;

            AddStep(HitTime,
                onEnter: () =>
                {
                    boss.FacePlayer();
                    dir = boss.FacingDirection;
                    hb.damage = damage;
                    hb.Activate();
                },
                onFixedTick: () =>
                {
                    float speed = boss.DistanceToPlayer() > MinGap && !boss.IsWallAhead(dir) ? AdvanceSpeed : 0f;
                    boss.rb.linearVelocity = new Vector2(dir * speed, boss.rb.linearVelocity.y);
                },
                onExit: () =>
                {
                    hb.Deactivate();
                    boss.StopHorizontal();
                });

            AddStep(GapTime, onEnter: () => boss.FacePlayer());
        }

        AddRecovery(0.6f);
    }

    protected override void Cleanup()
    {
        if (boss.slashHitbox != null) boss.slashHitbox.Deactivate();
    }
}
