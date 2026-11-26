using UnityEngine;

// Tajo hacia atrás (fase 1): reacción cuando el boss tiene al jugador acorralado contra la pared.
// El boss da un tajo mientras dashea hacia atrás, hacia el centro de la arena. Si el tajo conecta,
// AGARRA al jugador y lo arrastra delante de él (elevado) y, antes de llegar al centro, lo SUELTA:
// el jugador cae mientras el boss sigue yendo hacia atrás, y así tiene espacio para volver a intentarlo.
// Usa Hitbox_Slash.
public class BossBackDashSlashState : BossSequenceState
{
    private const float DashSpeed = 13f;
    private const float MaxDashTime = 0.9f;
    private const float HopSpeed = 6f;          // Pequeño salto hacia arriba mientras retrocede
    private const float SlashTime = 0.3f;       // La hitbox del tajo solo está activa al principio del dash
    private const float Lift = 0.8f;            // Cuánto levanta al jugador mientras lo arrastra
    private const float LiftSpeed = 6f;
    private const float MinHoldTime = 0.3f;     // Tiempo mínimo agarrándolo
    private const float MaxHoldTime = 0.8f;
    private const float ReleaseDistance = 3f;   // Lo suelta cuando le faltan estas unidades para el centro

    private bool carrying;
    private float holdTimer;

    public BossBackDashSlashState(BossController boss) : base(boss) { }

    protected override void BuildSteps()
    {
        AttackHitbox hb = boss.slashHitbox;
        hb.OnHit += OnSlashHit;

        float dir = -1f;
        float centerX = 0f;
        float startTime = 0f;

        AddWindup(0.3f, trackPlayer: false);

        AddStep(MaxDashTime,
            onEnter: () =>
            {
                boss.GetArenaBounds(out float minX, out float maxX);
                centerX = (minX + maxX) * 0.5f;
                dir = -boss.FacingDirection;           // Hacia atrás: alejándose del jugador, hacia el centro
                startTime = Time.time;
                hb.damage = 2;
                hb.Activate();
                boss.rb.linearVelocity = new Vector2(0f, HopSpeed);   // Salto hacia atrás
            },
            onTick: () =>
            {
                if (Time.time - startTime > SlashTime) hb.Deactivate();

                float x = boss.transform.position.x;
                bool reachedCenter = dir > 0f ? x >= centerX : x <= centerX;
                if (reachedCenter || boss.IsWallAhead(dir)) FinishStep();
            },
            onFixedTick: () =>
            {
                boss.rb.linearVelocity = new Vector2(dir * DashSpeed, boss.rb.linearVelocity.y);

                if (!carrying) return;

                // Lo lleva pegado delante del boss; al final lo suelta y cae mientras el boss sigue retrocediendo
                holdTimer += Time.fixedDeltaTime;
                float remaining = Mathf.Abs(centerX - boss.rb.position.x);
                if (holdTimer >= MaxHoldTime || (holdTimer >= MinHoldTime && remaining <= ReleaseDistance))
                {
                    Release();
                }
                else
                {
                    float x = boss.rb.position.x + dir * DashSpeed * Time.fixedDeltaTime - dir * boss.GripOffset();
                    boss.CarryPlayerTo(x, Lift, LiftSpeed);
                }
            },
            onExit: () =>
            {
                hb.Deactivate();
                boss.StopHorizontal();
                Release();
            });

        AddRecovery(0.6f);
    }

    // Si el tajo conecta, lo agarra y se lo lleva hacia el centro
    private void OnSlashHit(Collider2D target, AttackHitbox hitbox)
    {
        carrying = true;
        holdTimer = 0f;
        boss.BeginCarryPlayer();
    }

    private void Release()
    {
        carrying = false;
        boss.ReleasePlayer();
    }

    protected override void Cleanup()
    {
        if (boss.slashHitbox != null)
        {
            boss.slashHitbox.OnHit -= OnSlashHit;
            boss.slashHitbox.Deactivate();
        }
        Release();
    }
}
