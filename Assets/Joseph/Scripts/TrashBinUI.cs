using UnityEngine;
using UnityEngine.EventSystems;

public class TrashBinUI : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        // If the object dropped has the drag script, notify the manager
        if (eventData.pointerDrag != null)
        {
            // Trigger the binned logic in the manager
            CleaningMiniGameManager.Instance?.OnTrashBinned(eventData.pointerDrag);
        }
    }
}