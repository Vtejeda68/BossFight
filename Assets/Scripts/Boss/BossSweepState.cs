// Barrido a ras de suelo: golpe bajo y ancho que obliga al jugador a saltar.
// Usa Hitbox_Sweep (caja baja y ancha delante del boss, a la altura de los pies).
public class BossSweepState : BossSequenceState
{
    public BossSweepState(BossController boss) : base(boss) { }

    protected override void BuildSteps()
    {
        AddWindup(0.55f);
        AddStrike(boss.sweepHitbox, 0.3f, 2, 7f);
        AddRecovery(0.8f);
    }
}
