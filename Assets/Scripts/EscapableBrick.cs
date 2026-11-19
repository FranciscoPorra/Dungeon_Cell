using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(XRGrabInteractable), typeof(Collider))]
public class EscapableBrick : MonoBehaviour
{
    public XRGrabInteractable GrabInteractable { get; private set; }
    public Collider BrickCollider { get; private set; }

    public bool IsHeldByPlayer => GrabInteractable != null && GrabInteractable.isSelected;

    void Awake()
    {
        GrabInteractable = GetComponent<XRGrabInteractable>();
        BrickCollider = GetComponent<Collider>();
    }
}