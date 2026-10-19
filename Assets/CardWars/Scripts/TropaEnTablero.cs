
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

public class TropaEnTablero : MonoBehaviour
{
    public TroopCardData datosDeLaCarta;
    public Tropa estadísticasVivas;

    [Header("Posición en Tablero")]
    public int laneIndex;
    public bool esAliada;

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
            Debug.LogError($"TropaEnTablero '{name}': falta asignar 'estadísticasVivas' en el Inspector.", this);
            return;
        }

        // 1. Registrarse en BoardManager
        if (BoardManager.Instance != null)
        {
            BoardManager.Instance.RegistrarTropa(this, esAliada);
        }

        // 2. Registrarse en PhasesSystem
        var phasesSystem = FindFirstObjectByType<PhasesSystem>();

        if (phasesSystem != null)
        {
            phasesSystem.RegistrarTropa(this);
        }

        // 3. Inicializar estadísticas
        estadísticasVivas.Inicializar(
            datosDeLaCarta.Attack,
            datosDeLaCarta.Health,
            datosDeLaCarta.Armor,
            datosDeLaCarta.Shield,
            datosDeLaCarta.ShieldArmor,
            datosDeLaCarta.Burn,
            datosDeLaCarta.Overstrike
        );

        // 4. Inicializar habilidades
        foreach (var habilidadConfig in datosDeLaCarta.Abilities)
        {
            RuntimeAbility habilidadEnEjecucion =
                new RuntimeAbility(habilidadConfig, estadísticasVivas);

            habilidadesActivas.Add(habilidadEnEjecucion);
            habilidadEnEjecucion.Inicializar();
        }
    }

    private void OnDestroy()
    {
        // 1. Eliminar del BoardManager
        if (BoardManager.Instance != null)
        {
            BoardManager.Instance.QuitarTropa(this);
        }

        // 2. Eliminar del PhasesSystem
        var phasesSystem = FindFirstObjectByType<PhasesSystem>();

        if (phasesSystem != null)
        {
            phasesSystem.QuitarTropa(this);
        }

        // 3. Desvincular habilidades
        foreach (var habilidad in habilidadesActivas)
        {
            habilidad.Desvincular();
        }

        habilidadesActivas.Clear();
    }
}
