// Ráfaga de alas: onda de viento en área alrededor del boss que empuja al jugador hacia atrás.
// Usa Hitbox_WindBurst (círculo grande centrado en el boss). El empuje se configura con el campo
// 'Knockback' de la hitbox; aplicarlo al jugador se conecta en DealDamage de AttackHitbox.
public class BossWindBurstState : BossSequenceState
{
    public BossWindBurstState(BossController boss) : base(boss) { }

    protected override void BuildSteps()
    {
        AddWindup(0.6f, trackPlayer: false);                  // Se abre de alas: aviso largo
        AddStrike(boss.windBurstHitbox, 0.35f, 1);            // Onda
        AddRecovery(0.7f);
    }
}
