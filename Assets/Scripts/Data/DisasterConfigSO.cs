using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DisasterConfig", menuName = "FarmMerger/Data/Disaster Config")]
public class DisasterConfigSO : ScriptableObject
{
    [Header("Trigger")]
    public List<int> disasterWaveIndices = new List<int> { 3 };

    [Header("Symbols")]
    public Sprite skullSymbol;
    public Sprite cloverSymbol;

    [Header("Presentation")]
    public Color overlayColor = new Color(0f, 0f, 0f, 0.72f);
    public int spinDurationMs = 1000;
    public float spinStepSec = 0.1f;
    public float postSpinDelaySec = 0.15f;
    public float payoffBounceDurationSec = 0.45f;
    public float payoffFlashDurationSec = 0.2f;
    public float payoffHoldDurationSec = 0.2f;

    [Header("Enemy Buffs (Skull Outcomes)")]
    public EnemyDisasterBuff twoSkullBuff = new EnemyDisasterBuff { attackSpeedPercent = 15f, moveSpeedPercent = 15f, durationSec = 6f };
    public EnemyDisasterBuff threeSkullBuff = new EnemyDisasterBuff { attackSpeedPercent = 30f, moveSpeedPercent = 25f, durationSec = 8f };

    [Header("Hero Buffs (Clover Outcomes)")]
    public HeroDisasterBuff twoCloverBuff = new HeroDisasterBuff { damagePercent = 20f, attackSpeedPercent = 15f, durationSec = 6f };
    public HeroDisasterBuff threeCloverBuff = new HeroDisasterBuff { damagePercent = 40f, attackSpeedPercent = 30f, durationSec = 8f };

    [Header("Buff VFX")]
    public Color heroBuffVfxColor = new Color(0.35f, 1f, 0.5f, 0.5f);
    public Color enemyBuffVfxColor = new Color(1f, 0.28f, 0.28f, 0.5f);
    public float buffVfxPulseSpeed = 8f;
    public float buffVfxPulseStrength = 0.22f;
}

[Serializable]
public class EnemyDisasterBuff
{
    public float attackSpeedPercent;
    public float moveSpeedPercent;
    public float durationSec = 6f;
}

[Serializable]
public class HeroDisasterBuff
{
    public float damagePercent;
    public float attackSpeedPercent;
    public float durationSec = 6f;
}
