// Lanzamiento de lanza: la lanza vuela en horizontal, se clava en el suelo o la pared y explota
// tras un breve retraso. Controla el espacio cuando el jugador está lejos.
// La lanza (BossSpear) se crea sola por código; no necesita hitbox en el boss.
public class BossSpearThrowState : BossSequenceState
{
    public BossSpearThrowState(BossController boss) : base(boss) { }

    protected override void BuildSteps()
    {
        AddWindup(0.6f);                                                      // Apunta al jugador
        AddStep(0.15f, onEnter: () => boss.SpawnSpear(boss.FacingDirection)); // Lanza
        AddRecovery(0.9f);
    }
}
