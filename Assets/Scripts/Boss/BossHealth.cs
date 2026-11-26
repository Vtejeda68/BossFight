using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Vida del boss y control de las tres fases.
// Fase 1: 100%-66% | Fase 2: 66%-33% | Fase 3: 33%-0% (recibe solo el 25% del daño).
public class BossHealth : MonoBehaviour
{
    [Header("Vida")]
    public float maxHealth = 300f;

    [Header("Fases")]
    public float phase2Threshold = 0.66f;   // Porcentaje de vida al que empieza la fase 2
    public float phase3Threshold = 0.33f;   // Porcentaje de vida al que empieza la fase 3
    public float phase3DamageMultiplier = 0.25f;  // 75% menos de daño en la fase final

    [Header("Depuración")]
    public bool debugKeys = true;           // K = quitarle el 10% de vida al boss
    public float debugDamagePercent = 0.10f;

    public float CurrentHealth { get; private set; }
    public int CurrentPhase { get; private set; } = 1;
    public bool IsDead => CurrentHealth <= 0f;

    // Otros sistemas (barra de vida, selector de ataques, efectos) pueden suscribirse a esto
    public event Action<float, float> OnHealthChanged;   // (vida actual, vida máxima)
    public event Action<int> OnPhaseChanged;
    public event Action OnDeath;

    void Awake()
    {
        CurrentHealth = maxHealth;
    }

    void Update()
    {
        if (!debugKeys || Keyboard.current == null) return;

        if (Keyboard.current.kKey.wasPressedThisFrame)
            TakeDamage(maxHealth * debugDamagePercent);
    }

    // Esto es lo que llamará el ataque del jugador (el tuyo de pruebas o el de tus compañeros)
    public void TakeDamage(float amount)
    {
        if (IsDead) return;

        if (CurrentPhase == 3)
            amount *= phase3DamageMultiplier;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        Debug.Log($"Boss vida: {CurrentHealth:F0}/{maxHealth:F0} (fase {CurrentPhase})");
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

        if (IsDead)
        {
            Debug.Log("Boss derrotado");
            OnDeath?.Invoke();
            return;
        }

        UpdatePhase();
    }

    private void UpdatePhase()
    {
        float ratio = CurrentHealth / maxHealth;
        int newPhase = ratio <= phase3Threshold ? 3 : (ratio <= phase2Threshold ? 2 : 1);

        if (newPhase > CurrentPhase)
        {
            CurrentPhase = newPhase;
            Debug.Log($"Boss entra en la fase {CurrentPhase}");
            OnPhaseChanged?.Invoke(CurrentPhase);
        }
    }
}
