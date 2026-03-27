using UnityEngine;

[CreateAssetMenu(fileName = "MainMenuSoundConfig", menuName = "FarmMerger/Data/Main Menu Sound Config")]
public class MainMenuSoundConfigSO : ScriptableObject
{
    [Header("Background Music")]
    public AudioClip backgroundMusic;
    [Range(0f, 1f)] public float backgroundMusicVolume = 0.2f;

    [Header("UI SFX")]
    public AudioClip playButtonClick;
    [Range(0f, 1f)] public float playButtonClickVolume = 0.75f;
}
