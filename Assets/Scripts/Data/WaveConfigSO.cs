using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WaveConfig", menuName = "FarmMerger/Data/Wave Config")]
public class WaveConfigSO : ScriptableObject
{
    public float waveIntervalSec = 7f;
    public List<WaveEntry> waves = new List<WaveEntry>
    {
        new WaveEntry { waveIndex = 1, enemyId = "enemy_basic", enemyCount = 8 },
        new WaveEntry { waveIndex = 2, enemyId = "enemy_basic", enemyCount = 12 },
        new WaveEntry { waveIndex = 3, enemyId = "enemy_basic", enemyCount = 16 }
    };
}

[Serializable]
public class WaveEntry
{
    public int waveIndex = 1;
    public string enemyId = "enemy_basic";
    public int enemyCount = 8;
}
