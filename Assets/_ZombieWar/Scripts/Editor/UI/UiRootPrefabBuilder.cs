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
        private static readonly Vector2 CardSize = new Vector2(470f, 540f);

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
            UiBuildUtility.StretchRow(topBar, 1f, 1f, 0f, 0f, 0f, 215f);
            UiBuildUtility.AddImage(topBar, UiSkin.Frame("Frame_BarFrame_Top01_Navy"), UiSkin.Paper, Image.Type.Sliced, false);

            TimerView timer = BuildTimerChip(topBar);
            ScoreView score = BuildScoreChips(topBar);
            Button pauseButton = BuildPauseButton(topBar);
            HealthBarView healthBar = BuildHealthBar(topBar);
            BuildJoystick(safeArea);
            GunHudView gunHud = BuildGunButton(safeArea);
            BombButtonView bombButton = BuildBombButton(safeArea);

            MenuScreenView menuScreen = BuildMenuCanvas(root);
            CountdownView countdown = BuildCountdownCanvas(root);
            PopupManager popups = BuildPopupCanvas(root, out PausePopupUI pausePopup, out ResultPopupUI resultPopup);

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
                "_pauseButton", pauseButton,
                "_pausePopup", pausePopup,
                "_resultPopup", resultPopup,
                "_audioSource", audioSource,
                "_tapClip", tapClip,
                "_gunSwitchClip", tapClip);

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

            RectTransform glow = UiBuildUtility.CreateNode("TitleGlow", safeArea);
            UiBuildUtility.Place(glow, TopCenter, Center, new Vector2(0f, -300f), new Vector2(840f, 840f));
            UiBuildUtility.AddImage(glow, UiSkin.Popup("Popup_00_Glow_white"), TitleGlow, Image.Type.Simple, false);

            RectTransform titleBanner = UiBuildUtility.CreateNode("TitleBanner", safeArea);
            UiBuildUtility.Place(titleBanner, TopCenter, Center, new Vector2(0f, -280f), new Vector2(780f, 220f));
            UiBuildUtility.AddImage(titleBanner, UiSkin.Label("Label_TitleFlag01_Red"), UiSkin.Paper, Image.Type.Simple, false);
            RectTransform titleText = UiBuildUtility.CreateNode("Title", titleBanner);
            UiBuildUtility.Place(titleText, Center, Center, new Vector2(0f, 6f), new Vector2(680f, 110f));
            UiBuildUtility.AddText(titleText, "ZOMBIE WAR", UiSkin.TitleFont, 76f, UiSkin.Paper, TextAlignmentOptions.Center);

            RectTransform subtitle = UiBuildUtility.CreateNode("Subtitle", safeArea);
            UiBuildUtility.Place(subtitle, TopCenter, Center, new Vector2(0f, -470f), new Vector2(800f, 70f));
            UiBuildUtility.AddText(subtitle, "SELECT A LEVEL", UiSkin.BodyFont, 44f, UiSkin.HudLabel, TextAlignmentOptions.Center);

            int cardCount = CountLevels();
            RectTransform cardRow = UiBuildUtility.CreateNode("LevelCards", safeArea);
            UiBuildUtility.Place(cardRow, Center, Center, new Vector2(0f, -170f), new Vector2(1080f, CardSize.y));
            float spacing = CardSize.x + 60f;
            var cards = new List<Object>(cardCount);
            for (int i = 0; i < cardCount; i++)
            {
                float x = (i - (cardCount - 1) * 0.5f) * spacing;
                cards.Add(BuildCard(cardRow, i + 1, x));
            }

            var view = canvas.gameObject.AddComponent<MenuScreenView>();
            UiBuildUtility.Bind(view, "_canvas", canvas, "_cards", cards);
            return view;
        }

        private static LevelCardView BuildCard(RectTransform row, int slotNumber, float x)
        {
            RectTransform card = UiBuildUtility.CreateNode("LevelCard" + slotNumber, row);
            UiBuildUtility.Place(card, Center, Center, new Vector2(x, 0f), CardSize);
            UiBuildUtility.AddImage(card, UiSkin.Frame("Frame_StageFrame_n_Blue"), UiSkin.Paper, Image.Type.Simple, false);
            var canvasGroup = card.gameObject.AddComponent<CanvasGroup>();

            RectTransform badge = UiBuildUtility.CreateNode("IndexBadge", card);
            UiBuildUtility.Place(badge, TopCenter, Center, new Vector2(0f, -78f), new Vector2(116f, 116f));
            UiBuildUtility.AddImage(badge, UiSkin.Button("Btn_OtherButton_Circle02"), UiSkin.Paper, Image.Type.Simple, false);
            RectTransform indexRect = UiBuildUtility.CreateNode("Index", badge);
            UiBuildUtility.Stretch(indexRect, 0f, 8f, 0f, 0f);
            TMP_Text indexText = UiBuildUtility.AddText(indexRect, slotNumber.ToString(), UiSkin.TitleFont, 62f, UiSkin.Ink, TextAlignmentOptions.Center);

            RectTransform nameRect = UiBuildUtility.CreateNode("Name", card);
            UiBuildUtility.Place(nameRect, TopCenter, Center, new Vector2(0f, -186f), new Vector2(410f, 64f));
            TMP_Text nameText = UiBuildUtility.AddText(nameRect, "LEVEL", UiSkin.BodyFont, 40f, UiSkin.Paper, TextAlignmentOptions.Center);

            RectTransform bestGroup = UiBuildUtility.CreateNode("BestScore", card);
            UiBuildUtility.Place(bestGroup, TopCenter, Center, new Vector2(0f, -272f), new Vector2(280f, 76f));
            UiBuildUtility.AddImage(bestGroup, UiSkin.Frame("Frame_ItemFrame03_Navy"), UiSkin.Paper, Image.Type.Sliced, false);
            RectTransform bestIcon = UiBuildUtility.CreateNode("Icon", bestGroup);
            UiBuildUtility.Place(bestIcon, MiddleLeft, Center, new Vector2(42f, 0f), new Vector2(46f, 46f));
            UiBuildUtility.AddImage(bestIcon, UiSkin.Icon("Pictoicon_Trophy_0"), UiSkin.Warning, Image.Type.Simple, false);
            RectTransform bestRect = UiBuildUtility.CreateNode("Value", bestGroup);
            UiBuildUtility.Place(bestRect, MiddleRight, MiddleRight, new Vector2(-24f, 0f), new Vector2(200f, 52f));
            TMP_Text bestText = UiBuildUtility.AddText(bestRect, "0", UiSkin.TitleFont, 38f, UiSkin.Paper, TextAlignmentOptions.MidlineRight);

            RectTransform playRect = UiBuildUtility.CreateNode("PlayButton", card);
            UiBuildUtility.Place(playRect, BottomCenter, Center, new Vector2(0f, 104f), new Vector2(320f, 126f));
            Image playBackground = UiBuildUtility.AddImage(playRect, UiSkin.Button("Btn_MainButton_Green"), UiSkin.Paper, Image.Type.Sliced, true);
            Button playButton = UiBuildUtility.AddButton(playRect, playBackground);
            AddPressFx(playRect);
            RectTransform playLabel = UiBuildUtility.CreateNode("Label", playRect);
            UiBuildUtility.Place(playLabel, Center, Center, new Vector2(0f, 4f), new Vector2(300f, 80f));
            UiBuildUtility.AddText(playLabel, "PLAY", UiSkin.TitleFont, 54f, UiSkin.Paper, TextAlignmentOptions.Center);

            RectTransform locked = UiBuildUtility.CreateNode("LockedBadge", card);
            UiBuildUtility.Stretch(locked, 0f, 0f, 0f, 0f);
            UiBuildUtility.AddImage(locked, null, LockedVeil, Image.Type.Simple, true);
            RectTransform lockIcon = UiBuildUtility.CreateNode("Icon", locked);
            UiBuildUtility.Place(lockIcon, Center, Center, Vector2.zero, new Vector2(150f, 150f));
            UiBuildUtility.AddImage(lockIcon, UiSkin.Icon("Pictoicon_Lock"), UiSkin.Paper, Image.Type.Simple, false);
            locked.gameObject.SetActive(false);

            var view = card.gameObject.AddComponent<LevelCardView>();
            UiBuildUtility.Bind(view,
                "_playButton", playButton,
                "_nameText", nameText,
                "_indexText", indexText,
                "_bestScoreText", bestText,
                "_lockedBadge", locked.gameObject,
                "_bestScoreGroup", bestGroup.gameObject,
                "_canvasGroup", canvasGroup,
                "_lockedAlpha", 0.55f);
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

        private static PopupManager BuildPopupCanvas(RectTransform root, out PausePopupUI pausePopup, out ResultPopupUI resultPopup)
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

            UiBuildUtility.Bind(manager,
                "_backdrop", backdrop,
                "_canvas", canvas,
                "_popups", new Object[] { pausePopup, resultPopup });
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

        private static int CountLevels()
        {
            string[] guids = AssetDatabase.FindAssets("t:LevelDefinitionSO", new[] { "Assets/_ZombieWar/Data/Levels" });
            if (guids.Length == 0)
            {
                Debug.LogError($"{LogPrefix} no LevelDefinitionSO found - the menu would have no cards.");
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
