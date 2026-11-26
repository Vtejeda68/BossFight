using UnityEngine;

// Estocada con avance: aviso (telegraph) -> embestida con hitbox activa -> recuperación.
// Los tiempos y la velocidad se pueden ajustar aquí arriba para probar el "feeling".
public class BossStabState : BossState
{
    private enum Step { Windup, Lunge, Recovery }

    private const float WindupTime = 0.6f;    // Tiempo de aviso: el jugador puede reaccionar
    private const float LungeTime = 0.25f;    // Duración de la embestida
    private const float RecoveryTime = 0.7f;  // Ventana de castigo para el jugador
    private const float LungeSpeed = 18f;

    private Step step;
    private float timer;
    private float direction;

    public BossStabState(BossController boss) : base(boss) { }

    public override void Enter()
    {
        step = Step.Windup;
        timer = WindupTime;
        boss.rb.linearVelocity = new Vector2(0f, boss.rb.linearVelocity.y);
        boss.FacePlayer();
        boss.SetTelegraph(true);
    }

    public override void Tick()
    {
        timer -= Time.deltaTime;

        switch (step)
        {
            case Step.Windup:
                boss.FacePlayer();            // Sigue al jugador durante el aviso
                if (timer <= 0f)
                {
                    direction = boss.DirectionToPlayer();   // Dirección bloqueada al empezar la embestida
                    boss.SetTelegraph(false);
                    boss.stabHitbox.Activate();
                    step = Step.Lunge;
                    timer = LungeTime;
                }
                break;

            case Step.Lunge:
                if (timer <= 0f)
                {
                    boss.stabHitbox.Deactivate();
                    boss.rb.linearVelocity = new Vector2(0f, boss.rb.linearVelocity.y);
                    step = Step.Recovery;
                    timer = RecoveryTime;
                }
                break;

            case Step.Recovery:
                if (timer <= 0f)
                    boss.StateMachine.ChangeState(new BossIdleState(boss));
                break;
        }
    }

    public override void FixedTick()
    {
        if (step == Step.Lunge)
            boss.rb.linearVelocity = new Vector2(direction * LungeSpeed, boss.rb.linearVelocity.y);
    }

    public override void Exit()
    {
        boss.stabHitbox.Deactivate();
        boss.SetTelegraph(false);
        boss.rb.linearVelocity = new Vector2(0f, boss.rb.linearVelocity.y);
    }
}
