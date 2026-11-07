using UnityEngine;
using UnityEngine.EventSystems;

/// Panel lateral de UI: al pulsar con clic izquierdo sobre él se coge un mechón y se arrastra a la cabeza.
public class PanelMechonesUI : MonoBehaviour, IPointerDownHandler
{
    public GestorMechones gestor;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || gestor == null) return;
        gestor.EmpezarArrastreDesdeUI();
    }
}
