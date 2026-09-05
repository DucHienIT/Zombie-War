using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using ZombieWar.Data;
using ZombieWar.UI;

namespace ZombieWar.EditorTools.UI
{
    // Authors UIRoot.prefab: every screen the game has, in one prefab, with no reference
    // that points outside it. Both scenes drop the same instance in and let their binder
    // do the talking.
    public static class UiRootPrefabBuilder
    {
        private const string LogPrefix = "[UI Build]";
        public const string PrefabPath = "Assets/_ZombieWar/Prefabs/UI/UIRoot.prefab";
        private const string TapClipPath = "Assets/_ZombieWar/Audio/Weapons/Pistol_ClipIn_05.wav";
        private const string FeedbackProfilePath = "Assets/_ZombieWar/Data/Rules/FeedbackProfile.asset";

        private static readonly Vector2 ReferenceResolution = new Vector2(1080f, 1920f);
        private static readonly Vector2 TopLeft = new Vector2(0f, 1f);
        private static readonly Vector2 TopRight = new Vector2(1f, 1f);
        private static readonly Vector2 TopCenter = new Vector2(0.5f, 1f);
        private static readonly Vector2 BottomRight = new Vector2(1f, 0f);
        private static readonly Vector2 BottomCenter = new Vector2(0.5f, 0f);
        private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
        private static readonly Vector2 MiddleLeft = new Vector2(0f, 0.5f);
        private static readonly Vector2 MiddleRight = new Vector2(1f, 0.5f);
        private static readonly Color CooldownCover = new Color(0.02f, 0.03f, 0.05f, 0.66f);
        private static readonly Color ReloadSweep = new Color(1f, 0.85f, 0.3f, 0.55f);
        private static readonly Color CountdownGlow = new Color(1f, 0.78f, 0.25f, 0.85f);
        private static readonly Color MenuBackground = new Color(0.055f, 0.07f, 0.1f, 1f);
        private static readonly Color TitleGlow = new Color(0.85f, 0.18f, 0.14f, 0.28f);
        private static readonly Color LockedVeil = new Color(0.02f, 0.03f, 0.05f, 0.55f);

        private const float MenuHeaderHeight = 150f;
        private const float MenuTabBarHeight = 200f;
        // Weapon grid: three tiles per row, two rows before the grid runs under the tab bar on 9:16.
        private const int WeaponGridColumns = 3;
        private const int MaxWeaponSlots = 6;
        private const float WeaponGridTop = -210f;
        private const float WeaponCardSpacingX = 330f;
        private const float WeaponCardSpacingY = 410f;
        private static readonly Vector2 WeaponCardSize = new Vector2(300f, 380f);
        private static readonly Vector2 ChapterArtSize = new Vector2(520f, 520f);
        private const float ChapterArtCenterY = -560f;
        private static readonly string[] TabLabels = { "SHOP", "WEAPON", "BATTLE", "TALENT", "LOCK" };
        private static readonly string[] TabIcons = { "Pictoicon_Shop_0", "Pictoicon_Gun", "Pictoicon_Battle", "Pictoicon_Buff", "Pictoicon_Lock" };

        [MenuItem("Tools/Zombie War/UI/2. Rebuild UI Root Prefab", false, 101)]
        public static void Rebuild()
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Rebuild UI Root Prefab",
                "This re-authors UIRoot.prefab and the two popup prefabs from scratch. Manual Inspector tweaks inside them are lost.",
                "Rebuild",
                "Cancel");
            if (!confirmed)
            {
                return;
            }

            RebuildWithoutPrompt();
        }

        public static void RebuildWithoutPrompt()
        {
            EnsureFolder(Path.GetDirectoryName(PrefabPath).Replace('\\', '/') + "/");
            PopupPrefabBuilder.BuildAll();

            RectTransform root = UiBuildUtility.CreateNode("UIRoot", null);
            var manager = root.gameObject.AddComponent<UIManager>();

            BuildEventSystem(root);
            AudioSource audioSource = BuildAudioSource(root);

            Canvas hudCanvas = CreateCanvas(root, "Canvas_HUD", 0, true);
            RectTransform safeArea = UiBuildUtility.CreateNode("SafeArea", hudCanvas.transform);
            UiBuildUtility.Stretch(safeArea, 0f, 0f, 0f, 0f);
            var safeAreaFitter = safeArea.gameObject.AddComponent<SafeAreaFitter>();
            UiBuildUtility.Bind(safeAreaFitter, "_rectTransform", safeArea);

            RectTransform topBar = UiBuildUtility.CreateNode("TopBar", safeArea);
            // Tall enough for the health bar and the battle-level bar underneath it.
            UiBuildUtility.StretchRow(topBar, 1f, 1f, 0f, 0f, 0f, 272f);
            UiBuildUtility.AddImage(topBar, UiSkin.Frame("Frame_BarFrame_Top01_Navy"), UiSkin.Paper, Image.Type.Sliced, false);

            TimerView timer = BuildTimerChip(topBar);
            ScoreView score = BuildScoreChips(topBar);
            Button pauseButton = BuildPauseButton(topBar);
            HealthBarView healthBar = BuildHealthBar(topBar);
            XpBarView xpBar = BuildXpBar(topBar);
            BuildJoystick(safeArea);
            GunHudView gunHud = BuildGunButton(safeArea);
            BombButtonView bombButton = BuildBombButton(safeArea);

            MenuScreenView menuScreen = BuildMenuCanvas(root);
            CountdownView countdown = BuildCountdownCanvas(root);
            PopupManager popups = BuildPopupCanvas(root, out PausePopupUI pausePopup, out ResultPopupUI resultPopup,
                out SkillChoicePopupUI skillPopup, out WeaponDetailPopupUI weaponPopup, out SettingsPopupUI settingsPopup);

            var tapClip = AssetDatabase.LoadAssetAtPath<AudioClip>(TapClipPath);
            UiBuildUtility.Bind(manager,
                "_hudCanvas", hudCanvas,
                "_menuScreen", menuScreen,
                "_countdown", countdown,
                "_popups", popups,
                "_healthBar", healthBar,
                "_timer", timer,
                "_score", score,
                "_gunHud", gunHud,
                "_bombButton", bombButton,
                "_xpBar", xpBar,
                "_pauseButton", pauseButton,
                "_pausePopup", pausePopup,
                "_resultPopup", resultPopup,
                "_skillPopup", skillPopup,
                "_weaponPopup", weaponPopup,
                "_settingsPopup", settingsPopup,
                "_audioSource", audioSource,
                "_tapClip", tapClip,
                "_gunSwitchClip", tapClip,
                "_levelUpClip", tapClip);

            PrefabUtility.SaveAsPrefabAsset(root.gameObject, PrefabPath);
            Object.DestroyImmediate(root.gameObject);
            AssetDatabase.SaveAssets();
            Debug.Log($"{LogPrefix} rebuilt {PrefabPath}.");
        }

        private static void BuildEventSystem(RectTransform root)
        {
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            go.transform.SetParent(root, false);
        }

        private static AudioSource BuildAudioSource(RectTransform root)
        {
            var go = new GameObject("UiAudio", typeof(AudioSource));
            go.transform.SetParent(root, false);
            var source = go.GetComponent<AudioSource>();
            source.playOnAwake = false;
            // UI sound lives with the UI: the menu scene has no AudioService to borrow.
            source.spatialBlend = 0f;
            source.ignoreListenerPause = true;
            return source;
        }

        private static Canvas CreateCanvas(Transform parent, string name, int sortingOrder, bool interactive)
        {
            RectTransform rect = UiBuildUtility.CreateNode(name, parent);
            var canvas = rect.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            var scaler = rect.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            // Matching width keeps the UI the same physical size on every phone; a taller
            // screen simply gets more design pixels of headroom.
            scaler.matchWidthOrHeight = 0f;

            if (interactive)
            {
                rect.gameObject.AddComponent<GraphicRaycaster>();
            }

            return canvas;
        }

        private static TimerView BuildTimerChip(RectTransform topBar)
        {
            RectTransform chip = UiBuildUtility.CreateNode("TimerChip", topBar);
            UiBuildUtility.Place(chip, TopLeft, TopLeft, new Vector2(28f, -22f), new Vector2(252f, 96f));
            UiBuildUtility.AddImage(chip, UiSkin.Frame("Frame_ItemFrame03_Navy"), UiSkin.Paper, Image.Type.Sliced, false);

            RectTransform icon = UiBuildUtility.CreateNode("Icon", chip);
            UiBuildUtility.Place(icon, MiddleLeft, Center, new Vector2(50f, 0f), new Vector2(54f, 54f));
            UiBuildUtility.AddImage(icon, UiSkin.Icon("Pictoicon_Timer"), UiSkin.IconOnDark, Image.Type.Simple, false);

            RectTransform textRect = UiBuildUtility.CreateNode("Text", chip);
            UiBuildUtility.Place(textRect, MiddleRight, MiddleRight, new Vector2(-24f, 0f), new Vector2(160f, 64f));
            TMP_Text text = UiBuildUtility.AddText(textRect, "03:00", UiSkin.TitleFont, 48f, UiSkin.Paper, TextAlignmentOptions.MidlineRight);

            var view = chip.gameObject.AddComponent<TimerView>();
            UiBuildUtility.Bind(view,
                "_text", text,
                "_pulseTarget", chip,
                "_pulseAtSeconds", 30,
                "_warningAtSeconds", 10,
                "_normalColor", UiSkin.Paper,
                "_warningColor", UiSkin.Danger,
                "_pulseScale", 0.25f,
                "_pulseDuration", 0.4f);
            return view;
        }

        private static ScoreView BuildScoreChips(RectTransform topBar)
        {
            RectTransform group = UiBuildUtility.CreateNode("ScoreGroup", topBar);
            UiBuildUtility.Place(group, TopLeft, TopLeft, new Vector2(292f, -22f), new Vector2(0f, 96f));

            RectTransform killsChip = CreateStatChip(group, "KillsChip", "Pictoicon_Skull", new Vector2(0f, 0f), 210f, out TMP_Text killsText);
            CreateStatChip(group, "ScoreChip", "Pictoicon_Star", new Vector2(222f, 0f), 250f, out TMP_Text scoreText);

            var view = group.gameObject.AddComponent<ScoreView>();
            UiBuildUtility.Bind(view,
                "_killsText", killsText,
                "_scoreText", scoreText,
                "_killsPunchTarget", killsChip,
                "_punchScale", 0.2f,
                "_punchDuration", 0.25f);
            return view;
        }

        private static RectTransform CreateStatChip(RectTransform parent, string name, string iconName, Vector2 position, float width, out TMP_Text valueText)
        {
            RectTransform chip = UiBuildUtility.CreateNode(name, parent);
            UiBuildUtility.Place(chip, TopLeft, TopLeft, position, new Vector2(width, 96f));
            UiBuildUtility.AddImage(chip, UiSkin.Frame("Frame_ItemFrame03_Navy"), UiSkin.Paper, Image.Type.Sliced, false);

            RectTransform icon = UiBuildUtility.CreateNode("Icon", chip);
            UiBuildUtility.Place(icon, MiddleLeft, Center, new Vector2(48f, 0f), new Vector2(52f, 52f));
            UiBuildUtility.AddImage(icon, UiSkin.Icon(iconName), UiSkin.IconOnDark, Image.Type.Simple, false);

            RectTransform textRect = UiBuildUtility.CreateNode("Value", chip);
            UiBuildUtility.Place(textRect, MiddleRight, MiddleRight, new Vector2(-22f, 0f), new Vector2(width - 96f, 64f));
            valueText = UiBuildUtility.AddText(textRect, "0", UiSkin.TitleFont, 46f, UiSkin.Paper, TextAlignmentOptions.MidlineRight);
            return chip;
        }

        private static Button BuildPauseButton(RectTransform topBar)
        {
            RectTransform rect = UiBuildUtility.CreateNode("PauseButton", topBar);
            UiBuildUtility.Place(rect, TopRight, TopRight, new Vector2(-26f, -14f), new Vector2(120f, 134f));
            Image background = UiBuildUtility.AddImage(rect, UiSkin.Button("Btn_OtherButton_Circle01_n"), UiSkin.Paper, Image.Type.Simple, true);
            Button button = UiBuildUtility.AddButton(rect, background);
            AddPressFx(rect);

            RectTransform icon = UiBuildUtility.CreateNode("Icon", rect);
            UiBuildUtility.Place(icon, Center, Center, new Vector2(0f, 4f), new Vector2(58f, 58f));
            UiBuildUtility.AddImage(icon, UiSkin.Icon("Pictoicon_Control_Pause"), UiSkin.Paper, Image.Type.Simple, false);
            return button;
        }

        private static HealthBarView BuildHealthBar(RectTransform topBar)
        {
            RectTransform group = UiBuildUtility.CreateNode("HealthBar", topBar);
            UiBuildUtility.StretchRow(group, 1f, 0.5f, 28f, 28f, -150f, 70f);

            RectTransform icon = UiBuildUtility.CreateNode("Icon", group);
            UiBuildUtility.Place(icon, MiddleLeft, Center, new Vector2(32f, 0f), new Vector2(60f, 60f));
            UiBuildUtility.AddImage(icon, UiSkin.Icon("Pictoicon_Health"), UiSkin.Danger, Image.Type.Simple, false);

            RectTransform track = UiBuildUtility.CreateNode("Track", group);
            track.anchorMin = new Vector2(0f, 0.5f);
            track.anchorMax = new Vector2(1f, 0.5f);
            track.pivot = Center;
            track.offsetMin = new Vector2(78f, 0f);
            track.offsetMax = new Vector2(-8f, 0f);
            track.sizeDelta = new Vector2(track.sizeDelta.x, 46f);
            UiBuildUtility.AddImage(track, UiSkin.Slider("Slider10_Frame"), UiSkin.Paper, Image.Type.Sliced, false);

            RectTransform fillArea = UiBuildUtility.CreateNode("FillArea", track);
            UiBuildUtility.Stretch(fillArea, 8f, 7f, 8f, 7f);

            RectTransform delayedFill = CreateBarFill(fillArea, "DelayedFill", UiSkin.Slider("Slider10_Fill_Red"));
            RectTransform fill = CreateBarFill(fillArea, "Fill", UiSkin.Slider("Slider10_Fill_Green"));
            var fillImage = fill.GetComponent<Image>();

            RectTransform pulse = UiBuildUtility.CreateNode("LowHealthPulse", track);
            UiBuildUtility.Stretch(pulse, -6f, -6f, -6f, -6f);
            UiBuildUtility.AddImage(pulse, UiSkin.Slider("Slider10_Frame"), UiSkin.Danger, Image.Type.Sliced, false);
            var pulseGroup = pulse.gameObject.AddComponent<CanvasGroup>();
            pulseGroup.alpha = 0f;
            pulseGroup.blocksRaycasts = false;

            var view = group.gameObject.AddComponent<HealthBarView>();
            UiBuildUtility.Bind(view,
                "_feedback", AssetDatabase.LoadAssetAtPath<FeedbackProfileSO>(FeedbackProfilePath),
                "_fill", fill,
                "_delayedFill", delayedFill,
                "_lowHealthPulse", pulseGroup,
                "_fillImage", fillImage,
                "_healthyFill", UiSkin.Slider("Slider10_Fill_Green"),
                "_lowHealthFill", UiSkin.Slider("Slider10_Fill_Red"),
                "_pulseDuration", 0.4f);
            return view;
        }

        private static XpBarView BuildXpBar(RectTransform topBar)
        {
            RectTransform group = UiBuildUtility.CreateNode("XpBar", topBar);
            UiBuildUtility.StretchRow(group, 1f, 0.5f, 28f, 28f, -212f, 44f);

            RectTransform badge = UiBuildUtility.CreateNode("LevelBadge", group);
            UiBuildUtility.Place(badge, MiddleLeft, Center, new Vector2(30f, 0f), new Vector2(60f, 60f));
            UiBuildUtility.AddImage(badge, UiSkin.Button("Btn_OtherButton_Circle02"), UiSkin.XpAccent, Image.Type.Simple, false);
            RectTransform badgeText = UiBuildUtility.CreateNode("Text", badge);
            UiBuildUtility.Stretch(badgeText, 0f, 4f, 0f, 0f);
            TMP_Text levelText = UiBuildUtility.AddText(badgeText, "1", UiSkin.TitleFont, 34f, UiSkin.Paper, TextAlignmentOptions.Center);

            RectTransform track = UiBuildUtility.CreateNode("Track", group);
            track.anchorMin = new Vector2(0f, 0.5f);
            track.anchorMax = new Vector2(1f, 0.5f);
            track.pivot = Center;
            track.offsetMin = new Vector2(78f, 0f);
            track.offsetMax = new Vector2(-8f, 0f);
            track.sizeDelta = new Vector2(track.sizeDelta.x, 28f);
            UiBuildUtility.AddImage(track, UiSkin.Slider("Slider11_Frame"), UiSkin.Paper, Image.Type.Sliced, false);

            RectTransform fillArea = UiBuildUtility.CreateNode("FillArea", track);
            UiBuildUtility.Stretch(fillArea, 6f, 5f, 6f, 5f);
            RectTransform fill = CreateBarFill(fillArea, "Fill", UiSkin.Slider("Slider11_Fill_Blue"));
            // Authored empty: a run opens at zero experience, and the bar is anchor driven.
            fill.anchorMax = new Vector2(0f, 1f);

            var view = group.gameObject.AddComponent<XpBarView>();
            UiBuildUtility.Bind(view,
                "_fill", fill,
                "_levelText", levelText,
                "_levelPunchTarget", badge,
                "_fillDuration", 0.18f,
                "_punchScale", 0.35f,
                "_punchDuration", 0.35f);
            return view;
        }

        private static RectTransform CreateBarFill(RectTransform fillArea, string name, Sprite sprite)
        {
            RectTransform fill = UiBuildUtility.CreateNode(name, fillArea);
            fill.anchorMin = new Vector2(0f, 0f);
            fill.anchorMax = new Vector2(1f, 1f);
            fill.pivot = new Vector2(0f, 0.5f);
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
            UiBuildUtility.AddImage(fill, sprite, UiSkin.Paper, Image.Type.Sliced, false);
            return fill;
        }

        // The stick is authored here, not preserved from a scene, so the prefab is complete
        // on its own. Values mirror the joystick the project has been playing with.
        private static void BuildJoystick(RectTransform safeArea)
        {
            RectTransform joystick = UiBuildUtility.CreateNode("Joystick", safeArea);
            UiBuildUtility.Place(joystick, Vector2.zero, Center, new Vector2(230f, 300f), new Vector2(220f, 220f));
            UiBuildUtility.AddImage(joystick, UiSkin.Button("Btn_OtherButton_Circle02"), UiSkin.JoystickBase, Image.Type.Simple, false);

            RectTransform handle = UiBuildUtility.CreateNode("Handle", joystick);
            UiBuildUtility.Place(handle, Center, Center, Vector2.zero, new Vector2(96f, 96f));
            UiBuildUtility.AddImage(handle, UiSkin.Button("Btn_OtherButton_Circle02"), UiSkin.JoystickHandle, Image.Type.Simple, true);

            RectTransform touchArea = UiBuildUtility.CreateNode("TouchArea", handle);
            UiBuildUtility.Place(touchArea, Center, Center, Vector2.zero, new Vector2(260f, 260f));
            UiBuildUtility.AddImage(touchArea, null, UiSkin.Invisible, Image.Type.Simple, true);

            var stick = handle.gameObject.AddComponent<OnScreenStick>();
            UiBuildUtility.Bind(stick,
                "m_ControlPath", "<Gamepad>/leftStick",
                "m_MovementRange", 110f,
                "m_DynamicOriginRange", 100f,
                "m_Behaviour", 0,
                "m_UseIsolatedInputActions", false);
        }

        private static GunHudView BuildGunButton(RectTransform safeArea)
        {
            RectTransform rect = UiBuildUtility.CreateNode("GunButton", safeArea);
            UiBuildUtility.Place(rect, BottomRight, BottomRight, new Vector2(-44f, 316f), new Vector2(170f, 189f));
            Image background = UiBuildUtility.AddImage(rect, UiSkin.Button("Btn_OtherButton_Circle01_n"), UiSkin.Paper, Image.Type.Simple, true);
            Button button = UiBuildUtility.AddButton(rect, background);
            AddPressFx(rect);

            RectTransform icon = UiBuildUtility.CreateNode("Icon", rect);
            UiBuildUtility.Place(icon, Center, Center, new Vector2(0f, 12f), new Vector2(100f, 100f));
            Image iconImage = UiBuildUtility.AddImage(icon, UiSkin.Icon("Pictoicon_Gun"), UiSkin.Paper, Image.Type.Simple, false);

            RectTransform reloadFill = UiBuildUtility.CreateNode("ReloadFill", rect);
            UiBuildUtility.Place(reloadFill, Center, Center, new Vector2(0f, 6f), new Vector2(150f, 150f));
            Image reloadImage = UiBuildUtility.AddImage(reloadFill, UiSkin.Button("Btn_OtherButton_Circle02"), ReloadSweep, Image.Type.Filled, false);
            reloadImage.fillMethod = Image.FillMethod.Radial360;
            reloadImage.fillOrigin = (int)Image.Origin360.Top;
            reloadImage.fillClockwise = true;
            reloadImage.fillAmount = 0f;

            RectTransform switchBadge = UiBuildUtility.CreateNode("SwitchBadge", rect);
            UiBuildUtility.Place(switchBadge, TopLeft, Center, new Vector2(26f, -34f), new Vector2(50f, 50f));
            UiBuildUtility.AddImage(switchBadge, UiSkin.Icon("Pictoicon_Switch"), UiSkin.IconOnDark, Image.Type.Simple, false);

            RectTransform badge = UiBuildUtility.CreateNode("ReloadBadge", rect);
            UiBuildUtility.Place(badge, Center, Center, new Vector2(0f, 12f), new Vector2(84f, 84f));
            UiBuildUtility.AddImage(badge, UiSkin.Icon("Pictoicon_Reload"), UiSkin.Warning, Image.Type.Simple, false);
            badge.gameObject.SetActive(false);

            RectTransform nameRect = UiBuildUtility.CreateNode("Name", rect);
            UiBuildUtility.Place(nameRect, TopCenter, BottomCenter, new Vector2(0f, 26f), new Vector2(240f, 38f));
            // Outline face: this label floats over the map with no plate behind it.
            TMP_Text nameText = UiBuildUtility.AddText(nameRect, "RIFLE", UiSkin.TitleFont, 30f, UiSkin.Paper, TextAlignmentOptions.Center);

            RectTransform ammoRect = UiBuildUtility.CreateNode("Ammo", rect);
            UiBuildUtility.Place(ammoRect, BottomCenter, BottomCenter, new Vector2(0f, 16f), new Vector2(170f, 48f));
            TMP_Text ammoText = UiBuildUtility.AddText(ammoRect, "30/30", UiSkin.TitleFont, 36f, UiSkin.Paper, TextAlignmentOptions.Center);

            var view = rect.gameObject.AddComponent<GunHudView>();
            UiBuildUtility.Bind(view,
                "_switchButton", button,
                "_icon", iconImage,
                "_ammoText", ammoText,
                "_nameText", nameText,
                "_reloadFill", reloadImage,
                "_reloadBadge", badge.gameObject,
                "_ammoColor", UiSkin.Paper,
                "_emptyAmmoColor", UiSkin.Danger);
            return view;
        }

        private static BombButtonView BuildBombButton(RectTransform safeArea)
        {
            RectTransform rect = UiBuildUtility.CreateNode("BombButton", safeArea);
            UiBuildUtility.Place(rect, BottomRight, BottomRight, new Vector2(-30f, 70f), new Vector2(210f, 234f));
            Image background = UiBuildUtility.AddImage(rect, UiSkin.Button("Btn_OtherButton_Circle01_n"), UiSkin.Paper, Image.Type.Simple, true);
            Button button = UiBuildUtility.AddButton(rect, background);
            AddPressFx(rect);

            RectTransform icon = UiBuildUtility.CreateNode("Icon", rect);
            UiBuildUtility.Place(icon, Center, Center, new Vector2(0f, 8f), new Vector2(120f, 120f));
            Image iconImage = UiBuildUtility.AddImage(icon, UiSkin.Icon("Icon_Bomb_Bomb"), UiSkin.Paper, Image.Type.Simple, false);

            RectTransform cooldown = UiBuildUtility.CreateNode("CooldownFill", rect);
            UiBuildUtility.Place(cooldown, Center, Center, new Vector2(0f, 6f), new Vector2(186f, 186f));
            Image cooldownImage = UiBuildUtility.AddImage(cooldown, UiSkin.Button("Btn_OtherButton_Circle02"), CooldownCover, Image.Type.Filled, false);
            cooldownImage.fillMethod = Image.FillMethod.Radial360;
            cooldownImage.fillOrigin = (int)Image.Origin360.Top;
            cooldownImage.fillClockwise = false;
            cooldownImage.fillAmount = 0f;

            RectTransform badge = UiBuildUtility.CreateNode("ChargesBadge", rect);
            UiBuildUtility.Place(badge, TopRight, Center, new Vector2(-16f, -26f), new Vector2(74f, 74f));
            UiBuildUtility.AddImage(badge, UiSkin.Button("Btn_OtherButton_Circle02"), UiSkin.Danger, Image.Type.Simple, false);
            RectTransform badgeText = UiBuildUtility.CreateNode("Text", badge);
            UiBuildUtility.Stretch(badgeText, 0f, 4f, 0f, 0f);
            TMP_Text chargesText = UiBuildUtility.AddText(badgeText, "3", UiSkin.TitleFont, 40f, UiSkin.Paper, TextAlignmentOptions.Center);

            var view = rect.gameObject.AddComponent<BombButtonView>();
            UiBuildUtility.Bind(view,
                "_button", button,
                "_chargesText", chargesText,
                "_icon", iconImage,
                "_cooldownFill", cooldownImage,
                "_readyPunchTarget", rect,
                "_readyIconColor", UiSkin.Paper,
                "_spentIconColor", UiSkin.IconMuted,
                "_punchScale", 0.18f,
                "_punchDuration", 0.3f);
            return view;
        }

        private static MenuScreenView BuildMenuCanvas(RectTransform root)
        {
            Canvas canvas = CreateCanvas(root, "Canvas_Menu", 5, true);
            var canvasRect = (RectTransform)canvas.transform;

            RectTransform background = UiBuildUtility.CreateNode("Background", canvasRect);
            UiBuildUtility.Stretch(background, 0f, 0f, 0f, 0f);
            UiBuildUtility.AddImage(background, null, MenuBackground, Image.Type.Simple, true);

            RectTransform safeArea = UiBuildUtility.CreateNode("SafeArea", canvasRect);
            UiBuildUtility.Stretch(safeArea, 0f, 0f, 0f, 0f);
            var fitter = safeArea.gameObject.AddComponent<SafeAreaFitter>();
            UiBuildUtility.Bind(fitter, "_rectTransform", safeArea);

            MenuHeaderView header = BuildMenuHeader(safeArea);

            // Pages sit between the shared header and the tab bar; one is active at a time.
            RectTransform pages = UiBuildUtility.CreateNode("Pages", safeArea);
            UiBuildUtility.Stretch(pages, 0f, MenuTabBarHeight, 0f, MenuHeaderHeight);
            GameObject shopPage = BuildPlaceholderPage(pages, "Page_Shop", "Pictoicon_Shop_0", "SHOP", "COMING SOON");
            WeaponPageView weaponPage = BuildWeaponPage(pages);
            BattlePageView battlePage = BuildBattlePage(pages);
            GameObject talentPage = BuildPlaceholderPage(pages, "Page_Talent", "Pictoicon_Buff", "TALENT", "COMING SOON");
            GameObject lockedPage = BuildPlaceholderPage(pages, "Page_Locked", "Pictoicon_Lock", "LOCKED", "COMING SOON");
            shopPage.SetActive(false);
            weaponPage.gameObject.SetActive(false);
            talentPage.SetActive(false);
            lockedPage.SetActive(false);

            MenuTabBarView tabBar = BuildTabBar(safeArea);

            var view = canvas.gameObject.AddComponent<MenuScreenView>();
            UiBuildUtility.Bind(view,
                "_canvas", canvas,
                "_header", header,
                "_tabBar", tabBar,
                "_pages", new Object[] { shopPage, weaponPage.gameObject, battlePage.gameObject, talentPage, lockedPage },
                "_battlePage", battlePage,
                "_weaponPage", weaponPage,
                "_defaultTab", (int)MenuTab.Battle);
            return view;
        }

        private static MenuHeaderView BuildMenuHeader(RectTransform safeArea)
        {
            RectTransform header = UiBuildUtility.CreateNode("Header", safeArea);
            UiBuildUtility.StretchRow(header, 1f, 1f, 0f, 0f, 0f, MenuHeaderHeight);
            UiBuildUtility.AddImage(header, UiSkin.Frame("Frame_BarFrame_Top01_Navy"), UiSkin.Paper, Image.Type.Sliced, false);

            RectTransform crown = UiBuildUtility.CreateNode("Crown", header);
            UiBuildUtility.Place(crown, TopLeft, TopLeft, new Vector2(32f, -30f), new Vector2(44f, 44f));
            UiBuildUtility.AddImage(crown, UiSkin.Icon("Pictoicon_Crown"), UiSkin.Warning, Image.Type.Simple, false);

            RectTransform levelRect = UiBuildUtility.CreateNode("Level", header);
            UiBuildUtility.Place(levelRect, TopLeft, TopLeft, new Vector2(88f, -26f), new Vector2(320f, 52f));
            TMP_Text levelText = UiBuildUtility.AddText(levelRect, "LEVEL 1", UiSkin.TitleFont, 36f, UiSkin.Paper, TextAlignmentOptions.MidlineLeft);

            RectTransform xpBar = UiBuildUtility.CreateNode("XpBar", header);
            UiBuildUtility.Place(xpBar, TopLeft, TopLeft, new Vector2(32f, -94f), new Vector2(340f, 28f));
            UiBuildUtility.AddImage(xpBar, UiSkin.Slider("Slider10_Frame"), UiSkin.Paper, Image.Type.Sliced, false);
            RectTransform xpFillArea = UiBuildUtility.CreateNode("FillArea", xpBar);
            UiBuildUtility.Stretch(xpFillArea, 6f, 5f, 6f, 5f);
            RectTransform xpFill = CreateBarFill(xpFillArea, "Fill", UiSkin.Slider("Slider10_Fill_Green"));

            RectTransform xpTextRect = UiBuildUtility.CreateNode("XpText", header);
            UiBuildUtility.Place(xpTextRect, TopLeft, TopLeft, new Vector2(386f, -88f), new Vector2(200f, 40f));
            TMP_Text xpText = UiBuildUtility.AddText(xpTextRect, "0/100", UiSkin.BodyFont, 26f, UiSkin.HudLabel, TextAlignmentOptions.MidlineLeft);

            RectTransform coinChip = UiBuildUtility.CreateNode("CoinChip", header);
            UiBuildUtility.Place(coinChip, MiddleRight, MiddleRight, new Vector2(-150f, -4f), new Vector2(280f, 84f));
            UiBuildUtility.AddImage(coinChip, UiSkin.Frame("Frame_ItemFrame03_Navy"), UiSkin.Paper, Image.Type.Sliced, false);
            RectTransform coinIcon = UiBuildUtility.CreateNode("Icon", coinChip);
            UiBuildUtility.Place(coinIcon, MiddleLeft, Center, new Vector2(46f, 0f), new Vector2(56f, 56f));
            UiBuildUtility.AddImage(coinIcon, UiSkin.Icon("Icon_Gold"), UiSkin.Paper, Image.Type.Simple, false);
            RectTransform coinText = UiBuildUtility.CreateNode("Value", coinChip);
            UiBuildUtility.Place(coinText, MiddleRight, MiddleRight, new Vector2(-24f, 0f), new Vector2(180f, 60f));
            TMP_Text coinsText = UiBuildUtility.AddText(coinText, "0", UiSkin.TitleFont, 40f, UiSkin.Warning, TextAlignmentOptions.MidlineRight);

            RectTransform gear = UiBuildUtility.CreateNode("SettingsButton", header);
            UiBuildUtility.Place(gear, MiddleRight, MiddleRight, new Vector2(-24f, -4f), new Vector2(104f, 116f));
            Image gearBackground = UiBuildUtility.AddImage(gear, UiSkin.Button("Btn_OtherButton_Circle01_n"), UiSkin.Paper, Image.Type.Simple, true);
            Button settingsButton = UiBuildUtility.AddButton(gear, gearBackground);
            AddPressFx(gear);
            RectTransform gearIcon = UiBuildUtility.CreateNode("Icon", gear);
            UiBuildUtility.Place(gearIcon, Center, Center, new Vector2(0f, 4f), new Vector2(54f, 54f));
            UiBuildUtility.AddImage(gearIcon, UiSkin.Icon("Pictoicon_Setting"), UiSkin.Paper, Image.Type.Simple, false);

            var view = header.gameObject.AddComponent<MenuHeaderView>();
            UiBuildUtility.Bind(view,
                "_levelText", levelText,
                "_xpFill", xpFill,
                "_xpText", xpText,
                "_coinsText", coinsText,
                "_settingsButton", settingsButton);
            return view;
        }

        private static MenuTabBarView BuildTabBar(RectTransform safeArea)
        {
            RectTransform bar = UiBuildUtility.CreateNode("TabBar", safeArea);
            UiBuildUtility.StretchRow(bar, 0f, 0f, 0f, 0f, 0f, MenuTabBarHeight);
            UiBuildUtility.AddImage(bar, UiSkin.Frame("Frame_BarFrame_Bottom01_Navy"), UiSkin.Paper, Image.Type.Sliced, true);

            var tabs = new List<Object>(TabLabels.Length);
            for (int i = 0; i < TabLabels.Length; i++)
            {
                tabs.Add(BuildTab(bar, i, TabLabels.Length, TabLabels[i], TabIcons[i], i == (int)MenuTab.Gacha));
            }

            var view = bar.gameObject.AddComponent<MenuTabBarView>();
            UiBuildUtility.Bind(view, "_tabs", tabs);
            return view;
        }

        private static MenuTabButtonView BuildTab(RectTransform bar, int index, int count, string label, string iconName, bool locked)
        {
            RectTransform tab = UiBuildUtility.CreateNode("Tab_" + label, bar);
            tab.anchorMin = new Vector2((float)index / count, 0f);
            tab.anchorMax = new Vector2((float)(index + 1) / count, 1f);
            tab.pivot = Center;
            tab.offsetMin = Vector2.zero;
            tab.offsetMax = Vector2.zero;

            // An invisible plate takes the tap so the highlight can be switched off freely.
            Image hit = UiBuildUtility.AddImage(tab, null, UiSkin.Invisible, Image.Type.Simple, true);
            Button button = UiBuildUtility.AddButton(tab, hit);

            // The selected tab rises above the bar like a pressed key.
            RectTransform highlightRect = UiBuildUtility.CreateNode("Highlight", tab);
            UiBuildUtility.Stretch(highlightRect, 14f, 10f, 14f, -18f);
            Image highlight = UiBuildUtility.AddImage(highlightRect, UiSkin.Button("Btn_OtherButton_Square02"), UiSkin.Paper, Image.Type.Sliced, false);
            highlight.enabled = false;

            // Authored in its resting state so the prefab already looks right before Awake runs.
            Color restingTint = locked ? UiSkin.IconMuted : UiSkin.HudLabel;
            RectTransform icon = UiBuildUtility.CreateNode("Icon", tab);
            UiBuildUtility.Place(icon, Center, Center, new Vector2(0f, 24f), new Vector2(80f, 80f));
            Image iconImage = UiBuildUtility.AddImage(icon, UiSkin.Icon(iconName), restingTint, Image.Type.Simple, false);

            RectTransform labelRect = UiBuildUtility.CreateNode("Label", tab);
            UiBuildUtility.Place(labelRect, BottomCenter, BottomCenter, new Vector2(0f, 24f), new Vector2(200f, 34f));
            TMP_Text labelText = UiBuildUtility.AddText(labelRect, label, UiSkin.BodyFont, 28f, restingTint, TextAlignmentOptions.Center);

            var view = tab.gameObject.AddComponent<MenuTabButtonView>();
            UiBuildUtility.Bind(view,
                "_button", button,
                "_highlight", highlight,
                "_icon", iconImage,
                "_label", labelText,
                "_locked", locked,
                "_selectedColor", UiSkin.Paper,
                "_normalColor", UiSkin.HudLabel,
                "_lockedColor", UiSkin.IconMuted);
            return view;
        }

        private static GameObject BuildPlaceholderPage(RectTransform pages, string name, string iconName, string title, string message)
        {
            RectTransform page = UiBuildUtility.CreateNode(name, pages);
            UiBuildUtility.Stretch(page, 0f, 0f, 0f, 0f);

            RectTransform icon = UiBuildUtility.CreateNode("Icon", page);
            UiBuildUtility.Place(icon, Center, Center, new Vector2(0f, 140f), new Vector2(180f, 180f));
            UiBuildUtility.AddImage(icon, UiSkin.Icon(iconName), UiSkin.IconMuted, Image.Type.Simple, false);

            RectTransform titleRect = UiBuildUtility.CreateNode("Title", page);
            UiBuildUtility.Place(titleRect, Center, Center, new Vector2(0f, -10f), new Vector2(700f, 90f));
            UiBuildUtility.AddText(titleRect, title, UiSkin.TitleFont, 64f, UiSkin.Paper, TextAlignmentOptions.Center);

            RectTransform messageRect = UiBuildUtility.CreateNode("Message", page);
            UiBuildUtility.Place(messageRect, Center, Center, new Vector2(0f, -80f), new Vector2(700f, 60f));
            UiBuildUtility.AddText(messageRect, message, UiSkin.BodyFont, 40f, UiSkin.HudLabel, TextAlignmentOptions.Center);
            return page.gameObject;
        }

        private static BattlePageView BuildBattlePage(RectTransform pages)
        {
            RectTransform page = UiBuildUtility.CreateNode("Page_Battle", pages);
            UiBuildUtility.Stretch(page, 0f, 0f, 0f, 0f);

            BuildModeToggle(page);

            RectTransform glow = UiBuildUtility.CreateNode("Glow", page);
            UiBuildUtility.Place(glow, TopCenter, Center, new Vector2(0f, ChapterArtCenterY), new Vector2(900f, 900f));
            UiBuildUtility.AddImage(glow, UiSkin.Popup("Popup_00_Glow_white"), TitleGlow, Image.Type.Simple, false);

            LevelCardView card = BuildChapterCard(page);

            Button prev = BuildArrowButton(page, "PrevButton", "Pictoicon_Arrow_Prev", TopLeft, new Vector2(110f, ChapterArtCenterY));
            Button next = BuildArrowButton(page, "NextButton", "Pictoicon_Arrow_Next", TopRight, new Vector2(-110f, ChapterArtCenterY));

            RectTransform counter = UiBuildUtility.CreateNode("ChapterCounter", page);
            UiBuildUtility.Place(counter, BottomCenter, BottomCenter, new Vector2(0f, 44f), new Vector2(400f, 40f));
            TMP_Text counterText = UiBuildUtility.AddText(counter, "1 / 1", UiSkin.BodyFont, 28f, UiSkin.HudLabel, TextAlignmentOptions.Center);

            var view = page.gameObject.AddComponent<BattlePageView>();
            UiBuildUtility.Bind(view,
                "_card", card,
                "_prevButton", prev,
                "_nextButton", next,
                "_chapterText", counterText);
            return view;
        }

        // NORMAL / HARD segmented control. Hard mode does not exist yet, so the right half is
        // authored locked and the control has no behaviour behind it.
        private static void BuildModeToggle(RectTransform page)
        {
            RectTransform track = UiBuildUtility.CreateNode("ModeToggle", page);
            UiBuildUtility.Place(track, TopCenter, TopCenter, new Vector2(0f, -30f), new Vector2(480f, 96f));
            UiBuildUtility.AddImage(track, UiSkin.Frame("Frame_ItemFrame03_Navy"), UiSkin.Paper, Image.Type.Sliced, false);

            RectTransform normal = UiBuildUtility.CreateNode("Normal", track);
            UiBuildUtility.Place(normal, MiddleLeft, MiddleLeft, new Vector2(8f, 0f), new Vector2(232f, 80f));
            UiBuildUtility.AddImage(normal, UiSkin.Button("Btn_OtherButton_Square02"), UiSkin.Paper, Image.Type.Sliced, false);
            RectTransform normalLabel = UiBuildUtility.CreateNode("Label", normal);
            UiBuildUtility.Stretch(normalLabel, 0f, 4f, 0f, 0f);
            UiBuildUtility.AddText(normalLabel, "NORMAL", UiSkin.TitleFont, 32f, UiSkin.Paper, TextAlignmentOptions.Center);

            RectTransform hard = UiBuildUtility.CreateNode("Hard", track);
            UiBuildUtility.Place(hard, MiddleRight, MiddleRight, new Vector2(-8f, 0f), new Vector2(232f, 80f));
            RectTransform hardLock = UiBuildUtility.CreateNode("Lock", hard);
            UiBuildUtility.Place(hardLock, MiddleLeft, Center, new Vector2(52f, 0f), new Vector2(32f, 32f));
            UiBuildUtility.AddImage(hardLock, UiSkin.Icon("Pictoicon_Lock"), UiSkin.IconMuted, Image.Type.Simple, false);
            RectTransform hardLabel = UiBuildUtility.CreateNode("Label", hard);
            UiBuildUtility.Place(hardLabel, Center, Center, new Vector2(22f, 0f), new Vector2(160f, 60f));
            UiBuildUtility.AddText(hardLabel, "HARD", UiSkin.TitleFont, 32f, UiSkin.IconMuted, TextAlignmentOptions.Center);
        }

        private static LevelCardView BuildChapterCard(RectTransform page)
        {
            RectTransform card = UiBuildUtility.CreateNode("ChapterCard", page);
            UiBuildUtility.Stretch(card, 0f, 0f, 0f, 0f);

            RectTransform chapter = UiBuildUtility.CreateNode("Chapter", card);
            UiBuildUtility.Place(chapter, TopCenter, TopCenter, new Vector2(0f, -150f), new Vector2(800f, 90f));
            TMP_Text chapterText = UiBuildUtility.AddText(chapter, "CHAPTER 1", UiSkin.TitleFont, 76f, UiSkin.Paper, TextAlignmentOptions.Center);

            RectTransform artFrame = UiBuildUtility.CreateNode("ArtFrame", card);
            UiBuildUtility.Place(artFrame, TopCenter, Center, new Vector2(0f, ChapterArtCenterY), ChapterArtSize);
            UiBuildUtility.AddImage(artFrame, UiSkin.Frame("Frame_BasicFrame_Square05"), UiSkin.Paper, Image.Type.Sliced, false);
            var artGroup = artFrame.gameObject.AddComponent<CanvasGroup>();

            RectTransform art = UiBuildUtility.CreateNode("Artwork", artFrame);
            UiBuildUtility.Stretch(art, 60f, 60f, 60f, 60f);
            Image artwork = UiBuildUtility.AddImage(art, UiSkin.Icon("Pictoicon_Castle"), UiSkin.IconOnDark, Image.Type.Simple, false);
            artwork.preserveAspect = true;

            RectTransform locked = UiBuildUtility.CreateNode("LockedBadge", artFrame);
            UiBuildUtility.Stretch(locked, 0f, 0f, 0f, 0f);
            UiBuildUtility.AddImage(locked, null, LockedVeil, Image.Type.Simple, false);
            RectTransform lockIcon = UiBuildUtility.CreateNode("Icon", locked);
            UiBuildUtility.Place(lockIcon, Center, Center, Vector2.zero, new Vector2(150f, 150f));
            UiBuildUtility.AddImage(lockIcon, UiSkin.Icon("Pictoicon_Lock"), UiSkin.Paper, Image.Type.Simple, false);
            locked.gameObject.SetActive(false);

            RectTransform nameRect = UiBuildUtility.CreateNode("Name", card);
            UiBuildUtility.Place(nameRect, TopCenter, TopCenter, new Vector2(0f, -860f), new Vector2(800f, 60f));
            TMP_Text nameText = UiBuildUtility.AddText(nameRect, "LEVEL", UiSkin.BodyFont, 44f, UiSkin.Paper, TextAlignmentOptions.Center);

            RectTransform bestGroup = UiBuildUtility.CreateNode("BestScore", card);
            UiBuildUtility.Place(bestGroup, TopCenter, TopCenter, new Vector2(0f, -935f), new Vector2(320f, 76f));
            UiBuildUtility.AddImage(bestGroup, UiSkin.Frame("Frame_ItemFrame03_Navy"), UiSkin.Paper, Image.Type.Sliced, false);
            RectTransform bestIcon = UiBuildUtility.CreateNode("Icon", bestGroup);
            UiBuildUtility.Place(bestIcon, MiddleLeft, Center, new Vector2(42f, 0f), new Vector2(46f, 46f));
            UiBuildUtility.AddImage(bestIcon, UiSkin.Icon("Pictoicon_Trophy_0"), UiSkin.Warning, Image.Type.Simple, false);
            RectTransform bestRect = UiBuildUtility.CreateNode("Value", bestGroup);
            UiBuildUtility.Place(bestRect, MiddleRight, MiddleRight, new Vector2(-24f, 0f), new Vector2(220f, 52f));
            TMP_Text bestText = UiBuildUtility.AddText(bestRect, "0", UiSkin.TitleFont, 38f, UiSkin.Paper, TextAlignmentOptions.MidlineRight);

            RectTransform playRect = UiBuildUtility.CreateNode("StartButton", card);
            UiBuildUtility.Place(playRect, BottomCenter, BottomCenter, new Vector2(0f, 110f), new Vector2(560f, 150f));
            Image playBackground = UiBuildUtility.AddImage(playRect, UiSkin.Button("Btn_MainButton_Green"), UiSkin.Paper, Image.Type.Sliced, true);
            Button playButton = UiBuildUtility.AddButton(playRect, playBackground);
            AddPressFx(playRect);
            RectTransform playLabel = UiBuildUtility.CreateNode("Label", playRect);
            UiBuildUtility.Place(playLabel, Center, Center, new Vector2(0f, 4f), new Vector2(400f, 90f));
            UiBuildUtility.AddText(playLabel, "START", UiSkin.TitleFont, 58f, UiSkin.Paper, TextAlignmentOptions.Center);

            var view = card.gameObject.AddComponent<LevelCardView>();
            UiBuildUtility.Bind(view,
                "_playButton", playButton,
                "_artwork", artwork,
                "_artworkGroup", artGroup,
                "_chapterText", chapterText,
                "_nameText", nameText,
                "_bestScoreText", bestText,
                "_bestScoreGroup", bestGroup.gameObject,
                "_lockedBadge", locked.gameObject,
                "_lockedAlpha", 0.45f);
            return view;
        }

        private static Button BuildArrowButton(RectTransform page, string name, string iconName, Vector2 anchor, Vector2 position)
        {
            RectTransform rect = UiBuildUtility.CreateNode(name, page);
            UiBuildUtility.Place(rect, anchor, Center, position, new Vector2(120f, 134f));
            Image background = UiBuildUtility.AddImage(rect, UiSkin.Button("Btn_OtherButton_Circle01_n"), UiSkin.Paper, Image.Type.Simple, true);
            Button button = UiBuildUtility.AddButton(rect, background);
            AddPressFx(rect);

            RectTransform icon = UiBuildUtility.CreateNode("Icon", rect);
            UiBuildUtility.Place(icon, Center, Center, new Vector2(0f, 4f), new Vector2(56f, 56f));
            UiBuildUtility.AddImage(icon, UiSkin.Icon(iconName), UiSkin.Paper, Image.Type.Simple, false);
            return button;
        }

        private static WeaponPageView BuildWeaponPage(RectTransform pages)
        {
            RectTransform page = UiBuildUtility.CreateNode("Page_Weapon", pages);
            UiBuildUtility.Stretch(page, 0f, 0f, 0f, 0f);

            RectTransform title = UiBuildUtility.CreateNode("Title", page);
            UiBuildUtility.Place(title, TopCenter, TopCenter, new Vector2(0f, -36f), new Vector2(800f, 80f));
            UiBuildUtility.AddText(title, "WEAPONS", UiSkin.TitleFont, 60f, UiSkin.Paper, TextAlignmentOptions.Center);

            RectTransform banner = UiBuildUtility.CreateNode("UnlockedBanner", page);
            UiBuildUtility.Place(banner, TopCenter, TopCenter, new Vector2(0f, -128f), new Vector2(420f, 66f));
            UiBuildUtility.AddImage(banner, UiSkin.Label("Label_BasicLabel01_White"), UiSkin.Warning, Image.Type.Sliced, false);
            RectTransform bannerLabel = UiBuildUtility.CreateNode("Label", banner);
            UiBuildUtility.Stretch(bannerLabel, 0f, 2f, 0f, 0f);
            UiBuildUtility.AddText(bannerLabel, "UNLOCKED", UiSkin.BodyFont, 30f, UiSkin.Ink, TextAlignmentOptions.Center);

            int gunCount = CountGuns();
            if (gunCount > MaxWeaponSlots)
            {
                Debug.LogWarning($"{LogPrefix} {gunCount} guns found but the weapon grid fits {MaxWeaponSlots} before it runs under the tab bar.");
            }

            var cards = new List<Object>(gunCount);
            for (int i = 0; i < gunCount; i++)
            {
                int column = i % WeaponGridColumns;
                int row = i / WeaponGridColumns;
                float x = (column - (WeaponGridColumns - 1) * 0.5f) * WeaponCardSpacingX;
                float y = WeaponGridTop - row * WeaponCardSpacingY;
                cards.Add(BuildWeaponCard(page, i, new Vector2(x, y)));
            }

            var view = page.gameObject.AddComponent<WeaponPageView>();
            UiBuildUtility.Bind(view, "_cards", cards);
            return view;
        }

        private static WeaponCardView BuildWeaponCard(RectTransform page, int index, Vector2 position)
        {
            RectTransform card = UiBuildUtility.CreateNode("WeaponCard" + (index + 1), page);
            UiBuildUtility.Place(card, TopCenter, TopCenter, position, WeaponCardSize);
            Image background = UiBuildUtility.AddImage(card, UiSkin.Button("Btn_OtherButton_Square03_Blue"), UiSkin.Paper, Image.Type.Sliced, true);
            Button button = UiBuildUtility.AddButton(card, background);
            AddPressFx(card);

            RectTransform namePill = UiBuildUtility.CreateNode("NamePill", card);
            UiBuildUtility.Place(namePill, TopCenter, TopCenter, new Vector2(0f, -22f), new Vector2(250f, 56f));
            UiBuildUtility.AddImage(namePill, UiSkin.Label("Label_BasicLabel01_White"), UiSkin.Warning, Image.Type.Sliced, false);
            RectTransform nameRect = UiBuildUtility.CreateNode("Label", namePill);
            UiBuildUtility.Stretch(nameRect, 8f, 2f, 8f, 0f);
            TMP_Text nameText = UiBuildUtility.AddText(nameRect, "RIFLE", UiSkin.BodyFont, 26f, UiSkin.Ink, TextAlignmentOptions.Center);

            RectTransform unlocked = UiBuildUtility.CreateNode("Unlocked", card);
            UiBuildUtility.Stretch(unlocked, 0f, 0f, 0f, 0f);
            RectTransform iconFrame = UiBuildUtility.CreateNode("IconFrame", unlocked);
            UiBuildUtility.Place(iconFrame, TopCenter, TopCenter, new Vector2(0f, -96f), new Vector2(150f, 160f));
            UiBuildUtility.AddImage(iconFrame, UiSkin.Frame("Frame_ItemFrame01_Color_Blue"), UiSkin.Paper, Image.Type.Simple, false);
            RectTransform iconRect = UiBuildUtility.CreateNode("Icon", iconFrame);
            UiBuildUtility.Place(iconRect, Center, Center, Vector2.zero, new Vector2(110f, 110f));
            Image icon = UiBuildUtility.AddImage(iconRect, UiSkin.Icon("Pictoicon_Gun"), UiSkin.Paper, Image.Type.Simple, false);
            icon.preserveAspect = true;

            RectTransform levelRect = UiBuildUtility.CreateNode("Level", unlocked);
            UiBuildUtility.Place(levelRect, TopCenter, TopCenter, new Vector2(0f, -270f), new Vector2(260f, 40f));
            TMP_Text levelText = UiBuildUtility.AddText(levelRect, "LEVEL 0", UiSkin.BodyFont, 28f, UiSkin.Paper, TextAlignmentOptions.Center);

            RectTransform bar = UiBuildUtility.CreateNode("LevelBar", unlocked);
            UiBuildUtility.Place(bar, TopCenter, TopCenter, new Vector2(0f, -320f), new Vector2(230f, 32f));
            UiBuildUtility.AddImage(bar, UiSkin.Slider("Slider10_Frame"), UiSkin.Paper, Image.Type.Sliced, false);
            RectTransform barFillArea = UiBuildUtility.CreateNode("FillArea", bar);
            UiBuildUtility.Stretch(barFillArea, 5f, 4f, 5f, 4f);
            RectTransform barFill = CreateBarFill(barFillArea, "Fill", UiSkin.Slider("Slider10_Fill_Green"));
            RectTransform progressRect = UiBuildUtility.CreateNode("Progress", bar);
            UiBuildUtility.Stretch(progressRect, 0f, 0f, 0f, 0f);
            TMP_Text progressText = UiBuildUtility.AddText(progressRect, "0/5", UiSkin.TitleFont, 22f, UiSkin.Paper, TextAlignmentOptions.Center);

            RectTransform lockedGroup = UiBuildUtility.CreateNode("Locked", card);
            UiBuildUtility.Stretch(lockedGroup, 0f, 0f, 0f, 0f);
            RectTransform lockIcon = UiBuildUtility.CreateNode("Icon", lockedGroup);
            UiBuildUtility.Place(lockIcon, TopCenter, TopCenter, new Vector2(0f, -122f), new Vector2(96f, 96f));
            UiBuildUtility.AddImage(lockIcon, UiSkin.Icon("Pictoicon_Lock"), UiSkin.IconMuted, Image.Type.Simple, false);
            RectTransform soonRect = UiBuildUtility.CreateNode("Text", lockedGroup);
            UiBuildUtility.Place(soonRect, TopCenter, TopCenter, new Vector2(0f, -286f), new Vector2(260f, 40f));
            UiBuildUtility.AddText(soonRect, "COMING SOON", UiSkin.BodyFont, 26f, UiSkin.HudLabel, TextAlignmentOptions.Center);
            lockedGroup.gameObject.SetActive(false);

            var view = card.gameObject.AddComponent<WeaponCardView>();
            UiBuildUtility.Bind(view,
                "_button", button,
                "_nameText", nameText,
                "_unlockedGroup", unlocked.gameObject,
                "_lockedGroup", lockedGroup.gameObject,
                "_icon", icon,
                "_levelText", levelText,
                "_levelFill", barFill,
                "_levelProgressText", progressText);
            return view;
        }

        private static CountdownView BuildCountdownCanvas(RectTransform root)
        {
            Canvas canvas = CreateCanvas(root, "Canvas_Countdown", 10, false);
            var canvasRect = (RectTransform)canvas.transform;

            RectTransform glow = UiBuildUtility.CreateNode("Glow", canvasRect);
            UiBuildUtility.Place(glow, Center, Center, new Vector2(0f, 120f), new Vector2(900f, 900f));
            Image glowImage = UiBuildUtility.AddImage(glow, UiSkin.Popup("Popup_00_Glow_white"), CountdownGlow, Image.Type.Simple, false);

            RectTransform textRect = UiBuildUtility.CreateNode("Text", canvasRect);
            UiBuildUtility.Place(textRect, Center, Center, new Vector2(0f, 120f), new Vector2(700f, 420f));
            TMP_Text text = UiBuildUtility.AddText(textRect, "3", UiSkin.TitleFont, 300f, UiSkin.Paper, TextAlignmentOptions.Center);

            var view = canvas.gameObject.AddComponent<CountdownView>();
            UiBuildUtility.Bind(view,
                "_canvas", canvas,
                "_text", text,
                "_glow", glowImage,
                "_punchTarget", textRect,
                "_punchScale", 0.4f,
                "_punchDuration", 0.35f,
                "_glowFadeDuration", 0.35f);
            return view;
        }

        private static PopupManager BuildPopupCanvas(RectTransform root, out PausePopupUI pausePopup, out ResultPopupUI resultPopup,
            out SkillChoicePopupUI skillPopup, out WeaponDetailPopupUI weaponPopup, out SettingsPopupUI settingsPopup)
        {
            Canvas canvas = CreateCanvas(root, "Canvas_Popup", 20, true);
            var canvasRect = (RectTransform)canvas.transform;

            RectTransform backdropRect = UiBuildUtility.CreateNode("Backdrop", canvasRect);
            UiBuildUtility.Stretch(backdropRect, 0f, 0f, 0f, 0f);
            Image backdropImage = UiBuildUtility.AddImage(backdropRect, null, UiSkin.Backdrop, Image.Type.Simple, true);
            var backdropGroup = backdropRect.gameObject.AddComponent<CanvasGroup>();
            backdropGroup.alpha = 0f;
            backdropGroup.blocksRaycasts = false;
            var backdrop = backdropRect.gameObject.AddComponent<PopupBackdrop>();

            var manager = canvas.gameObject.AddComponent<PopupManager>();
            UiBuildUtility.Bind(backdrop,
                "_manager", manager,
                "_canvasGroup", backdropGroup,
                "_image", backdropImage,
                "_fadeDuration", 0.18f,
                "_shownAlpha", 1f);

            pausePopup = InstantiatePopup<PausePopupUI>(PopupPrefabBuilder.PausePath, canvasRect);
            resultPopup = InstantiatePopup<ResultPopupUI>(PopupPrefabBuilder.ResultPath, canvasRect);
            skillPopup = InstantiatePopup<SkillChoicePopupUI>(PopupPrefabBuilder.SkillChoicePath, canvasRect);
            weaponPopup = InstantiatePopup<WeaponDetailPopupUI>(PopupPrefabBuilder.WeaponDetailPath, canvasRect);
            settingsPopup = InstantiatePopup<SettingsPopupUI>(PopupPrefabBuilder.SettingsPath, canvasRect);

            UiBuildUtility.Bind(manager,
                "_backdrop", backdrop,
                "_canvas", canvas,
                "_popups", new Object[] { pausePopup, resultPopup, skillPopup, weaponPopup, settingsPopup });
            return manager;
        }

        private static T InstantiatePopup<T>(string prefabPath, RectTransform parent) where T : PopupBase
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"{LogPrefix} popup prefab {prefabPath} is missing.");
                return null;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            var rect = (RectTransform)instance.transform;
            UiBuildUtility.Stretch(rect, 0f, 0f, 0f, 0f);
            instance.SetActive(false);
            return instance.GetComponent<T>();
        }

        private static void AddPressFx(RectTransform rect)
        {
            var fx = rect.gameObject.AddComponent<UiButtonFx>();
            UiBuildUtility.Bind(fx, "_target", rect);
        }

        private static int CountGuns()
        {
            string[] guids = AssetDatabase.FindAssets("t:GunDefinitionSO", new[] { "Assets/_ZombieWar/Data/Weapons" });
            if (guids.Length == 0)
            {
                Debug.LogError($"{LogPrefix} no GunDefinitionSO found - the weapon page would have no rows.");
            }

            return guids.Length;
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
