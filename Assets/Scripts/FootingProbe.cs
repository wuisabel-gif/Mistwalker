using UnityEngine;
using UnityEngine.Serialization;

public class FootingProbe : MonoBehaviour
{
    public CharacterController controller;
    public bool isGrounded;
    [FormerlySerializedAs("groundDistance")]
    public float checkDistance = 0.45f;
    public LayerMask groundMask;

    void Update()
    {
        if (controller == null)
        {
            isGrounded = false;
            return;
        }

        isGrounded = Physics.Raycast(controller.transform.position, Vector3.down, checkDistance, groundMask);
    }
}
