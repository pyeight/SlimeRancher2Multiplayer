using Il2CppTMPro;
using MelonLoader;

namespace SR2MP.Components.Utils;

[RegisterTypeInIl2Cpp(false)]
public sealed class TransformLookAtCamera : MonoBehaviour
{
    public Transform targetTransform;

    private bool isText;

    private Camera playerCamera;

    private void Start() => isText = targetTransform.GetComponent<TextMeshPro>();

    private void Update()
    {
        if (!playerCamera)
        {
            playerCamera = SceneContext.Instance?.Camera.GetComponent<Camera>()!;
            return;
        }
        if (!targetTransform)
            return;

        targetTransform.LookAt(playerCamera.transform);

        if (isText)
        {
            targetTransform.Rotate(0, 180, 0);
        }
    }
}