using UnityEngine;

namespace ZombieWar.UI
{
    // What the chapter card draws. The card never sees the LevelDefinitionSO behind it.
    public readonly struct LevelCardData
    {
        public readonly int Index;
        public readonly string DisplayName;
        // Null keeps the placeholder picture authored in the prefab.
        public readonly Sprite Artwork;
        public readonly bool Unlocked;
        public readonly int BestScore;

        public LevelCardData(int index, string displayName, Sprite artwork, bool unlocked, int bestScore)
        {
            Index = index;
            DisplayName = displayName;
            Artwork = artwork;
            Unlocked = unlocked;
            BestScore = bestScore;
        }
    }
}
