using UnityEngine;

// Caminar hacia el jugador hasta estar a distancia de ataque. SOLO se usa en la fase 1:
// en las fases 2 y 3 el boss se mueve únicamente con movimientos ofensivos.
public class BossApproachState : BossState
{
    private const float MaxWalkTime = 2.5f;   // Si no llega a rango, ataca igualmente

    private float timer;

    public BossApproachState(BossController boss) : base(boss) { }

    public override void Enter()
    {
        timer = MaxWalkTime;
    }

    public override void Tick()
    {
        boss.FacePlayer();
        timer -= Time.deltaTime;

        if (boss.DistanceToPlayer() <= boss.meleeRange || timer <= 0f)
            boss.StateMachine.ChangeState(boss.PickNextAttack());
    }

    public override void FixedTick()
    {
        boss.rb.linearVelocity = new Vector2(boss.DirectionToPlayer() * boss.walkSpeed, boss.rb.linearVelocity.y);
    }

    public override void Exit()
    {
        boss.rb.linearVelocity = new Vector2(0f, boss.rb.linearVelocity.y);
    }
}
