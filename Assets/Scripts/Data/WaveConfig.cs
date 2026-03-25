using System;
using System.Collections.Generic;
using UnityEngine;

namespace FarmMergerBattle.Data
{
    [CreateAssetMenu(menuName = "FarmMergerBattle/Wave Config")]
    public class WaveConfig : ScriptableObject
    {
        [Serializable]
        public class WaveEntry
        {
            public EnemyConfig enemy;
            public int count = 5;
            public float spawnInterval = 1f;
        }

        public List<WaveEntry> waves = new List<WaveEntry>();
    }
}
