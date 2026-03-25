using UnityEngine;

namespace FarmMergerBattle.Data
{
    [CreateAssetMenu(menuName = "FarmMergerBattle/Game Config")]
    public class GameConfig : ScriptableObject
    {
        public HeroConfig hero;
        public WaveConfig waveConfig;
        public SlotMachineConfig slotMachine;
        public Sprite wallVisual;
        public int startingCoins = 10;
        public float wallMaxHp = 50f;
        public float heroX = 2f;
        public float wallX = 0f;
        public float enemySpawnX = 10f;
    }
}
