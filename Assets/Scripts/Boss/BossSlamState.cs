using UnityEngine;

// Aplastamiento aéreo: agacharse (aviso), subir, quedarse un instante en el aire, caer en picado
// y golpear el suelo con una onda de impacto a su alrededor.
// Mientras sube y se mantiene en el aire sigue horizontalmente al jugador; al caer ya no.
public class BossSlamState : BossState
{
    private enum Step { Windup, Rise, Hang, Fall, Impact, Recovery }

    private const float WindupTime = 0.5f;
    private const float RiseTime = 0.3f;
    private const float RiseSpeed = 14f;
    private const float HangTime = 0.3f;
    private const float AirMoveSpeed = 6f;     // Seguimiento horizontal en el aire
    private const float FallSpeed = 28f;
    private const float ImpactTime = 0.25f;    // Tiempo con la hitbox de impacto activa
    private const float RecoveryTime = 0.9f;   // Ventana de castigo tras aterrizar
    private const int Damage = 2;

    private Step step;
    private float timer;
    private float fallTimer;
    private float originalGravity;

    public BossSlamState(BossController boss) : base(boss) { }

    public override void Enter()
    {
        originalGravity = boss.rb.gravityScale;
        step = Step.Windup;
        timer = WindupTime;
        boss.rb.linearVelocity = Vector2.zero;
        boss.FacePlayer();
        boss.SetTelegraph(true);
    }

    public override void Tick()
    {
        timer -= Time.deltaTime;

        switch (step)
        {
            case Step.Windup:
                boss.FacePlayer();
                if (timer <= 0f)
                {
                    boss.rb.gravityScale = 0f;   // Control manual del movimiento vertical
                    step = Step.Rise;
                    timer = RiseTime;
                }
                break;

            case Step.Rise:
                if (timer <= 0f)
                {
                    step = Step.Hang;
                    timer = HangTime;
                }
                break;

            case Step.Hang:
                if (timer <= 0f)
                {
                    boss.SetTelegraph(false);
                    fallTimer = 0f;
                    step = Step.Fall;
                }
                break;

            case Step.Fall:
                fallTimer += Time.deltaTime;
                // Aterriza si detecta el suelo, o (por seguridad, p. ej. si 'Ground Layer' está vacío)
                // si lleva un instante cayendo y el choque ya lo ha frenado
                bool landed = boss.IsGrounded() ||
                              (fallTimer > 0.1f && Mathf.Abs(boss.rb.linearVelocity.y) < 0.5f);
                if (landed)
                {
                    boss.rb.linearVelocity = Vector2.zero;
                    boss.rb.gravityScale = originalGravity;
                    boss.slamHitbox.damage = Damage;
                    boss.slamHitbox.Activate();
                    step = Step.Impact;
                    timer = ImpactTime;
                }
                break;

            case Step.Impact:
                if (timer <= 0f)
                {
                    boss.slamHitbox.Deactivate();
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
        switch (step)
        {
            case Step.Rise:
                boss.rb.linearVelocity = new Vector2(boss.DirectionToPlayer() * AirMoveSpeed, RiseSpeed);
                boss.FacePlayer();
                break;

            case Step.Hang:
                boss.rb.linearVelocity = new Vector2(boss.DirectionToPlayer() * AirMoveSpeed, 0f);
                boss.FacePlayer();
                break;

            case Step.Fall:
                boss.rb.linearVelocity = new Vector2(0f, -FallSpeed);
                break;
        }
    }

    public override void Exit()
    {
        boss.rb.gravityScale = originalGravity;
        boss.slamHitbox.Deactivate();
        boss.SetTelegraph(false);
        boss.rb.linearVelocity = Vector2.zero;
    }
}
