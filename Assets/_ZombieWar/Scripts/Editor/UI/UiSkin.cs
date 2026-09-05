using TMPro;
using UnityEditor;
using UnityEngine;

namespace ZombieWar.EditorTools.UI
{
    // The single place the UI builders read art and palette from. Values live here because
    // this assembly is the authoring step; once a prefab is written, the prefab owns them.
    public static class UiSkin
    {
        public const string Root = "Assets/_ZombieWar/Art/UI/";
        public const string FontFolder = Root + "Fonts/";
        public const string SpriteFolder = Root + "Sprites/";
        public const string ButtonFolder = SpriteFolder + "Buttons/";
        public const string FrameFolder = SpriteFolder + "Frames/";
        public const string PopupFolder = SpriteFolder + "Popups/";
        public const string SliderFolder = SpriteFolder + "Sliders/";
        public const string LabelFolder = SpriteFolder + "Labels/";
        public const string IconFolder = SpriteFolder + "Icons/";
        public const string ToggleFolder = SpriteFolder + "Toggles/";

        public static readonly string[] FontAssets =
        {
            "LilitaOne-Regular SDF",
            "LilitaOne-Regular Outline 54 SDF",
        };

        public static readonly string[] ButtonSprites =
        {
            "Btn_MainButton_Green",
            "Btn_MainButton_Red",
            "Btn_MainButton_Blue",
            "Btn_OtherButton_Circle01_n",
            "Btn_OtherButton_Circle02",
        };

        public static readonly string[] FrameSprites =
        {
            "Frame_BarFrame_Top01_Navy",
            "Frame_BasicFrame_Square05",
            "Frame_ItemFrame03_Navy",
            "Frame_StageFrame_n_Blue",
        };

        public static readonly string[] PopupSprites =
        {
            "Popup_Frame01_Navy",
            "Popup_00_TitleLIne",
            "Popup_00_Glow_white",
        };

        public static readonly string[] SliderSprites =
        {
            "Slider10_Frame",
            "Slider10_Fill_Red",
            "Slider10_Fill_Green",
        };

        public static readonly string[] LabelSprites =
        {
            "Label_TitleFlag01_Red",
            "Label_TitleFlag01_Green",
            "Label_TitleFlag01_Blue",
            "Label_TitleRibbon_Yellow",
        };

        public static readonly string[] IconSprites =
        {
            "Pictoicon_Control_Pause",
            "Pictoicon_Control_Play",
            "Pictoicon_Home_0",
            "Pictoicon_Refresh",
            "Pictoicon_Arrow_Next",
            "Pictoicon_Skull",
            "Pictoicon_Star",
            "Pictoicon_Timer",
            "Pictoicon_Lock",
            "Pictoicon_Switch",
            "Pictoicon_Health",
            "Pictoicon_Trophy_0",
            "Pictoicon_Reload",
            "Pictoicon_Damage",
            "Pictoicon_Gun",
            "Icon_Bomb_Bomb",
        };

        public static readonly string[] ToggleSprites =
        {
            "Toggle_Switch_On_Frame",
            "Toggle_Switch_On_Handle",
            "Toggle_Switch_Off_Frame",
            "Toggle_Switch_Off_Handle",
        };

        public static readonly Color Paper = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
        public static readonly Color Ink = new Color32(0x1B, 0x26, 0x33, 0xFF);
        public static readonly Color HudLabel = new Color32(0xC2, 0xCE, 0xDE, 0xFF);
        public static readonly Color Danger = new Color32(0xFF, 0x5B, 0x4A, 0xFF);
        public static readonly Color Warning = new Color32(0xFF, 0xC5, 0x3D, 0xFF);
        public static readonly Color Good = new Color32(0x5C, 0xE0, 0x8B, 0xFF);
        public static readonly Color IconOnDark = new Color32(0xE8, 0xEF, 0xF8, 0xFF);
        public static readonly Color IconMuted = new Color32(0x6E, 0x7A, 0x8C, 0xFF);
        public static readonly Color Backdrop = new Color32(0x06, 0x09, 0x0F, 0xC7);
        public static readonly Color Invisible = new Color(1f, 1f, 1f, 0f);
        public static readonly Color JoystickBase = new Color(1f, 1f, 1f, 0.22f);
        public static readonly Color JoystickHandle = new Color(1f, 1f, 1f, 0.62f);

        public static Sprite Button(string name) => Load<Sprite>(ButtonFolder + name + ".png");
        public static Sprite Frame(string name) => Load<Sprite>(FrameFolder + name + ".png");
        public static Sprite Popup(string name) => Load<Sprite>(PopupFolder + name + ".png");
        public static Sprite Slider(string name) => Load<Sprite>(SliderFolder + name + ".png");
        public static Sprite Label(string name) => Load<Sprite>(LabelFolder + name + ".png");
        public static Sprite Icon(string name) => Load<Sprite>(IconFolder + name + ".png");
        public static Sprite Toggle(string name) => Load<Sprite>(ToggleFolder + name + ".png");

        public static TMP_FontAsset BodyFont => Load<TMP_FontAsset>(FontFolder + FontAssets[0] + ".asset");
        public static TMP_FontAsset TitleFont => Load<TMP_FontAsset>(FontFolder + FontAssets[1] + ".asset");

        private static T Load<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                Debug.LogError($"[UI Skin] {path} is missing - run Tools/Zombie War/UI/1. Import Layer Lab UI Assets first.");
            }

            return asset;
        }
    }
}
