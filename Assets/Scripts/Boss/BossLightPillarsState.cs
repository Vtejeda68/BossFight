using UnityEngine;

// Pilares de luz (fase 3): tres pilares caen sobre donde VA a estar el jugador (usa su velocidad),
// y una fila de pilares cubre toda la arena dejando huecos estrechos entre ellos.
// Usa solo la capa del jugador de Hitbox_Slash; los pilares se crean por código.
public class BossLightPillarsState : BossSequenceState
{
    private const float Width = 1.6f;
    private const float Height = 14f;
    private const float ActiveTime = 0.3f;
    private const float TrackWarn = 0.75f;
    private const float RowWarn = 0.95f;
    private const float RowSpacing = 3.4f;
    private const float LeadTime = 0.35f;      // Cuánto se adelanta a la velocidad del jugador
    private const int Damage = 2;

    public BossLightPillarsState(BossController boss) : base(boss) { }

    protected override void BuildSteps()
    {
        AddWindup(0.4f);

        for (int i = 0; i < 3; i++)
            AddStep(0.4f, onEnter: () =>
            {
                float x = boss.player.position.x + boss.PlayerVelocityX * LeadTime;
                SpawnPillar(x, TrackWarn);
            });

        AddStep(0.5f, onEnter: () =>
        {
            boss.GetArenaBounds(out float minX, out float maxX);
            float x = minX + Random.Range(0f, RowSpacing);
            for (; x <= maxX; x += RowSpacing)
                SpawnPillar(x, RowWarn);
        });

        AddRecovery(0.6f);
    }

    private void SpawnPillar(float x, float warn)
    {
        BossPillar.Spawn(new Vector2(x, boss.GroundY), Width, Height, warn, ActiveTime, Damage, boss.PlayerLayers);
    }
}
