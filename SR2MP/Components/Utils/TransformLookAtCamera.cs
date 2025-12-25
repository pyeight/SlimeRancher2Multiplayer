using Il2Cpp;
using Il2CppTMPro;
using MelonLoader;

namespace SR2MP.Components.Utils;

[RegisterTypeInIl2Cpp(false)]
public class TransformLookAtCamera : MonoBehaviour
{
    public Transform targetTransform;

    private bool isText;

    private Camera playerCamera;

    void Start() => isText = targetTransform.GetComponent<TextMeshPro>();
    void Update()
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