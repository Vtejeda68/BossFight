using UnityEngine;

// Estado de espera entre acciones. Al terminar la espera, el boss decide qué hacer:
// caminar hacia el jugador (fase 1), teletransportarse o atacar (fases 2 y 3).
public class BossIdleState : BossState
{
    private float timer;

    public BossIdleState(BossController boss) : base(boss) { }

    public override void Enter()
    {
        timer = boss.IdleTime;     // Más corta en las fases avanzadas
        boss.rb.linearVelocity = Vector2.zero;
    }

    public override void Tick()
    {
        boss.FacePlayer();
        timer -= Time.deltaTime;

        if (timer <= 0f)
            boss.StateMachine.ChangeState(boss.DecideNextAction());
    }
}
