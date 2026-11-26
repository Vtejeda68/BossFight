// Combo con retraso: igual que el combo de la fase 1, pero con una pausa larga tras el primer golpe.
// Castiga al jugador que esquiva "a ciegas" con el dash y premia al que lee el ritmo (parry divino).
// Usa Hitbox_Slash.
public class BossDelayedComboState : BossSequenceState
{
    public BossDelayedComboState(BossController boss) : base(boss) { }

    protected override void BuildSteps()
    {
        AttackHitbox hb = boss.slashHitbox;

        AddWindup(0.5f);
        AddStrike(hb, 0.2f, 1, 6f);      // Golpe 1
        AddRecovery(0.55f);              // El retraso: el segundo golpe tarda más de lo esperado
        AddWindup(0.15f);                // Aviso muy corto
        AddStrike(hb, 0.2f, 1, 6f);      // Golpe 2
        AddRecovery(0.2f);
        AddWindup(0.35f);
        AddStrike(hb, 0.3f, 2, 9f);      // Remate fuerte
        AddRecovery(0.8f);               // Ventana de castigo
    }
}
