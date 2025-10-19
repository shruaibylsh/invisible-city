using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class InfoHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Assign in Inspector")]
    public GameObject infoPanel;
    public GameObject instructionsText;

    void Start()
    {
        if (infoPanel != null) infoPanel.SetActive(false);
        if (instructionsText != null) instructionsText.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (infoPanel != null) infoPanel.SetActive(true);
        if (instructionsText != null) instructionsText.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (infoPanel != null) infoPanel.SetActive(false);
        if (instructionsText != null) instructionsText.SetActive(false);
    }
}
