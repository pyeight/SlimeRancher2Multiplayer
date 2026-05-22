using MelonLoader;
using Starlight.Storage;

namespace SR2MP.Components.Time;

[InjectIntoIL]
internal sealed class ForceTimeScale : MonoBehaviour
{
    public float TimeScale = 1f;
    public float LoadingTimeScale;

    public void Update()
    {
        if (!Main.Server.IsRunning && !Main.Client.IsConnected)
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

        UnityEngine.Time.timeScale = loading ? LoadingTimeScale : TimeScale;
    }
}