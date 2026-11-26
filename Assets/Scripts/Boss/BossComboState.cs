using UnityEngine;

// Combo de 3 golpes: dos rápidos y un remate lento y más fuerte, con ventana de castigo al final.
// Los tiempos de cada golpe se ajustan en la lista 'hits'.
public class BossComboState : BossState
{
    private enum Step { Windup, Active, After }

    private struct Hit
    {
        public float windup;     // Aviso antes del golpe
        public float active;     // Tiempo con la hitbox activa
        public float after;      // Pausa tras el golpe (en el último, la recuperación)
        public float stepSpeed;  // Paso hacia delante durante el golpe
        public int damage;

        public Hit(float windup, float active, float after, float stepSpeed, int damage)
        {
            this.windup = windup;
            this.active = active;
            this.after = after;
            this.stepSpeed = stepSpeed;
            this.damage = damage;
        }
    }

    private readonly Hit[] hits =
    {
        new Hit(0.50f, 0.20f, 0.15f, 5f, 1),   // Golpe 1
        new Hit(0.20f, 0.20f, 0.15f, 5f, 1),   // Golpe 2
        new Hit(0.35f, 0.30f, 0.80f, 9f, 2),   // Remate lento y fuerte
    };

    private int index;
    private Step step;
    private float timer;
    private float direction;

    public BossComboState(BossController boss) : base(boss) { }

    public override void Enter()
    {
        index = 0;
        boss.rb.linearVelocity = new Vector2(0f, boss.rb.linearVelocity.y);
        BeginWindup();
    }

    private void BeginWindup()
    {
        step = Step.Windup;
        timer = hits[index].windup;
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
                    boss.slashHitbox.damage = hits[index].damage;
                    boss.slashHitbox.Activate();
                    step = Step.Active;
                    timer = hits[index].active;
                }
                break;

            case Step.Active:
                if (timer <= 0f)
                {
                    boss.slashHitbox.Deactivate();
                    boss.rb.linearVelocity = new Vector2(0f, boss.rb.linearVelocity.y);
                    step = Step.After;
                    timer = hits[index].after;
                }
                break;

            case Step.After:
                if (timer <= 0f)
                {
                    index++;
                    if (index >= hits.Length)
                        boss.StateMachine.ChangeState(new BossIdleState(boss));
                    else
                        BeginWindup();
                }
                break;
        }
    }

    public override void FixedTick()
    {
        if (step == Step.Active)
            boss.rb.linearVelocity = new Vector2(direction * hits[index].stepSpeed, boss.rb.linearVelocity.y);
    }

    public override void Exit()
    {
        boss.slashHitbox.Deactivate();
        boss.SetTelegraph(false);
        boss.rb.linearVelocity = new Vector2(0f, boss.rb.linearVelocity.y);
    }
}
