// Teletransporte con tajo: desaparece, reaparece al otro lado del jugador, se gira hacia él y ataca.
// Se puede usar a cualquier distancia. Usa Hitbox_Slash.
public class BossTeleportStrikeState : BossSequenceState
{
    public BossTeleportStrikeState(BossController boss) : base(boss) { }

    protected override void BuildSteps()
    {
        AddWindup(0.4f);                                          // Aviso antes de desaparecer

        AddStep(0.25f, onEnter: () => boss.SetVisible(false));    // Desaparece

        AddStep(0.35f,                                            // Reaparece: aviso corto antes del tajo
            onEnter: () =>
            {
                boss.TeleportBehindPlayer(boss.teleportOffset);
                boss.SetVisible(true);
                boss.FacePlayer();                                // Mirando al jugador, para que el tajo conecte
                boss.SetTelegraph(true);
            },
            onTick: () => boss.FacePlayer(),                      // Y sigue mirándolo hasta el golpe
            onExit: () => boss.SetTelegraph(false));

        AddStrike(boss.slashHitbox, 0.25f, 2);                    // Tajo
        AddRecovery(0.7f);
    }
}
