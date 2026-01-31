using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Serialization;
using Il2CppTMPro;
using MelonLoader;
using SR2E;
using SR2E.Expansion;
using SR2MP.Bootstrap;
using UnityEngine.UI;

// Don't modify beyond this point - Finn
// I can and I will - Az
// How dare you - Finn
// Yeah sorry I also gotta - salt

// Don't modify beyond this point
// This was made for SR2EExpansionV3
// This is MLEntrypoint V2
[SuppressMessage("ReSharper", "InconsistentNaming")]
internal class MLEntrypoint : MelonMod
{
    static MLEntrypoint()
    {
        // We have to resolve the DLLs NOW otherwise everything will break
        AssemblyResolver.Install();
    }

    private SR2EExpansionV3 expansion;
    bool isCorrectSR2EInstalled = false;
    private string installedSR2Ver = "";

    private System.Collections.IEnumerator CheckForMainMenu(int message)
    {
        yield return new WaitForSeconds(0.1f);        
        
        if (SystemContext.Instance.SceneLoader.IsCurrentSceneGroupMainMenu()) ShowIncompatibilityPopup(message);
        else MelonCoroutines.Start(CheckForMainMenu(message));
    }

    void ShowIncompatibilityPopup(int message)
    {
        Time.timeScale = 0;
        GameObject canvas = new GameObject("SR2EExpansionICV1");
        GameObject.DontDestroyOnLoad(canvas);
        canvas.tag = "Finish";
        var c = canvas.AddComponent<Canvas>();
        canvas.AddComponent<CanvasScaler>();
        canvas.AddComponent<GraphicRaycaster>();
        c.sortingOrder = 20000;
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        
        var superPR = new GameObject("SuperBackground");
        superPR.transform.SetParent(canvas.transform);
        superPR.transform.localScale = new Vector3(1, 1, 1);
        superPR.transform.localPosition = new Vector3(0, 0, 0);
        superPR.transform.localRotation = Quaternion.identity;
        var rectTPR = superPR.AddComponent<RectTransform>();
        superPR.AddComponent<Image>().color = new Color(0.1059f, 0.1059f, 0.1137f, 1f);
        rectTPR.sizeDelta = new Vector2(Screen.currentResolution.width, Screen.currentResolution.height);
        
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
        titleTMP.text = "Mod Error occured!";
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
        Sprite pill = null!;
        try
        {
            var pillTex = Resources.FindObjectsOfTypeAll<AssetBundle>().FirstOrDefault((x) => x.name == "cc50fee78e6b7bdd6142627acdaf89fa.bundle")!.LoadAsset("Assets/UI/Textures/MenuDemo/whitePillBg.png").Cast<Texture2D>();
            pill = Sprite.Create(pillTex, new Rect(0f, 0f, (float)pillTex.width, (float)pillTex.height), new Vector2(0.5f, 0.5f), 1f);
        }catch { }
        var img = buttonObj.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        img.sprite = pill;
        var btn = buttonObj.AddComponent<Button>();
        
        GameObject textObj = new GameObject("Text");
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
                () => Application.OpenURL("https://github.com/ThatFinnDev/SR2E/releases"), "GitHub");
            AddButton(pill, pr, new Vector2(0.34f, 0.105f), new Vector2(0.6596f, 0.2f),
                () => Application.OpenURL("https://www.nexusmods.com/slimerancher2/mods/60"), "Nexusmods");
            AddButton(pill, pr, new Vector2(0.6666f, 0.105f), new Vector2(0.995f, 0.2f),
                () => Application.OpenURL("https://sr2e.sr2.dev/downloads"), "SR2E Website");
        }
        
        msgTMP.text = "An error occured with the mod <b>'" + BuildInfo.Name + "'</b>!\n\n";
        if (message == 0)
        {
            msgTMP.text += "In order to run the mod '" + BuildInfo.Name +
                           "', you need to have SR2E installed! Currently, you don't have it installed. You can download it either via Nexusmods, GitHub or the SR2E website.";
            btn.onClick.AddListener((System.Action)(() => Application.Quit()));
            tmp.text = "Quit";
        }
        else if (message == 1)
        {
            msgTMP.text += "In order to run the mod '" + BuildInfo.Name +
                           $"', you need a newer version of SR2E installed! A minimum of <b>SR2E {BuildInfo.MinSr2EVersion}</b> is required. You have <b>SR2E {installedSR2Ver}</b>.You can enable auto updating for SR2E in the Mod Menu. Alternatively, you can download it either via Nexusmods, GitHub or the SR2E website.";
            btn.onClick.AddListener((System.Action)(() =>
            {
                bool fixTime = true;
                foreach (var obj in GameObject.FindGameObjectsWithTag("Finish"))
                    if (obj.name.Contains("SR2EExpansionIC") && obj != canvas)
                    {
                        fixTime = false;
                        break;
                    }

                if (fixTime) Time.timeScale = 1f;
                GameObject.Destroy(canvas);
            }));
            tmp.text = "Continue without this mod";
        }
        else if (message == 2)
        {
            var gameVer = Application.version.Split(" ")[0];
            msgTMP.text += "In order to run the mod '" + BuildInfo.Name +
                           $"', you need update the game! A minimum of <b>{BuildInfo.RequiredGameVersion}</b> is required. You have <b>{gameVer}</b>.";
            btn.onClick.AddListener((System.Action)(() => Application.Quit()));
            tmp.text = "Quit";
        }
        else if (message == 3)
        {
            var gameVer = Application.version.Split(" ")[0];
            msgTMP.text += "In order to run the mod '" + BuildInfo.Name +
                           $"', you need to use a different game version! The game version <b>{BuildInfo.RequiredGameVersion}</b> is required. You have <b>{gameVer}</b>.";
            btn.onClick.AddListener((System.Action)(() => Application.Quit()));
            tmp.text = "Quit";
        }
    }

    ColorBlock buttonColorBlock
    {
        get
        {
            var block = new ColorBlock();
            block.normalColor = new Color(0.149f, 0.7176f, 0.7961f, 1f);
            block.highlightedColor = new Color(0.1098f, 0.2314f, 0.4157f, 1f);
            block.pressedColor = new Color(0.1371f, 0.5248f, 0.6792f, 1f);
            block.selectedColor = new Color(0.8706f, 0.3098f, 0.5216f, 1f);
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
        
        GameObject textObj = new GameObject("Text");
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
        if (!string.IsNullOrWhiteSpace(BuildInfo.RequiredGameVersion))
        {
            if (!IsSameOrNewer(BuildInfo.RequiredGameVersion, gameVer))
            {
                MelonLogger.Msg("The game's version too old, aborting!");
                MelonCoroutines.Start(CheckForMainMenu(2));
            }
        }
        else if (!string.IsNullOrWhiteSpace(BuildInfo.ExactRequiredGameVersion))
        {
            if (BuildInfo.ExactRequiredGameVersion != gameVer)
            {
                MelonLogger.Msg($"The game version is not version {BuildInfo.ExactRequiredGameVersion}!");
                MelonCoroutines.Start(CheckForMainMenu(3));
            }
        }
        foreach (var melonBase in MelonBase.RegisteredMelons)
            if (melonBase.Info.Name == "SR2E")
            {
                isCorrectSR2EInstalled = true;
                installedSR2Ver = melonBase.Info.Version;
            }

        if (isCorrectSR2EInstalled)
        {
            if (IsSameOrNewer(BuildInfo.MinSr2EVersion, installedSR2Ver))
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

        try { RegisterBrokenInSR2E("Requires SR2E " + BuildInfo.MinSr2EVersion + " or newer!"); }catch { }
        Unregister();
    }

    void RegisterBrokenInSR2E(string errorMessage)
    {
        var SR2EEntryPoint = System.Type.GetType("SR2E.SR2EEntryPoint, SR2E");
        if (SR2EEntryPoint == null) return;
        var AddBrokenExpansion = SR2EEntryPoint.GetMethod("AddBrokenExpansion",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        if (AddBrokenExpansion == null) return;
        AddBrokenExpansion.Invoke(null,
            new object[]
                { Info.Name, Info.Author, Info.Version, Info.DownloadLink, MelonAssembly.Assembly, errorMessage });
    }

    bool IsSameOrNewer(string v1, string v2)
    {
        bool TryParse(string s, out int[] parts)
        {
            parts = null!;
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

    #nullable disable
    void OnSR2EInstalled()
    {
        var type = GetEntrypointType.type;
        if (typeof(SR2EExpansionV3).IsAssignableFrom(type))
        {
            expansion = FormatterServices.GetUninitializedObject(type) as SR2EExpansionV3;
        }
        else
        {
            MelonLogger.Error("Main class is not a " + nameof(SR2EExpansionV3) + "!");
            try { RegisterBrokenInSR2E("Main class is not a " + nameof(SR2EExpansionV3) + "!"); }catch { }

            Unregister();
            return;
        }

        typeof(SR2EExpansionV3).GetField("_melonBase", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(expansion, this);
        SR2EEntryPoint.LoadExpansion(expansion);
    }

    void Sr2EDeinit() => expansion.OnDeinitializeMelon();

    public override void OnDeinitializeMelon()
    {
        if (isCorrectSR2EInstalled) Sr2EDeinit();
    }

}
