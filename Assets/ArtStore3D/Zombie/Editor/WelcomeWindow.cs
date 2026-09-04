using UnityEngine;
using UnityEditor;

namespace YourPackage.Editor
{
    [InitializeOnLoad]
    public class WelcomeWindow : EditorWindow
    {

static WelcomeWindow()
        {
            EditorApplication.update += RunOnce;
        }
        static void RunOnce()
        {
            EditorApplication.update -= RunOnce;
            
            if (EditorPrefs.GetBool("ShowWelcomeWindow", true))
            {
                Open();
            }
        }

        [MenuItem("Tools/ArtStore3D/Welcome")]
        public static void Open()
        {
            WelcomeWindow window = GetWindow<WelcomeWindow>("Welcome");
            window.minSize = new Vector2(450, 600);
            window.Show();
        }


        Texture2D banner, tonIcon, usdtIcon;
        Vector2 scroll;

        void OnEnable()
        {
            banner = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ArtStore3D/Zombie/Editor/Images/ArtStore3D.png");
            tonIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ArtStore3D/Zombie/Editor/Images/Ton.png");
            usdtIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ArtStore3D/Zombie/Editor/Images/USDTRC20.png");
      Debug.Log("<color=green><b>Thank you for using my asset!</b></color> If you like it, please take a moment to rate it on the Asset Store. It helps me a lot!");
        }

        void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);

            if (banner) GUILayout.Box(banner, GUILayout.ExpandWidth(true), GUILayout.Height(200));

            GUILayout.Space(10);

            var titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 20, alignment = TextAnchor.MiddleCenter };
            titleStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
            GUILayout.Label("ArtStore3D", titleStyle);

            GUILayout.Space(10);

            var descStyle = new GUIStyle(EditorStyles.label) { wordWrap = true, alignment = TextAnchor.UpperCenter };
            descStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
            GUILayout.Label("Creating this asset was a labor of love, and I’m excited to share it with you for free! Providing high-quality tools to the community is my passion. If this pack helps your project, a positive review on the Asset Store would mean the world to me and helps me keep creating more free content for you", descStyle);

            GUILayout.Space(20);

            if (GUILayout.Button("AssetStore", GUILayout.Height(30))) Application.OpenURL("https://assetstore.unity.com/publishers/71551");
            if (GUILayout.Button("Artstation", GUILayout.Height(30))) Application.OpenURL("https://www.artstation.com/artstore_3d");
            if (GUILayout.Button("Contact me", GUILayout.Height(30))) Application.OpenURL("mailto:dragonnogard82@gmail.com");

            GUILayout.Space(20);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.Label("Support the Development", titleStyle);

            //
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

string supportText = "Your support ensures the continued growth and quality of this project. If you find this asset valuable, here are a few ways to assist our development:\n\n" +
                     "• Feedback: Please leave a rating and review on the Asset Store.\n" +
                     "• Discover: Check out our other available assets\n" +
                     "• Contribute: Consider supporting us via the donation links below to fuel future updates\n" +
                     "• Thank you for your professional support";
                    
        var textStyle = new GUIStyle(EditorStyles.label) { wordWrap = true, fontSize = 12 };
        GUILayout.Label(supportText, textStyle);

            //

            GUILayout.Space(20);
            
            if (GUILayout.Button("Support by Rating & Reviewing", GUILayout.Height(30)))
                Application.OpenURL("https://u3d.as/2UU9");

            GUILayout.Space(20);
            GUILayout.Label("Crypto Donations:", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();
            
            DrawCryptoWallet("TON Coin", "Network: TON", "UQDiaDuI_8mQmUnsAKtzLnmjFUymU1eFVxqERDmSQrc-9cZT", tonIcon);
            DrawCryptoWallet("USDT", "Network: TRC20", "TNPVwcvfXqPcYQnQH634cAjZFhihw6SzYW", usdtIcon);

            GUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();

            //
        
            bool showOnStartup = EditorPrefs.GetBool("ShowWelcomeWindow", true);
            bool newShow = EditorGUILayout.Toggle("Show Welcome Screen on Startup", showOnStartup);

            if (newShow != showOnStartup)
            {
                EditorPrefs.SetBool("ShowWelcomeWindow", newShow);
            }
            
            //
        }

        void DrawCryptoWallet(string title, string network, string address, Texture2D icon)
        {
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(210)); 
            
            if (icon) GUILayout.Label(icon, GUILayout.Width(200), GUILayout.Height(200));
            else GUILayout.Box("No Image", GUILayout.Width(200), GUILayout.Height(200));
            
            GUILayout.Label(title, EditorStyles.boldLabel);
            GUILayout.Label(network, EditorStyles.miniLabel);
            
            EditorGUILayout.SelectableLabel(address, EditorStyles.textField, GUILayout.Height(20));
            
            if (GUILayout.Button("Copy Address", GUILayout.Height(25)))
            {
                EditorGUIUtility.systemCopyBuffer = address;
                Debug.Log("Copied: " + address);
            }
            
            GUILayout.EndVertical();
        }
    }
    }
