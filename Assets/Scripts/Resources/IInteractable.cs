using UnityEngine;

public interface IInteractable
{
    bool CanInteract(PlayerController player);
    string GetInteractionText();
    void Interact(PlayerController player);
}