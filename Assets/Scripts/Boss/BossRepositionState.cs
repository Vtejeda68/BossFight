// Teletransporte de reposición: es la forma de moverse del boss por el campo en las fases 2 y 3.
// Desaparece, reaparece detrás del jugador a una distancia variable y, en cuanto reaparece,
// el cerebro elige el siguiente ataque según la nueva distancia y lo que ha usado últimamente
// (así puede enlazar con agarre, barrido, combo, ráfaga...).
// El aviso de ese ataque lo da su propio windup, así que el jugador siempre puede reaccionar.
public class BossRepositionState : BossSequenceState
{
    public BossRepositionState(BossController boss) : base(boss) { }

    protected override void BuildSteps()
    {
        AddStep(0.25f, onEnter: () => boss.SetVisible(false));         // Desaparece

        AddStep(0.2f,                                                  // Reaparece
            onEnter: () =>
            {
                boss.TeleportBehindPlayer(boss.Brain.RandomRepositionOffset());
                boss.SetVisible(true);
                boss.FacePlayer();
            },
            onTick: () => boss.FacePlayer());
    }

    // En vez de volver a Idle, encadena ya con un ataque elegido para la nueva distancia
    protected override BossState GetNextState()
    {
        return boss.Brain.PickAttack(afterReposition: true) ?? new BossIdleState(boss);
    }
}
