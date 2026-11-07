using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// Utilidades de UI.
public static class UtilUI
{
    static readonly List<RaycastResult> resultados = new List<RaycastResult>();

    /// true solo si el puntero está sobre UI INTERACTIVA (botones, paneles que reciben clic...).
    /// Las imágenes decorativas (fondos a pantalla completa, marcos...) se ignoran.
    public static bool PunteroSobreUIInteractiva(Vector2 posicion)
    {
        var es = EventSystem.current;
        if (es == null) return false;
        var datos = new PointerEventData(es) { position = posicion };
        resultados.Clear();
        es.RaycastAll(datos, resultados);
        foreach (var r in resultados)
        {
            if (r.gameObject == null) continue;
            if (ExecuteEvents.GetEventHandler<IPointerDownHandler>(r.gameObject) != null) return true;
            if (ExecuteEvents.GetEventHandler<IPointerClickHandler>(r.gameObject) != null) return true;
            if (ExecuteEvents.GetEventHandler<IDragHandler>(r.gameObject) != null) return true;
        }
        return false;
    }
}
