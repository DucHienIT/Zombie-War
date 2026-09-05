using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using ZombieWar.Data;
using ZombieWar.EditorTools.UI;

namespace ZombieWar.EditorTools.Roguelike
{
    // Authors the starting passive pool and the run settings that drive it. This is the
    // authoring step, so the numbers live here once; afterwards the .asset files own them
    // and the tool leaves anything a designer has already tuned alone.
    public static class RoguelikeSkillDataBuilder
    {
        private const string LogPrefix = "[Rogue Build]";
        private const string Folder = "Assets/_ZombieWar/Data/Roguelike/";
        private const string SettingsPath = Folder + "RoguelikeSettings.asset";

        private static readonly string[] IconSprites =
        {
            "Pictoicon_Attack",
            "Pictoicon_Thunder",
            "Pictoicon_Missile",
            "Pictoicon_Boot_Fly",
            "Pictoicon_Life_Add",
            "Pictoicon_Boom",
        };

        [MenuItem("Tools/Zombie War/Roguelike/1. Create Skill Data", false, 200)]
        public static void CreateSkillData()
        {
            LayerLabUiImporter.ImportSprites(IconSprites, UiSkin.IconFolder);
            EnsureFolder(Folder);

            SkillRecipe[] recipes = BuildRecipes();
            var pool = new List<Object>(recipes.Length);
            int created = 0;
            int kept = 0;
            for (int i = 0; i < recipes.Length; i++)
            {
                SkillRecipe recipe = recipes[i];
                string path = Folder + "Skill_" + recipe.AssetName + ".asset";
                var existing = AssetDatabase.LoadAssetAtPath<PassiveSkillSO>(path);
                if (existing != null)
                {
                    pool.Add(existing);
                    kept++;
                    continue;
                }

                var skill = ScriptableObject.CreateInstance<PassiveSkillSO>();
                AssetDatabase.CreateAsset(skill, path);
                WriteSkill(skill, recipe);
                pool.Add(skill);
                created++;
            }

            WriteSettings(pool);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"{LogPrefix} created {created} skills, left {kept} tuned ones alone. Pool wired into {SettingsPath}.");
        }

        // The whole design of the six starters. Every one of them is nothing but stat
        // modifiers, which is what makes skill number seven a new asset instead of new code.
        private static SkillRecipe[] BuildRecipes()
        {
            return new[]
            {
                new SkillRecipe
                {
                    AssetName = "HighCaliber",
                    Id = "high_caliber",
                    DisplayName = "HIGH CALIBER",
                    Description = "Damage +20%\nKnockback +15%",
                    IconName = "Pictoicon_Attack",
                    Accent = new Color32(0xFF, 0x9B, 0x3D, 0xFF),
                    MaxStacks = 5,
                    Modifiers = new[]
                    {
                        new StatEntry(StatId.WeaponDamage, StatModifierKind.Multiplicative, 1.2f),
                        new StatEntry(StatId.Knockback, StatModifierKind.Multiplicative, 1.15f),
                    },
                },
                new SkillRecipe
                {
                    AssetName = "RapidFire",
                    Id = "rapid_fire",
                    DisplayName = "RAPID FIRE",
                    Description = "Fire rate +15%\nReload 12% faster",
                    IconName = "Pictoicon_Thunder",
                    Accent = new Color32(0xFF, 0xD5, 0x4A, 0xFF),
                    MaxStacks = 5,
                    Modifiers = new[]
                    {
                        new StatEntry(StatId.FireRate, StatModifierKind.Multiplicative, 1.15f),
                        new StatEntry(StatId.ReloadSpeed, StatModifierKind.Multiplicative, 1.135f),
                    },
                },
                new SkillRecipe
                {
                    AssetName = "PiercingRounds",
                    Id = "piercing_rounds",
                    DisplayName = "PIERCING ROUNDS",
                    Description = "Bullets pierce\n1 more zombie",
                    IconName = "Pictoicon_Missile",
                    Accent = new Color32(0x4E, 0xCD, 0xE0, 0xFF),
                    MaxStacks = 3,
                    Modifiers = new[]
                    {
                        new StatEntry(StatId.ProjectilePierce, StatModifierKind.Additive, 1f),
                    },
                },
                new SkillRecipe
                {
                    AssetName = "Adrenaline",
                    Id = "adrenaline",
                    DisplayName = "ADRENALINE",
                    Description = "Move speed +10%\nExperience +8%",
                    IconName = "Pictoicon_Boot_Fly",
                    Accent = new Color32(0x5C, 0xE0, 0x8B, 0xFF),
                    MaxStacks = 4,
                    Modifiers = new[]
                    {
                        new StatEntry(StatId.MoveSpeed, StatModifierKind.Multiplicative, 1.1f),
                        new StatEntry(StatId.XpGain, StatModifierKind.Multiplicative, 1.08f),
                    },
                },
                new SkillRecipe
                {
                    AssetName = "BloodPact",
                    Id = "blood_pact",
                    DisplayName = "BLOOD PACT",
                    Description = "Max health +15\nHeal 2 per kill",
                    IconName = "Pictoicon_Life_Add",
                    Accent = new Color32(0xFF, 0x5B, 0x4A, 0xFF),
                    MaxStacks = 4,
                    Modifiers = new[]
                    {
                        new StatEntry(StatId.MaxHealth, StatModifierKind.Additive, 15f),
                        new StatEntry(StatId.HealPerKill, StatModifierKind.Additive, 2f),
                    },
                },
                new SkillRecipe
                {
                    AssetName = "Demolitionist",
                    Id = "demolitionist",
                    DisplayName = "DEMOLITIONIST",
                    Description = "Bombs +1\nBlast damage +20%\nBlast radius +12%",
                    IconName = "Pictoicon_Boom",
                    Accent = new Color32(0xB4, 0x7C, 0xFF, 0xFF),
                    MaxStacks = 3,
                    Modifiers = new[]
                    {
                        new StatEntry(StatId.BombCharges, StatModifierKind.Additive, 1f),
                        new StatEntry(StatId.BombRadius, StatModifierKind.Multiplicative, 1.12f),
                        new StatEntry(StatId.BombDamage, StatModifierKind.Multiplicative, 1.2f),
                    },
                },
            };
        }

        private static void WriteSkill(PassiveSkillSO skill, SkillRecipe recipe)
        {
            var serialized = new SerializedObject(skill);
            serialized.FindProperty("_id").stringValue = recipe.Id;
            serialized.FindProperty("_displayName").stringValue = recipe.DisplayName;
            serialized.FindProperty("_description").stringValue = recipe.Description;
            serialized.FindProperty("_icon").objectReferenceValue = UiSkin.Icon(recipe.IconName);
            serialized.FindProperty("_accentColor").colorValue = recipe.Accent;
            serialized.FindProperty("_maxStacks").intValue = recipe.MaxStacks;
            serialized.FindProperty("_draftWeight").floatValue = 1f;

            SerializedProperty modifiers = serialized.FindProperty("_modifiers");
            modifiers.arraySize = recipe.Modifiers.Length;
            for (int i = 0; i < recipe.Modifiers.Length; i++)
            {
                StatEntry entry = recipe.Modifiers[i];
                SerializedProperty element = modifiers.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("_stat").enumValueIndex = (int)entry.Stat;
                element.FindPropertyRelative("_kind").enumValueIndex = (int)entry.Kind;
                element.FindPropertyRelative("_valuePerStack").floatValue = entry.Value;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // The pool is re-pointed even on an existing asset, so adding a seventh skill means
        // running this again rather than dragging references by hand.
        private static void WriteSettings(List<Object> pool)
        {
            var settings = AssetDatabase.LoadAssetAtPath<RoguelikeSettingsSO>(SettingsPath);
            bool isNew = settings == null;
            if (isNew)
            {
                settings = ScriptableObject.CreateInstance<RoguelikeSettingsSO>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }

            var serialized = new SerializedObject(settings);
            SerializedProperty skills = serialized.FindProperty("_skillPool");
            skills.arraySize = pool.Count;
            for (int i = 0; i < pool.Count; i++)
            {
                skills.GetArrayElementAtIndex(i).objectReferenceValue = pool[i];
            }

            if (isNew)
            {
                serialized.FindProperty("_offersPerLevelUp").intValue = 3;
                serialized.FindProperty("_baseXpToLevel").floatValue = 80f;
                serialized.FindProperty("_xpGrowthPerLevel").floatValue = 1.35f;
                serialized.FindProperty("_maxBattleLevel").intValue = 12;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string folder)
        {
            string trimmed = folder.TrimEnd('/');
            if (AssetDatabase.IsValidFolder(trimmed))
            {
                return;
            }

            string parent = Path.GetDirectoryName(trimmed).Replace('\\', '/');
            EnsureFolder(parent + "/");
            AssetDatabase.CreateFolder(parent, Path.GetFileName(trimmed));
        }

        private readonly struct StatEntry
        {
            public readonly StatId Stat;
            public readonly StatModifierKind Kind;
            public readonly float Value;

            public StatEntry(StatId stat, StatModifierKind kind, float value)
            {
                Stat = stat;
                Kind = kind;
                Value = value;
            }
        }

        private sealed class SkillRecipe
        {
            public string AssetName;
            public string Id;
            public string DisplayName;
            public string Description;
            public string IconName;
            public Color Accent;
            public int MaxStacks;
            public StatEntry[] Modifiers;
        }
    }
}
