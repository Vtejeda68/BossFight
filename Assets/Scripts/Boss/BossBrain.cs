using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

// "Cerebro" del boss: decide qué hacer a continuación.
//
// Para elegir un ataque tiene en cuenta:
//  - la fase actual y si el ataque tiene sus hitboxes asignadas
//  - la distancia al jugador frente al ALCANCE REAL de cada ataque (los que avanzan llegan más lejos)
//  - un enfriamiento (cooldown) por ataque
//  - cuánto se ha usado cada ataque últimamente (los repetidos pesan menos)
//  - un bonus a los ataques que llevan mucho tiempo sin salir
//  - patrones: si la pareja "ataque anterior -> este ataque" ya ha salido hace poco, pesa menos
//  - si el jugador está en el aire (el barrido o la carga se esquivan saltando, la ráfaga no)
//
// En las fases 2 y 3 además decide si se teletransporta (reposición) antes de atacar.
//
// TODOS los números de ajuste (alcances, pesos, cooldowns) están en la tabla del constructor.
public class BossBrain
{
    public class AttackInfo
    {
        public string name;
        public Type type;
        public Func<BossState> create;
        public int minPhase = 1;               // Fase desde la que se usa
        public int maxPhase = 99;              // Fase hasta la que se usa
        public float minRange = 0f;            // Distancia mínima al jugador a la que tiene sentido
        public float maxRange = 999f;          // Alcance real: distancia máxima a la que puede conectar
        public float weight = 1f;              // Preferencia base
        public float cooldown = 3f;            // Segundos antes de poder repetirlo
        public float airborneMultiplier = 1f;  // Multiplicador si el jugador está en el aire
        public Func<bool> isAvailable;         // ¿Están asignadas las hitboxes que necesita?
        public float lastUsedTime = -999f;
    }

    private const int HistorySize = 8;         // Ataques recordados
    private const int RecentWindow = 6;        // Cuántos de ellos cuentan para el "uso reciente"
    private const float FarDistance = 7f;      // A partir de aquí el boss tiene más ganas de teletransportarse

    private readonly BossController boss;
    private readonly List<AttackInfo> catalog;
    private readonly List<Type> history = new List<Type>();
    private readonly Queue<bool> recentDecisions = new Queue<bool>();   // true = teletransporte de reposición
    private bool lastWasReposition;

    public BossBrain(BossController boss)
    {
        this.boss = boss;

        // El ORDEN importa: los índices 0-14 son los de las teclas de depuración del BossController.
        // maxRange tiene en cuenta cuánto avanza el boss durante el ataque + el alcance de su hitbox
        // (son estimaciones: ajústalas con lo que veas al jugar).
        catalog = new List<AttackInfo>
        {
            // ---------- FASE 1 ----------
            new AttackInfo { name = "Estocada", type = typeof(BossStabState),
                create = () => new BossStabState(boss), minPhase = 1, maxPhase = 1,
                maxRange = 5.5f, cooldown = 2.5f, weight = 1f,
                isAvailable = () => boss.stabHitbox != null },

            new AttackInfo { name = "Tajo ascendente", type = typeof(BossUpSlashState),
                create = () => new BossUpSlashState(boss), minPhase = 1, maxPhase = 1,
                maxRange = 3.5f, cooldown = 2f, weight = 1f,
                isAvailable = () => boss.slashHitbox != null },

            new AttackInfo { name = "Combo de 3 golpes", type = typeof(BossComboState),
                create = () => new BossComboState(boss), minPhase = 1, maxPhase = 1,
                maxRange = 4f, cooldown = 4f, weight = 1f,
                isAvailable = () => boss.slashHitbox != null },

            new AttackInfo { name = "Aplastamiento", type = typeof(BossSlamState),
                create = () => new BossSlamState(boss), minPhase = 1, maxPhase = 1,
                maxRange = 5.5f, cooldown = 6f, weight = 0.8f,
                isAvailable = () => boss.slamHitbox != null },

            // ---------- FASE 2 (y 3, hasta que tenga los suyos) ----------
            new AttackInfo { name = "Combo con retraso", type = typeof(BossDelayedComboState),
                create = () => new BossDelayedComboState(boss), minPhase = 2,
                maxRange = 4.5f, cooldown = 4f, weight = 1f,
                isAvailable = () => boss.slashHitbox != null },

            new AttackInfo { name = "Teletransporte con tajo", type = typeof(BossTeleportStrikeState),
                create = () => new BossTeleportStrikeState(boss), minPhase = 2,
                maxRange = 999f, cooldown = 5f, weight = 0.9f,           // A cualquier distancia
                isAvailable = () => boss.slashHitbox != null },

            new AttackInfo { name = "Barrido", type = typeof(BossSweepState),
                create = () => new BossSweepState(boss), minPhase = 2,
                maxRange = 5.5f, cooldown = 3.5f, weight = 1f, airborneMultiplier = 0.25f,
                isAvailable = () => boss.sweepHitbox != null },

            new AttackInfo { name = "Agarre", type = typeof(BossGrabState),
                create = () => new BossGrabState(boss), minPhase = 2,
                maxRange = 3.2f, cooldown = 7f, weight = 0.8f, airborneMultiplier = 0.5f,
                isAvailable = () => boss.grabHitbox != null },

            new AttackInfo { name = "Ráfaga de alas", type = typeof(BossWindBurstState),
                create = () => new BossWindBurstState(boss), minPhase = 2,
                maxRange = 3.5f, cooldown = 6f, weight = 0.8f, airborneMultiplier = 1.5f,
                isAvailable = () => boss.windBurstHitbox != null },

            new AttackInfo { name = "Lanza", type = typeof(BossSpearThrowState),
                create = () => new BossSpearThrowState(boss), minPhase = 2,
                minRange = 3f, maxRange = 999f, cooldown = 5f, weight = 1f,
                isAvailable = () => boss.slashHitbox != null },          // slashHitbox solo da la capa del jugador

            new AttackInfo { name = "Carga de hombro", type = typeof(BossShoulderChargeState),
                create = () => new BossShoulderChargeState(boss), minPhase = 2,
                minRange = 3.5f, maxRange = 999f, cooldown = 6f, weight = 1f, airborneMultiplier = 0.5f,
                isAvailable = () => boss.chargeHitbox != null },

            // ---------- FASE 3 (se suman a los de la fase 2) ----------
            new AttackInfo { name = "Salto con persecución", type = typeof(BossChaseJumpState),
                create = () => new BossChaseJumpState(boss), minPhase = 3,
                maxRange = 999f, cooldown = 6f, weight = 1.2f,
                isAvailable = () => boss.slamHitbox != null },

            new AttackInfo { name = "Pilares de luz", type = typeof(BossLightPillarsState),
                create = () => new BossLightPillarsState(boss), minPhase = 3,
                maxRange = 999f, cooldown = 8f, weight = 1.1f,
                isAvailable = () => boss.slashHitbox != null },          // slashHitbox solo da la capa del jugador

            new AttackInfo { name = "Doble teletransporte", type = typeof(BossDoubleTeleportState),
                create = () => new BossDoubleTeleportState(boss), minPhase = 3,
                maxRange = 999f, cooldown = 6f, weight = 1.2f,
                isAvailable = () => boss.slashHitbox != null },

            new AttackInfo { name = "Multitajo frenético", type = typeof(BossFrenzyState),
                create = () => new BossFrenzyState(boss), minPhase = 3,
                maxRange = 6.5f, cooldown = 5f, weight = 1.3f,
                isAvailable = () => boss.slashHitbox != null },
        };
    }

    // ---------- Decisiones ----------

    // Fases 2 y 3: ¿se teletransporta para recolocarse (y atacar justo después) o ataca directamente?
    public BossState DecideAction()
    {
        float dist = boss.DistanceToPlayer();
        int attacksInRange = Candidates(dist, true, true, false).Count;

        // Tras recolocarse y atacar, la siguiente vez ataca directamente (ritmo menos predecible)
        if (!lastWasReposition)
        {
            float chance = boss.CurrentPhase >= 3 ? 0.35f : 0.25f;
            if (dist > FarDistance) chance += 0.3f;
            chance *= 1f - 0.3f * RecentRepositions();
            if (attacksInRange == 0) chance = 1f;    // Nada llega desde aquí: se recoloca seguro

            if (Random.value < chance)
            {
                lastWasReposition = true;
                RecordDecision(true);
                Debug.Log($"Boss decide: teletransporte de reposición (jugador a {dist:F1})");
                return new BossRepositionState(boss);
            }
        }

        lastWasReposition = false;
        RecordDecision(false);
        return PickAttack() ?? new BossIdleState(boss);
    }

    // Elige un ataque para la distancia actual. Devuelve null si no hay ninguno disponible.
    // afterReposition: se acaba de teletransportar, así que no repite el teletransporte con tajo.
    public BossState PickAttack(bool afterReposition = false)
    {
        float dist = boss.DistanceToPlayer();
        bool airborne = boss.PlayerIsAirborne();

        // Se relajan las condiciones poco a poco para no quedarse nunca sin ataque
        List<AttackInfo> pool = Candidates(dist, true, true, afterReposition);
        if (pool.Count == 0) pool = Candidates(dist, true, false, afterReposition);   // sin cooldown
        if (pool.Count == 0) pool = Candidates(dist, false, false, afterReposition);  // sin límite de alcance
        if (pool.Count == 0) return null;

        AttackInfo chosen = WeightedPick(pool, airborne);
        Register(chosen);
        Debug.Log($"Boss elige ataque: {chosen.name} (jugador a {dist:F1}{(airborne ? ", en el aire" : "")})");
        return chosen.create();
    }

    // Fuerza el ataque número 'index' del catálogo (teclas de depuración)
    public BossState Force(int index)
    {
        if (index < 0 || index >= catalog.Count) return null;

        AttackInfo attack = catalog[index];
        if (!attack.isAvailable())
        {
            Debug.LogWarning($"No se puede lanzar '{attack.name}': falta asignar su hitbox en el BossController.");
            return null;
        }

        Register(attack);
        Debug.Log($"Boss ataque forzado: {attack.name}");
        return attack.create();
    }

    // Avisa en la consola de los ataques que no se usarán porque falta asignar su hitbox
    public void LogUnavailableAttacks()
    {
        foreach (AttackInfo a in catalog)
            if (!a.isAvailable())
                Debug.LogWarning($"BossBrain: '{a.name}' no se usará: falta asignar su hitbox en el BossController.");
    }

    // Distancia aleatoria a la que aparece tras un teletransporte de reposición
    public float RandomRepositionOffset()
    {
        return Random.Range(boss.repositionMinOffset, boss.repositionMaxOffset);
    }

    // ---------- Internos ----------

    private List<AttackInfo> Candidates(float dist, bool respectRange, bool respectCooldown, bool excludeTeleportStrike)
    {
        List<AttackInfo> list = new List<AttackInfo>();
        int phase = boss.CurrentPhase;

        foreach (AttackInfo a in catalog)
        {
            if (phase < a.minPhase || phase > a.maxPhase) continue;
            if (!a.isAvailable()) continue;
            if (excludeTeleportStrike && (a.type == typeof(BossTeleportStrikeState) || a.type == typeof(BossDoubleTeleportState))) continue;
            if (respectRange && (dist < a.minRange || dist > a.maxRange)) continue;
            if (respectCooldown && Time.time - a.lastUsedTime < a.cooldown) continue;
            list.Add(a);
        }
        return list;
    }

    private AttackInfo WeightedPick(List<AttackInfo> pool, bool airborne)
    {
        // Nunca el mismo ataque dos veces seguidas (si hay alternativa)
        if (pool.Count > 1 && history.Count > 0)
        {
            Type last = history[history.Count - 1];
            List<AttackInfo> withoutRepeat = pool.FindAll(a => a.type != last);
            if (withoutRepeat.Count > 0) pool = withoutRepeat;
        }

        float total = 0f;
        float[] weights = new float[pool.Count];
        for (int i = 0; i < pool.Count; i++)
        {
            weights[i] = Weight(pool[i], airborne);
            total += weights[i];
        }

        float roll = Random.value * total;
        for (int i = 0; i < pool.Count; i++)
        {
            roll -= weights[i];
            if (roll <= 0f) return pool[i];
        }
        return pool[pool.Count - 1];
    }

    private float Weight(AttackInfo a, bool airborne)
    {
        // Cuántas veces ha salido en los últimos ataques: cuantas más, menos probable
        int recent = 0;
        int start = Mathf.Max(0, history.Count - RecentWindow);
        for (int i = start; i < history.Count; i++)
            if (history[i] == a.type) recent++;
        float usage = 1f / (1f + 1.5f * recent);

        // Bonus (hasta x2) a los que llevan mucho sin salir (los que nunca han salido, el máximo)
        float stale = 1f + Mathf.Clamp01((Time.time - a.lastUsedTime) / 15f);

        // Si la secuencia "anterior -> este" ya ha pasado hace poco, penalización
        float pattern = PairAlreadySeen(a.type) ? 0.4f : 1f;

        float context = airborne ? a.airborneMultiplier : 1f;

        return Mathf.Max(0.01f, a.weight * usage * stale * pattern * context);
    }

    private bool PairAlreadySeen(Type next)
    {
        if (history.Count < 2) return false;

        Type last = history[history.Count - 1];
        for (int i = 0; i < history.Count - 1; i++)
            if (history[i] == last && history[i + 1] == next) return true;
        return false;
    }

    private void Register(AttackInfo a)
    {
        a.lastUsedTime = Time.time;
        history.Add(a.type);
        if (history.Count > HistorySize) history.RemoveAt(0);
    }

    private void RecordDecision(bool repositioned)
    {
        recentDecisions.Enqueue(repositioned);
        if (recentDecisions.Count > 4) recentDecisions.Dequeue();
    }

    private int RecentRepositions()
    {
        int n = 0;
        foreach (bool b in recentDecisions)
            if (b) n++;
        return n;
    }
}
