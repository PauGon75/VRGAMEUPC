using UnityEngine;
using UnityEngine.InputSystem;

public class HandAnimation : MonoBehaviour
{
    [SerializeField] private Animator handAnimator;
    [SerializeField] private InputActionReference gripAction;
    [SerializeField] private InputActionReference triggerAction;

    private void Awake()
    {
        handAnimator = GetComponent<Animator>();

    }

    void Update()
    {
        if (handAnimator == null) return;

        float gripValue = gripAction.action.ReadValue<float>();
        float triggerValue = triggerAction.action.ReadValue<float>();

        handAnimator.SetFloat("Grip", gripValue);
        handAnimator.SetFloat("Trigger", triggerValue);

    }
}
