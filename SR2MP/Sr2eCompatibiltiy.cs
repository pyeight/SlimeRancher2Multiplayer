using System.Reflection;
using System.Runtime.Serialization;
using Il2CppTMPro;
using MelonLoader;
using SR2E;
using SR2E.Expansion;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

// Don't modify beyond this point - Finn
// I can and I will - Az

// This was made for SR2EExpansionV3
// This is MLEntrypoint V1
#pragma warning disable RCS1110 // Declare type inside namespace
internal class MLEntrypoint : MelonMod
#pragma warning restore RCS1110 // Declare type inside namespace
{
    private static readonly ColorBlock buttonColorBlock = new()
    {
        normalColor = new Color(0.149f, 0.7176f, 0.7961f, 1f),
        highlightedColor = new Color(0.1098f, 0.2314f, 0.4157f, 1f),
        pressedColor = new Color(0.1371f, 0.5248f, 0.6792f, 1f),
        selectedColor = new Color(0.8706f, 0.3098f, 0.5216f, 1f),
        disabledColor = new Color(0.8706f, 0.3098f, 0.5216f, 1f),
        colorMultiplier = 1f,
        fadeDuration = 0.1f
    };

    private SR2EExpansionV3 expansion;
    private bool isCorrectSR2EInstalled = false;
    private string installedSR2Ver = string.Empty;

    public override void OnInitializeMelon()
    {
        foreach (var melonBase in MelonBase.RegisteredMelons)
        {
            if (melonBase.Info.Name == "SR2E")
            {
                isCorrectSR2EInstalled = true;
                installedSR2Ver = melonBase.Info.Version;
            }
        }

        if (isCorrectSR2EInstalled)
        {
            if (IsSameOrNewer(BuildInfo.MinSR2EVersion, installedSR2Ver))
            {
                OnSR2EInstalled();
                return;
            }

            isCorrectSR2EInstalled = false;
            MelonLogger.Msg("SR2E is too old, aborting!");
            MelonCoroutines.Start(CheckForMainMenu(1));
        }
        else
        {
            MelonLogger.Msg("SR2E is not installed, aborting!");
            MelonCoroutines.Start(CheckForMainMenu(0));
        }

        try
        {
            RegisterBrokenInSR2E("Requires SR2E " + BuildInfo.MinSR2EVersion + " or newer!");
        }
        catch { }

        Unregister();
    }

    public override void OnDeinitializeMelon()
    {
        if (isCorrectSR2EInstalled)
            SR2EDeinit();
    }

    private IEnumerator CheckForMainMenu(int message)
    {
        yield return new WaitForSeconds(0.1f);

        var hasMainMenuUI = false;

        for (var i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);

            if (scene.name.Contains("MainMenu") && scene.isLoaded)
            {
                hasMainMenuUI = true;
                break;
            }
        }

        if (hasMainMenuUI)
            ShowIncompatibilityPopup(message);
        else
            MelonCoroutines.Start(CheckForMainMenu(message));
    }

    private void ShowIncompatibilityPopup(int message)
    {
        Time.timeScale = 0;

        var canvasObj = new GameObject("SR2EExpansionICV1") { tag = "Finish" };
        Object.DontDestroyOnLoad(canvasObj);

        var canvas = canvasObj.AddComponent<Canvas>();

        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        canvas.sortingOrder = 20000;
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var superBG = CreateBackground("SuperBackground", canvasObj, new Color(0.1059f, 0.1059f, 0.1137f, 1f), 1f);
        var background = CreateBackground("Background", canvasObj, new Color(0.1882f, 0.2196f, 0.2745f, 1f), 1.23f);

        var titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(background.transform);

        var titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.sizeDelta = new Vector2(0, Screen.currentResolution.height * 0.1f);
        titleRect.anchoredPosition = new Vector2(0, 0);

        var titleTMP = titleObj.AddComponent<TextMeshProUGUI>();
        titleTMP.text = "Mod Error occurred!";
        titleTMP.enableAutoSizing = true;
        titleTMP.fontSizeMin = 0;
        titleTMP.color = Color.white;
        titleTMP.fontSizeMax = 9999;
        titleTMP.alignment = TextAlignmentOptions.Center;

        var msgObj = new GameObject("MessageText");
        msgObj.transform.SetParent(background.transform);

        var msgRect = msgObj.AddComponent<RectTransform>();
        msgRect.anchorMin = new Vector2(0.005f, 0.1f);
        msgRect.anchorMax = new Vector2(0.995f, 0.8f);
        msgRect.pivot = new Vector2(0.5f, 0.5f);
        msgRect.offsetMin = Vector2.zero;
        msgRect.offsetMax = Vector2.zero;

        var msgTMP = msgObj.AddComponent<TextMeshProUGUI>();
        msgTMP.fontSize = 24;
        msgTMP.alignment = TextAlignmentOptions.TopLeft;
        msgTMP.enableWordWrapping = true;
        msgTMP.color = Color.white;

        var buttonObj = new GameObject("Button");
        buttonObj.transform.SetParent(background.transform, false);

        var buttonRect = buttonObj.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.005f, 0.005f);
        buttonRect.anchorMax = new Vector2(0.995f, 0.1f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.offsetMin = Vector2.zero;
        buttonRect.offsetMax = Vector2.zero;

        Sprite pill = null!;

        try
        {
            var pillTex = Resources.FindObjectsOfTypeAll<AssetBundle>()
                .FirstOrDefault(x => x.name == "cc50fee78e6b7bdd6142627acdaf89fa.bundle")!
                .LoadAsset<Texture2D>("Assets/UI/Textures/MenuDemo/whitePillBg.png");
            pill = Sprite.Create(pillTex, new Rect(0f, 0f, pillTex.width, pillTex.height), new Vector2(0.5f, 0.5f), 1f);
        }
        catch { }

        var img = buttonObj.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        img.sprite = pill;

        var btn = buttonObj.AddComponent<Button>();
        btn.colors = buttonColorBlock;

        var textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);

        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 36;
        tmp.color = Color.white;

        var textRect = tmp.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        AddButton(pill, background, new Vector2(0.005f, 0.105f), new Vector2(0.3333f, 0.2f), "https://github.com/ThatFinnDev/SR2E/releases", "GitHub");
        AddButton(pill, background, new Vector2(0.34f, 0.105f), new Vector2(0.6596f, 0.2f), "https://www.nexusmods.com/slimerancher2/mods/60", "Nexusmods");
        AddButton(pill, background, new Vector2(0.6666f, 0.105f), new Vector2(0.995f, 0.2f), "https://sr2e.sr2.dev/downloads", "SR2E Website");

        msgTMP.text = "An error occurred with the mod <b>'" + BuildInfo.Name + "'</b>!\n\n";

        if (message == 0)
        {
            msgTMP.text += "In order to run the mod '" + BuildInfo.Name + "', you need to have SR2E installed! Currently, you don't have it installed. You can download it either via Nexusmods, GitHub or the SR2E website.";
            btn.onClick.AddListener((Action)Application.Quit);
            tmp.text = "Quit";
        }
        else if (message == 1)
        {
            msgTMP.text += "In order to run the mod '" + BuildInfo.Name + $"', you need a newer version of SR2E installed! A minimum of <b>SR2E {BuildInfo.MinSR2EVersion}</b> is required. You have <b>SR2E {installedSR2Ver}</b>.You can enable auto updating for SR2E in the Mod Menu. Alternatively, you can download it either via Nexusmods, GitHub or the SR2E website.";
            btn.onClick.AddListener((Action)(() =>
            {
                var fixTime = true;

                foreach (var obj in GameObject.FindGameObjectsWithTag("Finish"))
                {
                    if (!obj.name.Contains("SR2EExpansionIC") || obj == canvasObj)
                        continue;

                    fixTime = false;
                    break;
                }

                if (fixTime)
                    Time.timeScale = 1f;

                Object.Destroy(canvasObj);
            }));
            tmp.text = "Continue without this mod";
        }
    }

    private static GameObject CreateBackground(string name, GameObject canvasObj, Color imgColor, float widthFactor)
    {
        var background = new GameObject(name);
        background.transform.SetParent(canvasObj.transform);
        background.transform.localScale = new Vector3(1, 1, 1);
        background.transform.localPosition = new Vector3(0, 0, 0);
        background.transform.localRotation = Quaternion.identity;
        background.AddComponent<Image>().color = imgColor;
        background.AddComponent<RectTransform>().sizeDelta = new Vector2(Screen.currentResolution.width, Screen.currentResolution.height) / widthFactor;
        return background;
    }

    private static void AddButton(Sprite pill, GameObject parent, Vector2 anchorMin, Vector2 anchorMax, string link, string text)
    {
        var buttonObj = new GameObject(text.Replace(' ', '_') + "_Button");
        buttonObj.transform.SetParent(parent.transform, false);

        var buttonRect = buttonObj.AddComponent<RectTransform>();
        buttonRect.anchorMin = anchorMin;
        buttonRect.anchorMax = anchorMax;
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.offsetMin = Vector2.zero;
        buttonRect.offsetMax = Vector2.zero;

        var img = buttonObj.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        img.sprite = pill;

        var btn = buttonObj.AddComponent<Button>();
        btn.colors = buttonColorBlock;

        var textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);

        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 36;
        tmp.color = Color.white;
        tmp.text = text;

        var textRect = tmp.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        btn.onClick.AddListener((Action)(() => Application.OpenURL(link)));
    }

    private void RegisterBrokenInSR2E(string errorMessage)
    {
        var sR2EEntryPoint = Type.GetType("SR2E.SR2EEntryPoint, SR2E");

        if (sR2EEntryPoint == null)
            return;

        var addBrokenExpansion = sR2EEntryPoint.GetMethod("AddBrokenExpansion", BindingFlags.Static | BindingFlags.NonPublic);
        addBrokenExpansion?.Invoke(null, new object[] { Info.Name, Info.Author, Info.Version, Info.DownloadLink, MelonAssembly.Assembly, errorMessage });
    }

    private static bool IsSameOrNewer(string v1, string v2)
    {
        if (!TryParse(v1, out var a) || !TryParse(v2, out var b))
            return false;

        for (var i = 0; i < 3; i++)
        {
            if (b[i] > a[i])
                return true;

            if (b[i] < a[i])
                return false;
        }

        return true;
    }

    private static bool TryParse(string s, out int[] parts)
    {
        parts = null!;
        var split = s.Split('.');

        if (split.Length != 3)
            return false;

        parts = new int[3];

        for (var i = 0; i < 3; i++)
        {
            if (!int.TryParse(split[i], out parts[i]) || parts[i] < 0)
                return false;
        }

        return true;
    }

    private void OnSR2EInstalled()
    {
        var type = GetEntrypointType.type;

        if (typeof(SR2EExpansionV3).IsAssignableFrom(type))
            expansion = (SR2EExpansionV3)FormatterServices.GetUninitializedObject(type);
        else
        {
            MelonLogger.Error("Main class is not a " + nameof(SR2EExpansionV3) + "!");

            try
            {
                RegisterBrokenInSR2E("Main class is not a " + nameof(SR2EExpansionV3) + "!");
            }
            catch { }

            Unregister();
            return;
        }

        typeof(SR2EExpansionV3).GetField("_melonBase", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(expansion, this);
        SR2EEntryPoint.LoadExpansion(expansion);
    }

    private void SR2EDeinit() => expansion.OnDeinitializeMelon();
}