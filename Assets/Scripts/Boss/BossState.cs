// Clase base de todos los estados del boss (idle, ataques, transiciones de fase...).
public abstract class BossState
{
    protected readonly BossController boss;

    protected BossState(BossController boss)
    {
        this.boss = boss;
    }

    public virtual void Enter() { }      // Al entrar en el estado
    public virtual void Tick() { }       // Cada Update
    public virtual void FixedTick() { }  // Cada FixedUpdate (movimiento con físicas)
    public virtual void Exit() { }       // Al salir del estado
}
