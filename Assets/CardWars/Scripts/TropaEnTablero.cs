using Sirenix.OdinInspector;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class TropaEnTablero : MonoBehaviour
{
    public TroopCardData datosDeLaCarta;
    public Tropa estadísticasVivas;

    [Header("Posición en Tablero")]
    public int laneIndex; // Define en qué carril se colocó (ej: 0, 1, 2)
    public bool esAliada; // Indica si pertenece al jugador local

    private List<RuntimeAbility> habilidadesActivas = new List<RuntimeAbility>();

    [Button]
    public void AlEntrarAlTablero()
    {
        if (datosDeLaCarta == null)
        {
            Debug.LogError($"TropaEnTablero '{name}': falta asignar 'datosDeLaCarta' en el Inspector.", this);
            return;
        }

        if (estadísticasVivas == null)
        {
            Debug.LogError($"TropaEnTablero '{name}': falta asignar 'estadísticasVivas' (componente Tropa) en el Inspector.", this);
            return;
        }

        // 1. Registrarse automáticamente en el TableroManager
        BoardManager.Instance.RegistrarTropa(this, esAliada);

        estadísticasVivas.Inicializar(datosDeLaCarta.Attack, datosDeLaCarta.Health, datosDeLaCarta.Armor, datosDeLaCarta.Shield, datosDeLaCarta.ShieldArmor, datosDeLaCarta.Burn, datosDeLaCarta.DirectAttack);

        foreach (var habilidadConfig in datosDeLaCarta.Abilities)
        {
            RuntimeAbility habilidadEnEjecucion = new RuntimeAbility(habilidadConfig, estadísticasVivas);
            habilidadesActivas.Add(habilidadEnEjecucion);
            habilidadEnEjecucion.Inicializar();
        }
    }

    private void OnDestroy()
    {
        // 2. Limpiarse del TableroManager al morir
        if (BoardManager.Instance != null)
        {
            BoardManager.Instance.QuitarTropa(this);
        }

        foreach (var habilidad in habilidadesActivas)
        {
            habilidad.Desvincular();
        }
    }
}