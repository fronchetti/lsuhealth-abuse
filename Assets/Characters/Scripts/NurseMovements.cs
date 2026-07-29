using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class NurseMovements : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float rotationSpeed = 10f;

    private Rigidbody rb;
    private NurseAnimations nurseAnimations;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        nurseAnimations = GetComponent<NurseAnimations>();
    }

    void FixedUpdate()
    {
        if (Keyboard.current == null) return;

        // Stop movement if performing an action
        if (nurseAnimations != null && nurseAnimations.IsBusy)
            return;

        Vector3 inputDirection = GetInputDirection();

        if (inputDirection.sqrMagnitude < 0.01f)
            return;

        MoveCharacter(inputDirection);
        RotateCharacter(inputDirection);
    }

    Vector3 GetInputDirection()
    {
        Vector3 direction = Vector3.zero;

        if (Keyboard.current.wKey.isPressed) direction += Vector3.forward;
        if (Keyboard.current.sKey.isPressed) direction += Vector3.back;
        if (Keyboard.current.aKey.isPressed) direction += Vector3.left;
        if (Keyboard.current.dKey.isPressed) direction += Vector3.right;

        return direction.normalized;
    }

    void MoveCharacter(Vector3 direction)
    {
        Vector3 newPosition = rb.position + direction * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(newPosition);
    }

    void RotateCharacter(Vector3 direction)
    {
        Quaternion targetRotation = Quaternion.LookRotation(direction);

        Quaternion smoothRotation = Quaternion.Slerp(
            rb.rotation,
            targetRotation,
            rotationSpeed * Time.fixedDeltaTime
        );

        rb.MoveRotation(smoothRotation);
    }
}