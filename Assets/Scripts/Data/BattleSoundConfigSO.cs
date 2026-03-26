using UnityEngine;

[CreateAssetMenu(fileName = "BattleSoundConfig", menuName = "FarmMerger/Data/Battle Sound Config")]
public class BattleSoundConfigSO : ScriptableObject
{
    [Header("Background Music")]
    public AudioClip backgroundMusic;
    [Range(0f, 1f)] public float backgroundMusicVolume = 0.2f;

    [Header("Gameplay SFX")]
    public AudioClip heroSpawn;
    [Range(0f, 1f)] public float heroSpawnVolume = 0.8f;

    public AudioClip heroMerge;
    [Range(0f, 1f)] public float heroMergeVolume = 0.85f;

    public AudioClip pullButton;
    [Range(0f, 1f)] public float pullButtonVolume = 0.75f;
}
