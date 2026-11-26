// Teletransporte de escape (fases 2 y 3): reacción cuando el boss tiene al jugador acorralado
// contra la pared. Desaparece y reaparece lejos del jugador, le da un respiro y encadena un ataque
// elegido para esa distancia (lanza, carga...).
public class BossEscapeTeleportState : BossSequenceState
{
    public BossEscapeTeleportState(BossController boss) : base(boss) { }

    protected override void BuildSteps()
    {
        AddStep(0.2f, onEnter: () => boss.SetVisible(false));       // Desaparece

        AddStep(0.2f,                                               // Reaparece lejos del jugador
            onEnter: () =>
            {
                boss.TeleportAwayFromPlayer();
                boss.SetVisible(true);
                boss.FacePlayer();
            },
            onTick: () => boss.FacePlayer());

        AddRecovery(0.5f);                                          // Respiro para el jugador
    }

    protected override BossState GetNextState()
    {
        return boss.Brain.PickAttack(afterReposition: true) ?? new BossIdleState(boss);
    }
}
