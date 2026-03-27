using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ChapterPresentationConfig", menuName = "FarmMerger/Data/Chapter Presentation Config")]
public class ChapterPresentationConfigSO : ScriptableObject
{
    public List<ChapterPresentationEntry> chapters = new List<ChapterPresentationEntry>
    {
        new ChapterPresentationEntry { chapterNumber = 1, levelDisplayName = "Land of wild sand" }
    };

    public ChapterPresentationEntry GetChapterByNumber(int chapterNumber)
    {
        for (int i = 0; i < chapters.Count; i++)
        {
            if (chapters[i].chapterNumber == chapterNumber)
            {
                return chapters[i];
            }
        }

        return chapters.Count > 0 ? chapters[0] : new ChapterPresentationEntry();
    }
}

[Serializable]
public class ChapterPresentationEntry
{
    public int chapterNumber = 1;
    public string levelDisplayName = "Land of wild sand";
}
