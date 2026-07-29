using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class NurseAnimations : MonoBehaviour
{
    private Animator animator;

    [SerializeField] private float jumpDuration = 1.0f;
    [SerializeField] private float waveDuration = 1.5f;

    private bool isBusy = false;

    public bool IsBusy => isBusy;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        HandleWalkAnimation();
        HandleActions();
    }

    void HandleWalkAnimation()
    {
        if (isBusy)
        {
            animator.SetFloat("Speed", 0f);
            return;
        }

        bool isWalking =
            Keyboard.current.wKey.isPressed ||
            Keyboard.current.aKey.isPressed ||
            Keyboard.current.sKey.isPressed ||
            Keyboard.current.dKey.isPressed;

        animator.SetFloat("Speed", isWalking ? 1f : 0f);
    }

    void HandleActions()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame && !isBusy)
        {
            StartCoroutine(JumpRoutine());
        }

        if (Keyboard.current.qKey.wasPressedThisFrame && !isBusy)
        {
            StartCoroutine(WaveRoutine());
        }
    }

    IEnumerator JumpRoutine()
    {
        isBusy = true;

        animator.SetFloat("Speed", 0f);
        animator.SetTrigger("Jump");

        yield return new WaitForSeconds(jumpDuration);

        isBusy = false;
    }

    IEnumerator WaveRoutine()
    {
        isBusy = true;

        animator.SetFloat("Speed", 0f);
        animator.SetTrigger("Wave");

        yield return new WaitForSeconds(waveDuration);

        isBusy = false;
    }
}