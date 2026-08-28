using UnityEngine;
using UnityEngine.EventSystems;

public class TrashBinUI : MonoBehaviour, IDropHandler
{
    [Header("Bin Configuration")]
    [Tooltip("Check true if this bin is for Recyclables; uncheck for General/Organic Waste.")]
    [SerializeField] private bool isRecycleBin = true;

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag != null)
        {
            CleaningMiniGameManager.Instance?.OnTrashSorted(eventData.pointerDrag, isRecycleBin);
        }
    }
}