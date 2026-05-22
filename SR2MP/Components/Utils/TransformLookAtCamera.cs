using Il2CppTMPro;
using MelonLoader;
using Starlight.Storage;

namespace SR2MP.Components.Utils;

[InjectIntoIL]
internal sealed class TransformLookAtCamera : MonoBehaviour
{
    public Transform TargetTransform;

    private bool isText;

    private Camera playerCamera;

    public void Start() => isText = TargetTransform.GetComponent<TextMeshPro>();

    public void Update()
    {
        if (!playerCamera)
        {
            playerCamera = SceneContext.Instance?.Camera.GetComponent<Camera>()!;
            return;
        }

        if (!TargetTransform)
            return;

        TargetTransform.LookAt(playerCamera.transform);

        if (isText)
            TargetTransform.Rotate(0, 180, 0);
    }
}