// Doble teletransporte (fase 3): aparece detrás del jugador y tajea, desaparece al instante,
// reaparece al otro lado y tajea otra vez. Usa Hitbox_Slash.
public class BossDoubleTeleportState : BossSequenceState
{
    public BossDoubleTeleportState(BossController boss) : base(boss) { }

    protected override void BuildSteps()
    {
        AddWindup(0.25f);

        for (int i = 0; i < 2; i++)
        {
            AddStep(0.15f, onEnter: () => boss.SetVisible(false));

            AddStep(0.28f,
                onEnter: () =>
                {
                    boss.TeleportBehindPlayer(boss.teleportOffset);
                    boss.SetVisible(true);
                    boss.FacePlayer();
                    boss.SetTelegraph(true);
                },
                onTick: () => boss.FacePlayer(),
                onExit: () => boss.SetTelegraph(false));

            AddStrike(boss.slashHitbox, 0.2f, 2);
        }

        AddRecovery(0.5f);
    }
}
