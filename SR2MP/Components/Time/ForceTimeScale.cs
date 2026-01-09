using MelonLoader;

namespace SR2MP.Components.Time;

[RegisterTypeInIl2Cpp(false)]
public sealed class ForceTimeScale : MonoBehaviour
{
    public float timeScale = 1f;
    public float loadingTimeScale;

    private void Update()
    {
        if (!Main.Server.IsRunning() && !Main.Client.IsConnected)
            return;

        if (GameContext.Instance.InputDirector._paused.Map.enabled)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        var loading = SystemContext.Instance.SceneLoader.IsSceneLoadInProgress;

        UnityEngine.Time.timeScale = loading ? loadingTimeScale : timeScale;
    }
}