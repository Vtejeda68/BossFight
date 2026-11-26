using UnityEngine;

// Tajo ascendente: ataque rápido de corto alcance, con un pequeño paso hacia delante.
public class BossUpSlashState : BossState
{
    private enum Step { Windup, Slash, Recovery }

    private const float WindupTime = 0.45f;
    private const float SlashTime = 0.25f;
    private const float RecoveryTime = 0.55f;
    private const float StepSpeed = 6f;
    private const int Damage = 1;

    private Step step;
    private float timer;
    private float direction;

    public BossUpSlashState(BossController boss) : base(boss) { }

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
                boss.FacePlayer();
                if (timer <= 0f)
                {
                    direction = boss.DirectionToPlayer();
                    boss.SetTelegraph(false);
                    boss.slashHitbox.damage = Damage;
                    boss.slashHitbox.Activate();
                    step = Step.Slash;
                    timer = SlashTime;
                }
                break;

            case Step.Slash:
                if (timer <= 0f)
                {
                    boss.slashHitbox.Deactivate();
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
        if (step == Step.Slash)
            boss.rb.linearVelocity = new Vector2(direction * StepSpeed, boss.rb.linearVelocity.y);
    }

    public override void Exit()
    {
        boss.slashHitbox.Deactivate();
        boss.SetTelegraph(false);
        boss.rb.linearVelocity = new Vector2(0f, boss.rb.linearVelocity.y);
    }
}
