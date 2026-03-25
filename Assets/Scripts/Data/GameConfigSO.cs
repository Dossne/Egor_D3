using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "FarmMerger/Data/Game Config")]
public class GameConfigSO : ScriptableObject
{
    public int startingCoins = 60;
    public float wallMaxHp = 150f;
}
