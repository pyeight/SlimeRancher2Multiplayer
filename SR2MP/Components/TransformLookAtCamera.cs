using MelonLoader;
using UnityEngine;

namespace SR2MP.Components;

[RegisterTypeInIl2Cpp(false)]
public class TransformLookAtCamera : MonoBehaviour
{
    public Transform targetTransform;

    private bool isText;

    void Start() => isText = targetTransform.GetComponent<TextMesh>();
    void Update()
    {
        targetTransform.LookAt(Camera.main.transform);

        if (isText)
        {
            targetTransform.Rotate(0, 180, 0);
        }
    }
}