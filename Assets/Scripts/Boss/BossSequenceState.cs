using System;
using System.Collections.Generic;
using UnityEngine;

// Base para ataques definidos como una secuencia de pasos con duración:
// aviso -> golpe -> recuperación -> ...
// Cada ataque solo describe sus pasos en BuildSteps(); esta clase se encarga de avanzar entre ellos,
// de limpiar al terminar (o si el ataque se interrumpe) y de volver a Idle.
public abstract class BossSequenceState : BossState
{
    private class Step
    {
        public float duration;
        public Action onEnter, onTick, onFixedTick, onExit;
        public Func<bool> onlyIf;      // Si devuelve false, el paso se salta
    }

    private readonly List<Step> steps = new List<Step>();
    private int index = -1;
    private float timer;

    protected BossSequenceState(BossController boss) : base(boss) { }

    // Cada ataque añade aquí sus pasos con AddStep / AddWindup / AddStrike / AddRecovery
    protected abstract void BuildSteps();

    // Limpieza extra al salir del ataque (por ejemplo, desuscribirse de eventos)
    protected virtual void Cleanup() { }

    // Termina el paso actual en el siguiente Tick (útil para cortar un paso antes de tiempo)
    protected void FinishStep()
    {
        timer = 0f;
    }

    protected void AddStep(float duration, Action onEnter = null, Action onTick = null,
                           Action onFixedTick = null, Action onExit = null, Func<bool> onlyIf = null)
    {
        steps.Add(new Step
        {
            duration = duration,
            onEnter = onEnter,
            onTick = onTick,
            onFixedTick = onFixedTick,
            onExit = onExit,
            onlyIf = onlyIf
        });
    }

    // Aviso: el boss se tiñe de rojo y, si trackPlayer es true, sigue mirando al jugador
    protected void AddWindup(float duration, bool trackPlayer = true, Func<bool> onlyIf = null)
    {
        AddStep(duration * boss.TempoMultiplier,
            onEnter: () => { boss.FacePlayer(); boss.SetTelegraph(true); },
            onTick: () => { if (trackPlayer) boss.FacePlayer(); },
            onExit: () => boss.SetTelegraph(false),
            onlyIf: onlyIf);
    }

    // Pausa sin hacer nada (recuperación, ventana de castigo)
    protected void AddRecovery(float duration, Func<bool> onlyIf = null)
    {
        AddStep(duration * boss.TempoMultiplier, onlyIf: onlyIf);
    }

    // Aturdimiento: el boss queda expuesto y se tiñe de verde mientras dura
    protected void AddStun(float duration, Func<bool> onlyIf = null)
    {
        AddStep(duration,
            onEnter: () => boss.SetStunned(true),
            onExit: () => boss.SetStunned(false),
            onlyIf: onlyIf);
    }

    // Estado al que pasa el ataque cuando termina todos sus pasos (por defecto, esperar en Idle).
    // Un ataque puede sobrescribirlo para encadenar otro (por ejemplo, el teletransporte).
    protected virtual BossState GetNextState()
    {
        return boss.NextAfterAttack();
    }

    // Golpe: activa una hitbox durante 'duration' mientras el boss avanza hacia donde mira
    protected void AddStrike(AttackHitbox hitbox, float duration, int damage, float moveSpeed = 0f,
                             Action onTick = null, Func<bool> onlyIf = null)
    {
        float dir = 1f;
        AddStep(duration,
            onEnter: () =>
            {
                dir = boss.FacingDirection;
                hitbox.damage = damage;
                hitbox.Activate();
            },
            onTick: onTick,
            onFixedTick: () => boss.rb.linearVelocity = new Vector2(dir * moveSpeed, boss.rb.linearVelocity.y),
            onExit: () =>
            {
                hitbox.Deactivate();
                boss.StopHorizontal();
            },
            onlyIf: onlyIf);
    }

    public override void Enter()
    {
        steps.Clear();
        BuildSteps();
        boss.StopHorizontal();
        index = -1;
        NextStep();
    }

    private void NextStep()
    {
        if (index >= 0 && index < steps.Count)
            steps[index].onExit?.Invoke();

        do { index++; }
        while (index < steps.Count && steps[index].onlyIf != null && !steps[index].onlyIf());

        if (index >= steps.Count)
        {
            boss.StateMachine.ChangeState(GetNextState());
            return;
        }

        timer = steps[index].duration;
        steps[index].onEnter?.Invoke();
    }

    public override void Tick()
    {
        if (index < 0 || index >= steps.Count) return;

        steps[index].onTick?.Invoke();
        timer -= Time.deltaTime;
        if (timer <= 0f) NextStep();
    }

    public override void FixedTick()
    {
        if (index < 0 || index >= steps.Count) return;
        steps[index].onFixedTick?.Invoke();
    }

    public override void Exit()
    {
        // Si el ataque se interrumpe a mitad, se ejecuta la salida del paso en curso
        if (index >= 0 && index < steps.Count)
            steps[index].onExit?.Invoke();

        Cleanup();
        boss.SetTelegraph(false);
        boss.SetStunned(false);
        boss.SetVisible(true);
        boss.StopHorizontal();
    }
}
