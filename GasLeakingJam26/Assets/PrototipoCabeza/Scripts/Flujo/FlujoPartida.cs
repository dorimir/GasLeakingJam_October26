using UnityEngine;

/// En el prototipo: botón "Ver resultado". Guarda el resultado, se lleva el busto con sus
/// mechones colocados a la escena de Resultados y hace la transición.
public class FlujoPartida : MonoBehaviour
{
    public GestorMechones gestor;
    [Tooltip("Raíz del busto que se lleva a Resultados (con los mechones colocados)")]
    public Transform busto;
    public string escenaResultados = "Resultados";

    public void IrAResultados()
    {
        if (Transicion.Ocupado || gestor == null || busto == null) return;

        int usados = 0;
        foreach (var m in gestor.Mechones) if (m.estado == Mechon.Estado.Colocado) usados++;
        ResultadoPartida.porcentajeCalvicie = gestor.PorcentajeCalvicie;
        ResultadoPartida.mechonesUsados = usados;
        ResultadoPartida.mechonesTotales = gestor.numeroMechones;
        ResultadoPartida.escenaOrigen = gameObject.scene.name;

        Transicion.Ir(escenaResultados, () =>
        {
            // Con la pantalla en negro: los mechones colocados pasan a ser hijos del busto y viajan con él.
            foreach (var m in gestor.Mechones)
            {
                if (m.estado != Mechon.Estado.Colocado) continue;
                m.transform.SetParent(busto, true);
                var col = m.GetComponent<Collider>();
                if (col) col.enabled = false;
            }
            if (busto.parent != null) busto.SetParent(null, true);
            DontDestroyOnLoad(busto.gameObject);
            ResultadoPartida.busto = busto.gameObject;
        });
    }
}
