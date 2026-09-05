using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using ZombieWar.UI;

namespace ZombieWar.EditorTools.UI
{
    // Authors the two popup prefabs. They stay free of any reference that points outside
    // themselves, which is what lets them live as prefab assets and be dropped into the
    // popup layer of any scene.
    public static class PopupPrefabBuilder
    {
        public const string Folder = "Assets/_ZombieWar/Prefabs/UI/Popups/";
        public const string PausePath = Folder + "PausePopup.prefab";
        public const string ResultPath = Folder + "ResultPopup.prefab";

        private const float ButtonWidth = 640f;
        private const float ButtonHeight = 145f;
        private const float RowHeight = 92f;
        // The pack's switch handle is drawn larger than its track and overhangs it.
        private const float SwitchHandleInset = 30f;
        private const float SwitchHandleLift = 3f;
        private static readonly Vector2 SwitchSize = new Vector2(136f, 64f);
        private static readonly Vector2 SwitchHandleSize = new Vector2(64f, 77f);

        public static void BuildAll()
        {
            EnsureFolder(Folder);
            BuildPause();
            BuildResult();
        }

        private static void BuildPause()
        {
            RectTransform root = UiBuildUtility.CreateNode("PausePopup", null);
            UiBuildUtility.Stretch(root, 0f, 0f, 0f, 0f);
            var canvasGroup = root.gameObject.AddComponent<CanvasGroup>();
            var popup = root.gameObject.AddComponent<PausePopupUI>();

            RectTransform panel = UiBuildUtility.CreateNode("Panel", root);
            UiBuildUtility.Place(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(880f, 800f));
            UiBuildUtility.AddImage(panel, UiSkin.Popup("Popup_Frame01_Navy"), UiSkin.Paper, Image.Type.Sliced, true);

            CreateTitleFlag(panel, "PAUSED", UiSkin.Label("Label_TitleFlag01_Blue"));

            SwitchToggleView shakeSwitch = CreateSettingRow(panel, "Row_ScreenShake", "SCREEN SHAKE", new Vector2(0f, 190f));

            Button resume = CreateMainButton(panel, "ResumeButton", "Btn_MainButton_Green", "RESUME", "Pictoicon_Control_Play", new Vector2(0f, 40f));
            Button restart = CreateMainButton(panel, "RestartButton", "Btn_MainButton_Blue", "RESTART", "Pictoicon_Refresh", new Vector2(0f, -120f));
            Button menu = CreateMainButton(panel, "MenuButton", "Btn_MainButton_Red", "MAIN MENU", "Pictoicon_Home_0", new Vector2(0f, -280f));

            UiBuildUtility.Bind(popup,
                "_canvasGroup", canvasGroup,
                "_content", panel,
                "_dimBackground", true,
                "_closeOnBackdropClick", false,
                "_closeOnBackKey", true,
                "_fitPadding", 48f,
                "_resumeButton", resume,
                "_restartButton", restart,
                "_menuButton", menu,
                "_shakeSwitch", shakeSwitch);

            Save(root, PausePath);
        }

        // A labelled row with an on/off switch on the right, the same plate as the result stats.
        private static SwitchToggleView CreateSettingRow(RectTransform panel, string name, string label, Vector2 position)
        {
            RectTransform row = UiBuildUtility.CreateNode(name, panel);
            UiBuildUtility.Place(row, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(760f, RowHeight));
            UiBuildUtility.AddImage(row, UiSkin.Frame("Frame_ItemFrame03_Navy"), UiSkin.Paper, Image.Type.Sliced, false);

            RectTransform labelRect = UiBuildUtility.CreateNode("Label", row);
            UiBuildUtility.Place(labelRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(48f, 0f), new Vector2(440f, 60f));
            UiBuildUtility.AddText(labelRect, label, UiSkin.BodyFont, 38f, UiSkin.HudLabel, TextAlignmentOptions.MidlineLeft);

            return CreateSwitch(row, new Vector2(-40f, 0f));
        }

        private static SwitchToggleView CreateSwitch(RectTransform row, Vector2 position)
        {
            RectTransform rect = UiBuildUtility.CreateNode("Switch", row);
            UiBuildUtility.Place(rect, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), position, SwitchSize);

            // An invisible plate takes the tap so either visual can be turned off freely.
            Image hit = UiBuildUtility.AddImage(rect, null, UiSkin.Invisible, Image.Type.Simple, true);
            var toggle = rect.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = hit;
            toggle.transition = Selectable.Transition.None;
            toggle.toggleTransition = Toggle.ToggleTransition.None;
            toggle.graphic = null;
            toggle.isOn = true;

            GameObject offVisual = CreateSwitchState(rect, "Off", "Toggle_Switch_Off_Frame", "Toggle_Switch_Off_Handle", new Vector2(0f, 0.5f), new Vector2(SwitchHandleInset, SwitchHandleLift));
            GameObject onVisual = CreateSwitchState(rect, "On", "Toggle_Switch_On_Frame", "Toggle_Switch_On_Handle", new Vector2(1f, 0.5f), new Vector2(-SwitchHandleInset, SwitchHandleLift));
            offVisual.SetActive(false);

            var view = rect.gameObject.AddComponent<SwitchToggleView>();
            UiBuildUtility.Bind(view,
                "_toggle", toggle,
                "_onVisual", onVisual,
                "_offVisual", offVisual);
            return view;
        }

        private static GameObject CreateSwitchState(RectTransform parent, string name, string frameName, string handleName, Vector2 handleAnchor, Vector2 handleOffset)
        {
            RectTransform state = UiBuildUtility.CreateNode(name, parent);
            UiBuildUtility.Stretch(state, 0f, 0f, 0f, 0f);
            UiBuildUtility.AddImage(state, UiSkin.Toggle(frameName), UiSkin.Paper, Image.Type.Sliced, false);

            RectTransform handle = UiBuildUtility.CreateNode("Handle", state);
            UiBuildUtility.Place(handle, handleAnchor, new Vector2(0.5f, 0.5f), handleOffset, SwitchHandleSize);
            UiBuildUtility.AddImage(handle, UiSkin.Toggle(handleName), UiSkin.Paper, Image.Type.Simple, false);
            return state.gameObject;
        }

        private static void BuildResult()
        {
            RectTransform root = UiBuildUtility.CreateNode("ResultPopup", null);
            UiBuildUtility.Stretch(root, 0f, 0f, 0f, 0f);
            var canvasGroup = root.gameObject.AddComponent<CanvasGroup>();
            var popup = root.gameObject.AddComponent<ResultPopupUI>();

            RectTransform panel = UiBuildUtility.CreateNode("Panel", root);
            UiBuildUtility.Place(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(920f, 1380f));
            UiBuildUtility.AddImage(panel, UiSkin.Popup("Popup_Frame01_Navy"), UiSkin.Paper, Image.Type.Sliced, true);

            RectTransform divider = UiBuildUtility.CreateNode("TitleDivider", panel);
            UiBuildUtility.Place(divider, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -150f), new Vector2(760f, 56f));
            UiBuildUtility.AddImage(divider, UiSkin.Popup("Popup_00_TitleLIne"), UiSkin.Paper, Image.Type.Sliced, false);

            Image banner = CreateTitleFlag(panel, "LEVEL CLEARED", UiSkin.Label("Label_TitleFlag01_Green"));
            TMP_Text titleText = banner.transform.GetChild(0).GetComponent<TMP_Text>();

            RectTransform badge = UiBuildUtility.CreateNode("NewBestBadge", panel);
            UiBuildUtility.Place(badge, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -212f), new Vector2(420f, 110f));
            UiBuildUtility.AddImage(badge, UiSkin.Label("Label_TitleRibbon_Yellow"), UiSkin.Paper, Image.Type.Sliced, false);
            RectTransform badgeText = UiBuildUtility.CreateNode("Text", badge);
            UiBuildUtility.Stretch(badgeText, 0f, 6f, 0f, 0f);
            UiBuildUtility.AddText(badgeText, "NEW BEST", UiSkin.TitleFont, 40f, UiSkin.Ink, TextAlignmentOptions.Center);
            badge.gameObject.SetActive(false);

            TMP_Text kills = CreateStatRow(panel, "Row_Kills", "Pictoicon_Skull", "KILLS", -330f, UiSkin.Paper);
            TMP_Text score = CreateStatRow(panel, "Row_Score", "Pictoicon_Star", "SCORE", -434f, UiSkin.Paper);
            TMP_Text healthBonus = CreateStatRow(panel, "Row_HealthBonus", "Pictoicon_Health", "HEALTH BONUS", -538f, UiSkin.Good);
            TMP_Text damageTaken = CreateStatRow(panel, "Row_DamageTaken", "Pictoicon_Damage", "DAMAGE TAKEN", -642f, UiSkin.Danger);

            RectTransform totalRow = UiBuildUtility.CreateNode("Row_Total", panel);
            UiBuildUtility.Place(totalRow, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -775f), new Vector2(800f, 130f));
            UiBuildUtility.AddImage(totalRow, UiSkin.Frame("Frame_BasicFrame_Square05"), UiSkin.Paper, Image.Type.Sliced, false);
            RectTransform totalLabel = UiBuildUtility.CreateNode("Label", totalRow);
            UiBuildUtility.Place(totalLabel, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(48f, 0f), new Vector2(360f, 70f));
            UiBuildUtility.AddText(totalLabel, "TOTAL", UiSkin.TitleFont, 54f, UiSkin.Paper, TextAlignmentOptions.MidlineLeft);
            RectTransform totalValueRect = UiBuildUtility.CreateNode("Value", totalRow);
            UiBuildUtility.Place(totalValueRect, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-48f, 0f), new Vector2(360f, 80f));
            TMP_Text totalValue = UiBuildUtility.AddText(totalValueRect, "0", UiSkin.TitleFont, 68f, UiSkin.Warning, TextAlignmentOptions.MidlineRight);

            Button next = CreateBottomButton(panel, "NextButton", "Btn_MainButton_Green", "NEXT LEVEL", "Pictoicon_Arrow_Next", 440f);
            Button retry = CreateBottomButton(panel, "RetryButton", "Btn_MainButton_Blue", "RETRY", "Pictoicon_Refresh", 280f);
            Button menu = CreateBottomButton(panel, "MenuButton", "Btn_MainButton_Red", "MAIN MENU", "Pictoicon_Home_0", 120f);

            UiBuildUtility.Bind(popup,
                "_canvasGroup", canvasGroup,
                "_content", panel,
                "_dimBackground", true,
                "_closeOnBackdropClick", false,
                // The run is over: the buttons are the only way out.
                "_closeOnBackKey", false,
                "_fitPadding", 48f,
                "_titleBanner", banner,
                "_titleText", titleText,
                "_wonBanner", UiSkin.Label("Label_TitleFlag01_Green"),
                "_lostBanner", UiSkin.Label("Label_TitleFlag01_Red"),
                "_wonTitle", "LEVEL CLEARED",
                "_lostTitle", "YOU DIED",
                "_killsText", kills,
                "_scoreText", score,
                "_healthBonusText", healthBonus,
                "_damageTakenText", damageTaken,
                "_totalText", totalValue,
                "_newBestBadge", badge.gameObject,
                "_retryButton", retry,
                "_nextButton", next,
                "_menuButton", menu);

            Save(root, ResultPath);
        }

        private static Image CreateTitleFlag(RectTransform panel, string title, Sprite flag)
        {
            RectTransform rect = UiBuildUtility.CreateNode("TitleBanner", panel);
            UiBuildUtility.Place(rect, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, 12f), new Vector2(620f, 175f));
            Image image = UiBuildUtility.AddImage(rect, flag, UiSkin.Paper, Image.Type.Simple, false);
            RectTransform textRect = UiBuildUtility.CreateNode("Title", rect);
            UiBuildUtility.Place(textRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 6f), new Vector2(520f, 90f));
            UiBuildUtility.AddText(textRect, title, UiSkin.TitleFont, 58f, UiSkin.Paper, TextAlignmentOptions.Center);
            return image;
        }

        private static TMP_Text CreateStatRow(RectTransform panel, string name, string iconName, string label, float y, Color iconColor)
        {
            RectTransform row = UiBuildUtility.CreateNode(name, panel);
            UiBuildUtility.Place(row, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(760f, RowHeight));
            UiBuildUtility.AddImage(row, UiSkin.Frame("Frame_ItemFrame03_Navy"), UiSkin.Paper, Image.Type.Sliced, false);

            RectTransform icon = UiBuildUtility.CreateNode("Icon", row);
            UiBuildUtility.Place(icon, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(54f, 0f), new Vector2(52f, 52f));
            UiBuildUtility.AddImage(icon, UiSkin.Icon(iconName), iconColor, Image.Type.Simple, false);

            RectTransform labelRect = UiBuildUtility.CreateNode("Label", row);
            UiBuildUtility.Place(labelRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(102f, 0f), new Vector2(380f, 60f));
            UiBuildUtility.AddText(labelRect, label, UiSkin.BodyFont, 38f, UiSkin.HudLabel, TextAlignmentOptions.MidlineLeft);

            RectTransform valueRect = UiBuildUtility.CreateNode("Value", row);
            UiBuildUtility.Place(valueRect, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-40f, 0f), new Vector2(240f, 60f));
            return UiBuildUtility.AddText(valueRect, "0", UiSkin.TitleFont, 46f, UiSkin.Paper, TextAlignmentOptions.MidlineRight);
        }

        private static Button CreateMainButton(RectTransform panel, string name, string spriteName, string label, string iconName, Vector2 position)
        {
            RectTransform rect = UiBuildUtility.CreateNode(name, panel);
            UiBuildUtility.Place(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(ButtonWidth, ButtonHeight));
            return FinishButton(rect, spriteName, label, iconName);
        }

        private static Button CreateBottomButton(RectTransform panel, string name, string spriteName, string label, string iconName, float y)
        {
            RectTransform rect = UiBuildUtility.CreateNode(name, panel);
            UiBuildUtility.Place(rect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(ButtonWidth, ButtonHeight));
            return FinishButton(rect, spriteName, label, iconName);
        }

        private static Button FinishButton(RectTransform rect, string spriteName, string label, string iconName)
        {
            Image background = UiBuildUtility.AddImage(rect, UiSkin.Button(spriteName), UiSkin.Paper, Image.Type.Sliced, true);
            Button button = UiBuildUtility.AddButton(rect, background);
            var fx = rect.gameObject.AddComponent<UiButtonFx>();
            UiBuildUtility.Bind(fx, "_target", rect);

            RectTransform icon = UiBuildUtility.CreateNode("Icon", rect);
            UiBuildUtility.Place(icon, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(88f, 4f), new Vector2(76f, 76f));
            UiBuildUtility.AddImage(icon, UiSkin.Icon(iconName), UiSkin.Paper, Image.Type.Simple, false);

            RectTransform labelRect = UiBuildUtility.CreateNode("Label", rect);
            UiBuildUtility.Place(labelRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(56f, 4f), new Vector2(430f, 80f));
            UiBuildUtility.AddText(labelRect, label, UiSkin.TitleFont, 50f, UiSkin.Paper, TextAlignmentOptions.Center);
            return button;
        }

        private static void Save(RectTransform root, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(root.gameObject, path);
            Object.DestroyImmediate(root.gameObject);
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
    }
}
