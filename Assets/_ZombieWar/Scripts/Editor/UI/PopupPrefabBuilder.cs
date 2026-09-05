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
        public const string SkillChoicePath = Folder + "SkillChoicePopup.prefab";
        public const string WeaponDetailPath = Folder + "WeaponDetailPopup.prefab";
        public const string SettingsPath = Folder + "SettingsPopup.prefab";

        private const float ButtonWidth = 640f;
        private const float ButtonHeight = 145f;
        private const float RowHeight = 92f;
        // The pack's switch handle is drawn larger than its track and overhangs it.
        private const float SwitchHandleInset = 30f;
        private const float SwitchHandleLift = 3f;
        private static readonly Vector2 SwitchSize = new Vector2(136f, 64f);
        private static readonly Vector2 SwitchHandleSize = new Vector2(64f, 77f);

        private const int SkillCardSlots = 3;
        // One star slot per stack the deepest skill can reach; shallower skills hide the rest.
        private const int SkillStarSlots = 5;
        private const float SkillCardGap = 20f;
        private const float SkillCardTop = -290f;
        private const float SkillStarSpacing = 48f;
        private static readonly Vector2 SkillCardSize = new Vector2(300f, 680f);

        public static void BuildAll()
        {
            EnsureFolder(Folder);
            BuildPause();
            BuildResult();
            BuildSkillChoice();
            BuildWeaponDetail();
            BuildSettings();
        }

        private static void BuildSettings()
        {
            RectTransform root = UiBuildUtility.CreateNode("SettingsPopup", null);
            UiBuildUtility.Stretch(root, 0f, 0f, 0f, 0f);
            var canvasGroup = root.gameObject.AddComponent<CanvasGroup>();
            var popup = root.gameObject.AddComponent<SettingsPopupUI>();

            RectTransform panel = UiBuildUtility.CreateNode("Panel", root);
            UiBuildUtility.Place(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(880f, 520f));
            UiBuildUtility.AddImage(panel, UiSkin.Popup("Popup_Frame01_Navy"), UiSkin.Paper, Image.Type.Sliced, true);

            CreateTitleFlag(panel, "SETTINGS", UiSkin.Label("Label_TitleFlag01_Blue"));
            Button close = CreateCloseButton(panel);
            SwitchToggleView shakeSwitch = CreateSettingRow(panel, "Row_ScreenShake", "SCREEN SHAKE", new Vector2(0f, -40f));

            UiBuildUtility.Bind(popup,
                "_canvasGroup", canvasGroup,
                "_content", panel,
                "_dimBackground", true,
                "_closeOnBackdropClick", true,
                "_closeOnBackKey", true,
                "_fitPadding", 48f,
                "_closeButton", close,
                "_shakeSwitch", shakeSwitch);

            Save(root, SettingsPath);
        }

        private static void BuildWeaponDetail()
        {
            RectTransform root = UiBuildUtility.CreateNode("WeaponDetailPopup", null);
            UiBuildUtility.Stretch(root, 0f, 0f, 0f, 0f);
            var canvasGroup = root.gameObject.AddComponent<CanvasGroup>();
            var popup = root.gameObject.AddComponent<WeaponDetailPopupUI>();

            RectTransform panel = UiBuildUtility.CreateNode("Panel", root);
            UiBuildUtility.Place(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 1400f));
            UiBuildUtility.AddImage(panel, UiSkin.Popup("Popup_Frame01_Navy"), UiSkin.Paper, Image.Type.Sliced, true);

            Button close = CreateCloseButton(panel);

            RectTransform nameRect = UiBuildUtility.CreateNode("Name", panel);
            UiBuildUtility.Place(nameRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(660f, 70f));
            TMP_Text nameText = UiBuildUtility.AddText(nameRect, "RIFLE", UiSkin.TitleFont, 56f, UiSkin.Paper, TextAlignmentOptions.Center);

            RectTransform iconFrame = UiBuildUtility.CreateNode("IconFrame", panel);
            UiBuildUtility.Place(iconFrame, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(300f, 300f));
            UiBuildUtility.AddImage(iconFrame, UiSkin.Frame("Frame_ItemFrame01_Color_Blue"), UiSkin.Paper, Image.Type.Simple, false);
            RectTransform iconRect = UiBuildUtility.CreateNode("Icon", iconFrame);
            UiBuildUtility.Place(iconRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(220f, 220f));
            Image icon = UiBuildUtility.AddImage(iconRect, UiSkin.Icon("Pictoicon_Gun"), UiSkin.Paper, Image.Type.Simple, false);
            icon.preserveAspect = true;

            RectTransform levelRect = UiBuildUtility.CreateNode("Level", panel);
            UiBuildUtility.Place(levelRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -470f), new Vector2(500f, 50f));
            TMP_Text levelText = UiBuildUtility.AddText(levelRect, "LEVEL 0 / 5", UiSkin.BodyFont, 36f, UiSkin.Warning, TextAlignmentOptions.Center);

            RectTransform descriptionRect = UiBuildUtility.CreateNode("Description", panel);
            UiBuildUtility.Place(descriptionRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -530f), new Vector2(760f, 90f));
            TextMeshProUGUI descriptionText = UiBuildUtility.AddText(descriptionRect, "Description", UiSkin.BodyFont, 30f, UiSkin.HudLabel, TextAlignmentOptions.Top);
            descriptionText.enableWordWrapping = true;

            RectTransform grid = UiBuildUtility.CreateNode("StatGrid", panel);
            UiBuildUtility.Place(grid, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -640f), new Vector2(800f, 420f));
            UiBuildUtility.AddImage(grid, UiSkin.Frame("Frame_BasicFrame_Square05"), UiSkin.Paper, Image.Type.Sliced, false);
            WeaponStatCellView attack = CreateStatCell(grid, "Cell_Attack", "Pictoicon_Damage", "ATTACK", 0, 0);
            WeaponStatCellView rate = CreateStatCell(grid, "Cell_Rate", "Pictoicon_Speed", "RATE", 1, 0);
            WeaponStatCellView magazine = CreateStatCell(grid, "Cell_Magazine", "Pictoicon_Stack", "MAGAZINE", 0, 1);
            WeaponStatCellView reload = CreateStatCell(grid, "Cell_Reload", "Pictoicon_Reload", "RELOAD", 1, 1);
            WeaponStatCellView shots = CreateStatCell(grid, "Cell_Shots", "Pictoicon_Missile", "SHOTS", 0, 2);
            WeaponStatCellView type = CreateStatCell(grid, "Cell_Type", "Pictoicon_Target", "TYPE", 1, 2);

            RectTransform cost = UiBuildUtility.CreateNode("Cost", panel);
            UiBuildUtility.Place(cost, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -1090f), new Vector2(400f, 60f));
            RectTransform costIcon = UiBuildUtility.CreateNode("Icon", cost);
            UiBuildUtility.Place(costIcon, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-70f, 0f), new Vector2(48f, 48f));
            UiBuildUtility.AddImage(costIcon, UiSkin.Icon("Icon_Gold"), UiSkin.Paper, Image.Type.Simple, false);
            RectTransform costTextRect = UiBuildUtility.CreateNode("Value", cost);
            UiBuildUtility.Place(costTextRect, new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f), new Vector2(-30f, 0f), new Vector2(220f, 60f));
            TMP_Text costText = UiBuildUtility.AddText(costTextRect, "150", UiSkin.TitleFont, 44f, UiSkin.Paper, TextAlignmentOptions.MidlineLeft);

            RectTransform upgrade = UiBuildUtility.CreateNode("UpgradeButton", panel);
            UiBuildUtility.Place(upgrade, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 110f), new Vector2(560f, 140f));
            Image upgradeBackground = UiBuildUtility.AddImage(upgrade, UiSkin.Button("Btn_MainButton_Green"), UiSkin.Paper, Image.Type.Sliced, true);
            Button upgradeButton = UiBuildUtility.AddButton(upgrade, upgradeBackground);
            var upgradeFx = upgrade.gameObject.AddComponent<UiButtonFx>();
            UiBuildUtility.Bind(upgradeFx, "_target", upgrade);
            RectTransform upgradeLabel = UiBuildUtility.CreateNode("Label", upgrade);
            UiBuildUtility.Place(upgradeLabel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(400f, 70f));
            UiBuildUtility.AddText(upgradeLabel, "UPGRADE", UiSkin.TitleFont, 48f, UiSkin.Paper, TextAlignmentOptions.Center);

            RectTransform hintRect = UiBuildUtility.CreateNode("Hint", panel);
            UiBuildUtility.Place(hintRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 44f), new Vector2(760f, 44f));
            TMP_Text hintText = UiBuildUtility.AddText(hintRect, "", UiSkin.BodyFont, 26f, UiSkin.HudLabel, TextAlignmentOptions.Center);

            UiBuildUtility.Bind(popup,
                "_canvasGroup", canvasGroup,
                "_content", panel,
                "_dimBackground", true,
                "_closeOnBackdropClick", true,
                "_closeOnBackKey", true,
                "_fitPadding", 48f,
                "_closeButton", close,
                "_icon", icon,
                "_nameText", nameText,
                "_levelText", levelText,
                "_descriptionText", descriptionText,
                "_attackCell", attack,
                "_rateCell", rate,
                "_magazineCell", magazine,
                "_reloadCell", reload,
                "_shotsCell", shots,
                "_typeCell", type,
                "_upgradeButton", upgradeButton,
                "_costGroup", cost.gameObject,
                "_costText", costText,
                "_hintText", hintText,
                "_affordableCostColor", UiSkin.Paper,
                "_unaffordableCostColor", UiSkin.Danger);

            Save(root, WeaponDetailPath);
        }

        // Two columns by three rows inside the 800x420 grid plate.
        private static WeaponStatCellView CreateStatCell(RectTransform grid, string name, string iconName, string label, int column, int row)
        {
            RectTransform cell = UiBuildUtility.CreateNode(name, grid);
            UiBuildUtility.Place(cell, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f + column * 390f, -12f - row * 132f), new Vector2(370f, 128f));

            RectTransform icon = UiBuildUtility.CreateNode("Icon", cell);
            UiBuildUtility.Place(icon, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(34f, 0f), new Vector2(40f, 40f));
            UiBuildUtility.AddImage(icon, UiSkin.Icon(iconName), UiSkin.IconOnDark, Image.Type.Simple, false);

            RectTransform labelRect = UiBuildUtility.CreateNode("Label", cell);
            UiBuildUtility.Place(labelRect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(76f, -18f), new Vector2(280f, 34f));
            UiBuildUtility.AddText(labelRect, label, UiSkin.BodyFont, 26f, UiSkin.HudLabel, TextAlignmentOptions.MidlineLeft);

            RectTransform valueRect = UiBuildUtility.CreateNode("Value", cell);
            UiBuildUtility.Place(valueRect, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(76f, 14f), new Vector2(150f, 50f));
            TMP_Text valueText = UiBuildUtility.AddText(valueRect, "0", UiSkin.TitleFont, 40f, UiSkin.Paper, TextAlignmentOptions.MidlineLeft);

            RectTransform nextGroup = UiBuildUtility.CreateNode("Next", cell);
            UiBuildUtility.Place(nextGroup, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(226f, 14f), new Vector2(140f, 50f));
            RectTransform arrow = UiBuildUtility.CreateNode("Arrow", nextGroup);
            UiBuildUtility.Place(arrow, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(12f, 0f), new Vector2(24f, 24f));
            UiBuildUtility.AddImage(arrow, UiSkin.Icon("Pictoicon_Arrow_Next"), UiSkin.Good, Image.Type.Simple, false);
            RectTransform nextRect = UiBuildUtility.CreateNode("Value", nextGroup);
            UiBuildUtility.Place(nextRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(34f, 0f), new Vector2(106f, 50f));
            TMP_Text nextText = UiBuildUtility.AddText(nextRect, "0", UiSkin.TitleFont, 30f, UiSkin.Good, TextAlignmentOptions.MidlineLeft);

            var view = cell.gameObject.AddComponent<WeaponStatCellView>();
            UiBuildUtility.Bind(view,
                "_valueText", valueText,
                "_nextText", nextText,
                "_nextGroup", nextGroup.gameObject);
            return view;
        }

        private static Button CreateCloseButton(RectTransform panel)
        {
            RectTransform rect = UiBuildUtility.CreateNode("CloseButton", panel);
            UiBuildUtility.Place(rect, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-40f, -44f), new Vector2(92f, 102f));
            Image background = UiBuildUtility.AddImage(rect, UiSkin.Button("Btn_OtherButton_Circle01_n"), UiSkin.Paper, Image.Type.Simple, true);
            Button button = UiBuildUtility.AddButton(rect, background);
            var fx = rect.gameObject.AddComponent<UiButtonFx>();
            UiBuildUtility.Bind(fx, "_target", rect);

            RectTransform icon = UiBuildUtility.CreateNode("Icon", rect);
            UiBuildUtility.Place(icon, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(40f, 40f));
            UiBuildUtility.AddImage(icon, UiSkin.Icon("Pictoicon_Close"), UiSkin.Paper, Image.Type.Simple, false);
            return button;
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
            UiBuildUtility.Place(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(920f, 1600f));
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
            TMP_Text coins = CreateStatRow(panel, "Row_Coins", "Icon_Gold", "COINS EARNED", -746f, UiSkin.Warning);
            TMP_Text xp = CreateStatRow(panel, "Row_Xp", "Pictoicon_Trophy_0", "XP EARNED", -850f, UiSkin.Good);

            RectTransform totalRow = UiBuildUtility.CreateNode("Row_Total", panel);
            UiBuildUtility.Place(totalRow, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -985f), new Vector2(800f, 130f));
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
                "_coinsText", coins,
                "_xpText", xp,
                "_newBestBadge", badge.gameObject,
                "_retryButton", retry,
                "_nextButton", next,
                "_menuButton", menu);

            Save(root, ResultPath);
        }

        private static void BuildSkillChoice()
        {
            RectTransform root = UiBuildUtility.CreateNode("SkillChoicePopup", null);
            UiBuildUtility.Stretch(root, 0f, 0f, 0f, 0f);
            var canvasGroup = root.gameObject.AddComponent<CanvasGroup>();
            var popup = root.gameObject.AddComponent<SkillChoicePopupUI>();

            RectTransform panel = UiBuildUtility.CreateNode("Panel", root);
            UiBuildUtility.Place(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 1050f));
            UiBuildUtility.AddImage(panel, UiSkin.Popup("Popup_Frame01_Navy"), UiSkin.Paper, Image.Type.Sliced, true);

            RectTransform divider = UiBuildUtility.CreateNode("TitleDivider", panel);
            UiBuildUtility.Place(divider, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -150f), new Vector2(760f, 56f));
            UiBuildUtility.AddImage(divider, UiSkin.Popup("Popup_00_TitleLIne"), UiSkin.Paper, Image.Type.Sliced, false);

            CreateTitleFlag(panel, "LEVEL UP", UiSkin.Label("Label_TitleFlag01_Purple"));
            TMP_Text levelText = CreateLevelChip(panel);

            // Three columns side by side: the whole draft has to be readable in one glance,
            // and the star row under each card shows how deep that skill can still go.
            var cards = new System.Collections.Generic.List<Object>(SkillCardSlots);
            float step = SkillCardSize.x + SkillCardGap;
            for (int i = 0; i < SkillCardSlots; i++)
            {
                cards.Add(BuildSkillCard(panel, i, (i - (SkillCardSlots - 1) * 0.5f) * step));
            }

            UiBuildUtility.Bind(popup,
                "_canvasGroup", canvasGroup,
                "_content", panel,
                "_dimBackground", true,
                // The player has to pick: there is no way out of this one but a card.
                "_closeOnBackdropClick", false,
                "_closeOnBackKey", false,
                "_fitPadding", 48f,
                "_cards", cards,
                "_levelText", levelText);

            Save(root, SkillChoicePath);
        }

        private static TMP_Text CreateLevelChip(RectTransform panel)
        {
            RectTransform chip = UiBuildUtility.CreateNode("LevelChip", panel);
            UiBuildUtility.Place(chip, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -218f), new Vector2(460f, 76f));
            UiBuildUtility.AddImage(chip, UiSkin.Frame("Frame_ItemFrame03_Navy"), UiSkin.Paper, Image.Type.Sliced, false);

            RectTransform label = UiBuildUtility.CreateNode("Label", chip);
            UiBuildUtility.Place(label, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(36f, 0f), new Vector2(300f, 56f));
            UiBuildUtility.AddText(label, "BATTLE LEVEL", UiSkin.BodyFont, 34f, UiSkin.HudLabel, TextAlignmentOptions.MidlineLeft);

            RectTransform value = UiBuildUtility.CreateNode("Value", chip);
            UiBuildUtility.Place(value, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-36f, 0f), new Vector2(120f, 60f));
            return UiBuildUtility.AddText(value, "1", UiSkin.TitleFont, 44f, UiSkin.Warning, TextAlignmentOptions.MidlineRight);
        }

        private static SkillCardView BuildSkillCard(RectTransform panel, int index, float x)
        {
            RectTransform card = UiBuildUtility.CreateNode("SkillCard" + (index + 1), panel);
            UiBuildUtility.Place(card, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(x, SkillCardTop), SkillCardSize);
            Image background = UiBuildUtility.AddImage(card, UiSkin.Frame("Frame_ItemFrame03_Navy"), UiSkin.Paper, Image.Type.Sliced, true);
            Button button = UiBuildUtility.AddButton(card, background);
            var fx = card.gameObject.AddComponent<UiButtonFx>();
            UiBuildUtility.Bind(fx, "_target", card);

            RectTransform nameRect = UiBuildUtility.CreateNode("Name", card);
            UiBuildUtility.Place(nameRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(256f, 120f));
            TextMeshProUGUI nameText = UiBuildUtility.AddText(nameRect, "SKILL NAME", UiSkin.TitleFont, 32f, UiSkin.Paper, TextAlignmentOptions.Top);
            // A narrow column cannot hold a name like PIERCING ROUNDS on one line.
            nameText.enableWordWrapping = true;

            GameObject newBadge = CreateNewBadge(card);

            RectTransform plate = UiBuildUtility.CreateNode("IconPlate", card);
            UiBuildUtility.Place(plate, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -306f), new Vector2(196f, 196f));
            UiBuildUtility.AddImage(plate, UiSkin.Button("Btn_OtherButton_Circle02"), UiSkin.Paper, Image.Type.Simple, false);

            RectTransform icon = UiBuildUtility.CreateNode("Icon", plate);
            UiBuildUtility.Place(icon, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(116f, 116f));
            Image iconImage = UiBuildUtility.AddImage(icon, UiSkin.Icon("Pictoicon_Buff"), UiSkin.Paper, Image.Type.Simple, false);

            RectTransform descriptionRect = UiBuildUtility.CreateNode("Description", card);
            UiBuildUtility.Place(descriptionRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -420f), new Vector2(256f, 130f));
            TextMeshProUGUI descriptionText = UiBuildUtility.AddText(descriptionRect, "Description", UiSkin.BodyFont, 26f, UiSkin.HudLabel, TextAlignmentOptions.Top);
            descriptionText.enableWordWrapping = true;

            RectTransform starRow = UiBuildUtility.CreateNode("Stars", card);
            UiBuildUtility.Place(starRow, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 64f),
                new Vector2(SkillStarSlots * SkillStarSpacing, 48f));
            var stars = new System.Collections.Generic.List<Object>(SkillStarSlots);
            for (int i = 0; i < SkillStarSlots; i++)
            {
                RectTransform star = UiBuildUtility.CreateNode("Star" + (i + 1), starRow);
                float starX = (i - (SkillStarSlots - 1) * 0.5f) * SkillStarSpacing;
                UiBuildUtility.Place(star, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(starX, 0f), new Vector2(42f, 42f));
                stars.Add(UiBuildUtility.AddImage(star, UiSkin.Icon("Pictoicon_Star"), UiSkin.Warning, Image.Type.Simple, false));
            }

            var view = card.gameObject.AddComponent<SkillCardView>();
            UiBuildUtility.Bind(view,
                "_button", button,
                "_icon", iconImage,
                "_nameText", nameText,
                "_descriptionText", descriptionText,
                "_newBadge", newBadge,
                "_stars", stars,
                "_starRow", starRow,
                "_starSpacing", SkillStarSpacing,
                "_starOnColor", UiSkin.Warning,
                "_starOffColor", UiSkin.IconMuted);
            return view;
        }

        private static GameObject CreateNewBadge(RectTransform card)
        {
            RectTransform badge = UiBuildUtility.CreateNode("NewBadge", card);
            UiBuildUtility.Place(badge, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -176f), new Vector2(180f, 62f));
            UiBuildUtility.AddImage(badge, UiSkin.Label("Label_TitleRibbon_Yellow"), UiSkin.Paper, Image.Type.Sliced, false);

            RectTransform text = UiBuildUtility.CreateNode("Text", badge);
            UiBuildUtility.Stretch(text, 0f, 4f, 0f, 0f);
            UiBuildUtility.AddText(text, "NEW", UiSkin.TitleFont, 32f, UiSkin.Ink, TextAlignmentOptions.Center);
            badge.gameObject.SetActive(false);
            return badge.gameObject;
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
