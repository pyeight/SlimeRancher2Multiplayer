using System.Diagnostics.CodeAnalysis; using System.IO; using System.Reflection; using Il2CppTMPro; using MelonLoader; using UnityEngine.SceneManagement; using UnityEngine.UI; [assembly: HarmonyDontPatchAll()] [assembly: AssemblyMetadata(Starlight.Expansion.StarlightModInfoAttributes.MinimumStarlightVersion, OptionFileInfo.MinimumStarlightVersion)]
[assembly: MelonGame("MonomiPark", "SlimeRancher2")]
// Conversation:
    // Don't modify beyond this point - Finn
    // I can and I will - Az
    // How dare you - Finn
    // Why did you mess up the formatting again :( - Az
    // Are you happy with this one? - Finn


// This is an optional file V1. You can add into your expansion
// This will show an error message to the user, if Starlight isn't installed!
// This also allows you to provide a required game or Starlight version

// [assembly: MelonInfo(typeof(OptionFileEntrypoint),  
//     //Those infos are only shown by MelonLoader in the console when starting up
//     //They don't have to match the real ones.
//     BuildInfo.Name, //Put in the Expansions name
//     BuildInfo.MelonVersion,  //Put in the version
//     BuildInfo.Author //Put in your name
//     )]
[SuppressMessage("ReSharper", "CheckNamespace")]
static class OptionFileInfo
{
    internal const string MinimumGameVersion = BuildInfo.MinimumGameVersion; //e.g 1.1.0 or something similar (optional)
    internal const string ExactGameVersion = BuildInfo.ExactGameVersion; //e.g 1.1.0 or something similar (optional)
    internal const string MinimumStarlightVersion = BuildInfo.MinimumStarlightVersion; //e.g 4.0.0 put in the minimum required version of Starlight (optional)
}

// DON'T MODIFY BEYOND THIS POINT
// Leave this as is
[SuppressMessage("ReSharper", "CheckNamespace")]
class OptionFileEntrypoint : MelonMod
{
    private bool isCorrectStarlightInstalled;
    private string installedSr2Ver = "";

    private System.Collections.IEnumerator CheckForMainMenu(int message)
    {
        yield return new WaitForSeconds(0.1f);
        var hasMainMenuUI = false;
        for (int i = 0; i < SceneManager.sceneCount; i++) { var scene = SceneManager.GetSceneAt(i); if (scene.name.Contains("MainMenu") && scene.isLoaded) { hasMainMenuUI = true; break; } }
        if (hasMainMenuUI) ShowIncompatibilityPopup(message);
        else MelonCoroutines.Start(CheckForMainMenu(message));
    }
    void ShowIncompatibilityPopup(int message)
    {
        Time.timeScale = 0;
        var canvas = new GameObject("StarlightExpansionICV1");
        Object.DontDestroyOnLoad(canvas);
        canvas.tag = "Finish";
        var c = canvas.AddComponent<Canvas>();
        canvas.AddComponent<CanvasScaler>();
        canvas.AddComponent<GraphicRaycaster>();
        c.sortingOrder = 20000;
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        
        var superPr = new GameObject("SuperBackground");
        superPr.transform.SetParent(canvas.transform);
        superPr.transform.localScale = new Vector3(1, 1, 1);
        superPr.transform.localPosition = new Vector3(0, 0, 0);
        superPr.transform.localRotation = Quaternion.identity;
        var rectTpr = superPr.AddComponent<RectTransform>();
        superPr.AddComponent<Image>().color = new Color(0.1059f, 0.1059f, 0.1137f, 1f);
        rectTpr.sizeDelta = new Vector2(Screen.currentResolution.width, Screen.currentResolution.height);
        
        var pr = new GameObject("Background");
        pr.transform.SetParent(canvas.transform);
        pr.transform.localScale = new Vector3(1, 1, 1);
        pr.transform.localPosition = new Vector3(0, 0, 0);
        pr.transform.localRotation = Quaternion.identity;
        var rectT = pr.AddComponent<RectTransform>();
        pr.AddComponent<Image>().color = new Color(0.1882f, 0.2196f, 0.2745f, 1f);
        rectT.sizeDelta = new Vector2(Screen.currentResolution.width / 1.23f, Screen.currentResolution.height / 1.23f);
        
        var titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(pr.transform);
        var titleRT = titleObj.AddComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 1);
        titleRT.anchorMax = new Vector2(1, 1);
        titleRT.pivot = new Vector2(0.5f, 1);
        titleRT.sizeDelta = new Vector2(0, Screen.currentResolution.height * 0.1f);
        titleRT.anchoredPosition = new Vector2(0, 0);
        var titleTMP = titleObj.AddComponent<TextMeshProUGUI>();
        titleTMP.text = "Expansion Error occured!";
        titleTMP.enableAutoSizing = true;
        titleTMP.fontSizeMin = 0;
        titleTMP.color = Color.white;
        titleTMP.fontSizeMax = 9999;
        titleTMP.alignment = TextAlignmentOptions.Center;
        
        var msgObj = new GameObject("MessageText");
        msgObj.transform.SetParent(pr.transform);
        var msgRT = msgObj.AddComponent<RectTransform>();
        msgRT.anchorMin = new Vector2(0.005f, 0.1f);
        msgRT.anchorMax = new Vector2(0.995f, 0.8f);
        msgRT.pivot = new Vector2(0.5f, 0.5f);
        msgRT.offsetMin = Vector2.zero;
        msgRT.offsetMax = Vector2.zero;
        var msgTMP = msgObj.AddComponent<TextMeshProUGUI>();
        msgTMP.fontSize = 24;
        msgTMP.alignment = TextAlignmentOptions.TopLeft;
        msgTMP.enableWordWrapping = true;
        msgTMP.color = Color.white;
        
        var buttonObj = new GameObject("Button");
        buttonObj.transform.SetParent(pr.transform, false);
        var quitRT = buttonObj.AddComponent<RectTransform>();
        quitRT.anchorMin = new Vector2(0.005f, 0.005f);
        quitRT.anchorMax = new Vector2(0.995f, 0.1f);
        quitRT.pivot = new Vector2(0.5f, 0.5f);
        quitRT.offsetMin = Vector2.zero;
        quitRT.offsetMax = Vector2.zero;
        Sprite pill = null;
        try
        {
            Texture2D pillTex = null;
            foreach (var bundle in Il2CppAssetBundleManager.GetAllLoadedAssetBundles())
                try
                {
                    Texture2D tex = bundle.LoadAsset("Assets/UI/Textures/MenuDemo/whitePillBg.png").Cast<Texture2D>();
                    if (tex == null) continue;
                    pillTex = tex;
                } catch { /* ignored */ }
            pill = Sprite.Create(pillTex, new Rect(0f, 0f, pillTex.width, pillTex.height),
                new Vector2(0.5f, 0.5f), 1f);
        }
        catch { /* ignored */ }
        
        var img = buttonObj.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        img.sprite = pill;
        var btn = buttonObj.AddComponent<Button>();
        
        var textObj = new GameObject("Text");
        btn.colors = buttonColorBlock;
        textObj.transform.SetParent(buttonObj.transform, false);
        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 36;
        tmp.color = Color.white;
        var textRT = tmp.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        if (message == 0 || message == 1)
        {
            AddButton(pill, pr, new Vector2(0.005f, 0.105f), new Vector2(0.3333f, 0.2f),
                () => Application.OpenURL("https://github.com/ThatFinnDev/Starlight/releases"), "GitHub");
            AddButton(pill, pr, new Vector2(0.34f, 0.105f), new Vector2(0.6596f, 0.2f),
                () => Application.OpenURL("https://www.nexusmods.com/slimerancher2/mods/60"), "Nexusmods");
            AddButton(pill, pr, new Vector2(0.6666f, 0.105f), new Vector2(0.995f, 0.2f),
                () => Application.OpenURL("https://starlight.sr2.dev/downloads"), "Starlight Website");
        }

        var name = new FileInfo(MelonAssembly.Assembly.Location).Name;
        msgTMP.text = "An error occured with the expansion <b>'" + name + "'</b>!\n\n";
        if (message == 0)
        {
            msgTMP.text += "In order to run the expansion '" + name + "', you need to have Starlight installed! Currently, you don't have it installed. You can download it either via Nexusmods, GitHub or the Starlight website.";
            btn.onClick.AddListener((System.Action)(Application.Quit));
            tmp.text = "Quit";
        }
        else if (message == 1)
        {
            msgTMP.text += "In order to run the expansion '" + name + $"', you need a newer version of Starlight installed! A minimum of <b>Starlight {OptionFileInfo.MinimumStarlightVersion}</b> is required. You have <b>Starlight {installedSr2Ver}</b>.You can enable auto updating for Starlight in the Mod Menu. Alternatively, you can download it either via Nexusmods, GitHub or the Starlight website.";
            btn.onClick.AddListener((System.Action)(() =>
            {
                bool fixTime = true;
                foreach (var obj in GameObject.FindGameObjectsWithTag("Finish"))
                    if (obj.name.Contains("StarlightExpansionIC") && obj != canvas)
                    {
                        fixTime = false;
                        break;
                    }
                if (fixTime) Time.timeScale = 1f;
                Object.Destroy(canvas);
            }));
            tmp.text = "Continue without this expansion";
        }
        else if (message == 2)
        {
            var gameVer = Application.version.Split(" ")[0];
            msgTMP.text += "In order to run the expansion '" + name +
                           $"', you need update the game! A minimum of <b>{OptionFileInfo.MinimumGameVersion}</b> is required. You have <b>{gameVer}</b>.";
            btn.onClick.AddListener((System.Action)(Application.Quit));
            tmp.text = "Quit";
        }
        else if (message == 3)
        {
            var gameVer = Application.version.Split(" ")[0];
            msgTMP.text += "In order to run the expansion '" + name +
                           $"', you need to use a different game version! The exact game version <b>{OptionFileInfo.ExactGameVersion}</b> is required. You have <b>{gameVer}</b>.";
            btn.onClick.AddListener((System.Action)(Application.Quit));
            tmp.text = "Quit";
        }
    }

    ColorBlock buttonColorBlock
    {
        get
        {
            var block = new ColorBlock
            {
                normalColor = new Color(0.149f, 0.7176f, 0.7961f, 1f),
                highlightedColor = new Color(0.1098f, 0.2314f, 0.4157f, 1f),
                pressedColor = new Color(0.1371f, 0.5248f, 0.6792f, 1f),
                selectedColor = new Color(0.8706f, 0.3098f, 0.5216f, 1f),
            };
            block.disabledColor = block.selectedColor;
            block.colorMultiplier = 1f;
            block.fadeDuration = 0.1f;
            return block;
        }
    }

    void AddButton(Sprite pill, GameObject pr, Vector2 anchorMin, Vector2 anchorMax, System.Action action, string text)
    {
        var buttonObj = new GameObject("Button");
        buttonObj.transform.SetParent(pr.transform, false);
        var quitRT = buttonObj.AddComponent<RectTransform>();
        quitRT.anchorMin = anchorMin;
        quitRT.anchorMax = anchorMax;
        quitRT.pivot = new Vector2(0.5f, 0.5f);
        quitRT.offsetMin = Vector2.zero;
        quitRT.offsetMax = Vector2.zero;
        
        var img = buttonObj.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        img.sprite = pill;
        var btn = buttonObj.AddComponent<Button>();
        
        var textObj = new GameObject("Text");
        btn.colors = buttonColorBlock;
        textObj.transform.SetParent(buttonObj.transform, false);
        
        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 36;
        tmp.color = Color.white;
        
        var textRT = tmp.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;
        tmp.text = text;
        btn.onClick.AddListener(action);
    }

    public override void OnInitializeMelon()
    {
        var gameVer = Application.version.Split(" ")[0];
        if (!string.IsNullOrWhiteSpace(OptionFileInfo.MinimumGameVersion))
        {
            if (!IsSameOrNewer(OptionFileInfo.MinimumGameVersion, gameVer))
            {
                LogError("The game's version too old, aborting!");
                MelonCoroutines.Start(CheckForMainMenu(2));
            }
        }
        else if (!string.IsNullOrWhiteSpace(OptionFileInfo.ExactGameVersion))
        {
            if (OptionFileInfo.ExactGameVersion != gameVer)
            {
                MelonLogger.Msg($"The game version is not version {OptionFileInfo.ExactGameVersion}!");
                MelonCoroutines.Start(CheckForMainMenu(3));
            }
        }
        foreach (var melonBase in RegisteredMelons)
            if (melonBase.Info.Name == "Starlight"||melonBase.Info.Name == "Starlight Core Essentials")
            {
                isCorrectStarlightInstalled = true;
                installedSr2Ver = melonBase.Info.Version;
            }

        if (isCorrectStarlightInstalled)
        {
            if (string.IsNullOrWhiteSpace(OptionFileInfo.MinimumStarlightVersion)||IsSameOrNewer(OptionFileInfo.MinimumStarlightVersion, installedSr2Ver)) return;
            isCorrectStarlightInstalled = false;
            MelonLogger.Msg("Starlight is too old, aborting!");
            MelonCoroutines.Start(CheckForMainMenu(1));
        }
        else
        {
            MelonLogger.Msg("Starlight is not installed, aborting!");
            MelonCoroutines.Start(CheckForMainMenu(0));
        }
    }
    
    bool IsSameOrNewer(string v1, string v2)
    {
        bool TryParse(string s, out int[] parts)
        {
            parts = null;
            var split = s.Split('.');
            if (split.Length != 3) return false;
            parts = new int[3];
            for (int i = 0; i < 3; i++)
                if (!int.TryParse(split[i], out parts[i]) || parts[i] < 0)
                    return false;
            return true;
        }

        if (!TryParse(v1, out var a) || !TryParse(v2, out var b)) return false;
        for (int i = 0; i < 3; i++)
        {
            if (b[i] > a[i]) return true;
            if (b[i] < a[i]) return false;
        }

        return true;
    }

}
