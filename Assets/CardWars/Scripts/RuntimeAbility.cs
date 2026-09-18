using UnityEngine;

public class RuntimeAbility
{
    private AbilityData datosOriginales; // La referencia a los Enums de la foto
    private Tropa poseedor;              // La tropa física en el tablero

    // Variables internas únicas para CADA instancia independiente en juego
    private int armaduraOtorgadaAnteriormente = 0;

    public RuntimeAbility(AbilityData datos, Tropa tropaEnTablero)
    {
        this.datosOriginales = datos;
        this.poseedor = tropaEnTablero;
    }

    public void Inicializar()
    {
        // Ya no hay if/else por trigger. En su lugar, ejecutamos el efecto correspondiente
        // usando el executor centralizado. Si no hay executor registrado, simplemente registramos warning.
        // Ejecutamos efecto inicial si la lista de efectos no está vacía.
        if (datosOriginales.Effects != null && datosOriginales.Effects.Count > 0)
        {
            foreach (var effect in datosOriginales.Effects)
            {
                AbilityEffectExecutor.Execute(datosOriginales.Trigger, effect.EffectType, datosOriginales, poseedor, BoardManager.Instance);
            }
        }
    }

    // LÓGICA INDEPENDIENTE 1: Armadura por Vida Faltante
    // (Ahora manejada vía AbilityEffectExecutor.Register en lugar de código hardcodeado)
    // Método mantenido por si se necesita limpieza, pero la lógica principal ya no está aquí.

    // LÓGICA INDEPENDIENTE 2: Ocaso / Fin de turno (Curar por enemigos en carril)
    // (También manejada vía executor)

    public void Desvincular()
    {
        // En el nuevo patrón executor, la limpieza es mínima
        // ya que la ejecución ocurre de inmediato en Inicializar()
        // y no hay suscripciones de eventos persistentes por mantener.
        // Se conservan referencias para posibles futuras suscripciones
        // si algún efecto requiere suscripción evento-based.
        this.datosOriginales = null;
        this.poseedor = null;
    }
}