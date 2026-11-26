// Máquina de estados simple: un único estado activo a la vez.
public class BossStateMachine
{
    public BossState Current { get; private set; }

    public void ChangeState(BossState next)
    {
        Current?.Exit();
        Current = next;
        Current?.Enter();
    }

    public void Tick() => Current?.Tick();
    public void FixedTick() => Current?.FixedTick();
}
