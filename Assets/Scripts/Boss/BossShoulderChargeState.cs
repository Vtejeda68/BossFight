using UnityEngine;

// Carga de hombro: embestida larga hacia donde mira. Se esquiva saltando o con el dash.
// Si choca con la pared queda aturdido (verde) más tiempo: ventana de castigo.
// Usa Hitbox_Charge: una caja que cubre solo el cuerpo/hombro del boss (distinta de Hitbox_Stab,
// que es larga, para que la carga no haga daño sin tocar al jugador).
public class BossShoulderChargeState : BossSequenceState
{
    private const float ChargeSpeed = 15f;
    private const float MaxChargeTime = 1.0f;

    private bool hitWall;

    public BossShoulderChargeState(BossController boss) : base(boss) { }

    protected override void BuildSteps()
    {
        float dir = 1f;
        float startTime = 0f;

        AddWindup(0.7f);                       // Aviso largo, sigue al jugador

        AddStep(MaxChargeTime,
            onEnter: () =>
            {
                dir = boss.FacingDirection;    // Dirección bloqueada al empezar
                startTime = Time.time;
                boss.chargeHitbox.damage = 2;
                boss.chargeHitbox.Activate();
            },
            onTick: () =>
            {
                // Termina al tener una pared delante o si algo lo frena en seco
                bool blocked = Time.time - startTime > 0.2f && Mathf.Abs(boss.rb.linearVelocity.x) < 1f;
                if (boss.IsWallAhead(dir) || blocked)
                {
                    hitWall = true;
                    FinishStep();
                }
            },
            onFixedTick: () => boss.rb.linearVelocity = new Vector2(dir * ChargeSpeed, boss.rb.linearVelocity.y),
            onExit: () =>
            {
                boss.chargeHitbox.Deactivate();
                boss.StopHorizontal();
            });

        AddRecovery(0.9f, onlyIf: () => !hitWall);
        AddStun(1.6f, onlyIf: () => hitWall);       // Aturdido contra la pared (verde)
    }
}
