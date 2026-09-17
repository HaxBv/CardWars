using UnityEngine;

// Esta clase representará CADA habilidad ejecutándose en tiempo real en la mesa
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
        // 1. Evaluamos el TRIGGER del Enum
        if (datosOriginales.Trigger == TriggerType.Passive)
        {
            // Si la condición depende de la vida faltante
            if (datosOriginales.Condition == ConditionType.MissingHealth)
            {
                // Nos suscribimos de manera independiente al evento de la tropa
                poseedor.OnVidaModificada += ProcesarHabilidadPasivaVida;
                ProcesarHabilidadPasivaVida(); // Cálculo inicial
            }
        }
        else if (datosOriginales.Trigger == TriggerType.EndTurn)
        {
            // ACTUAlIZADO: Ahora nos suscribimos al evento global de tu PhasesSystem oficial
            PhasesSystem.OnFinDelCombate += ProcesarHabilidadFinDeTurno;
        }
    }

    // LÓGICA INDEPENDIENTE 1: Armadura por Vida Faltante
    private void ProcesarHabilidadPasivaVida()
    {
        // Revertimos lo otorgado antes por ESTA instancia
        poseedor.armadura -= armaduraOtorgadaAnteriormente;

        int vidaFaltante = poseedor.VidaMaxima - poseedor.VidaActual;

        // Leemos el valor (1) desde los efectos de tu Enum
        int valorEfecto = datosOriginales.Effects[0].Value;

        armaduraOtorgadaAnteriormente = vidaFaltante * valorEfecto;
        poseedor.armadura += armaduraOtorgadaAnteriormente;
    }

    // LÓGICA INDEPENDIENTE 2: Ocaso / Fin de turno (Curar por enemigos en carril)
    private void ProcesarHabilidadFinDeTurno()
    {
        if (datosOriginales.Condition == ConditionType.EnemiesInLane)
        {
            // Leemos el radio (3) y el valor de curación (1) de tus datos
            int rango = datosOriginales.LaneRadius;
            int valorCuracion = datosOriginales.Effects[0].Value;

            // Buscamos los enemigos usando la posición única de esta tropa
            int enemigosDetectados = BoardManager.Instance.ContarEnemigosEnCarril(poseedor, rango);

            if (enemigosDetectados > 0)
            {
                poseedor.Curar(enemigosDetectados * valorCuracion);
            }
        }
    }

    public void Desvincular()
    {
        // Limpieza obligatoria para que no se quede código colgado en memoria
        poseedor.OnVidaModificada -= ProcesarHabilidadPasivaVida;

        // ACTUALIZADO: Desuscripción de tu PhasesSystem oficial
        PhasesSystem.OnFinDelCombate -= ProcesarHabilidadFinDeTurno;
    }
}