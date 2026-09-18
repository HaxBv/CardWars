# 02_DECISIONES.md — CardWars

## 2026-09-18 — Sistema de cartas basado en ScriptableObjects

**Decisión:**
Utilizar ScriptableObjects para almacenar la información estática de las cartas.

**Motivo:**
Evitar tener cientos de prefabs diferentes y separar los datos de la lógica. Permite diseñar cartas en el editor sin tocar código.

**Consecuencia:**
La lógica de ejecución de efectos debe permanecer separada de CardData y se usa el AbilityEffectExecutor centralizado. Los ScriptableObjects son activos del proyecto, no prefabs.

## 2026-09-18 — Sistema de fases en 5 pasos

**Decisión:**
Implementar un sistema de turnos basado en 5 pasos secuenciales en lugar de fases tradicionales de MTG u otros juegos de cartas.

**Motivo:**
Proporciona una estructura clara y predecible para cada ronda: 2 pasos de tropa, 2 pasos de hechizos/dominios, y 1 fase de combate automático. Facilita la interpolación entre turnos de jugadores.

**Consecuencia:**
Se creó `PhasesSystem.cs` con evento `OnFinDeTurno`, `AvanzarTurno()` y `ActualizarEstadoTurno()`. Cada paso tiene lógica diferenciada por jugador.

## 2026-09-18 — Singleton BoardManager

**Decisión:**
Utilizar un patrón Singleton para BoardManager en lugar de DontDestroyOnLoad o eventos de escena.

**Motivo:**
Simplificación para la versión actual. El tablero persiste durante la partida y su estado (tropas en carriles) es esencial para la lógica de combate y efectos.

**Consecuencia:**
BoardManager.Instance es la fuente de consulta global para todas las tropas. Se pierde al recargar la escena a menos que se use DontDestroyOnLoad manualmente.

## 2026-09-18 — Sistema de daño: escudo → armadura → vida

**Decisión:**
Implementar la mitigación de daño en el orden: primero escudo, luego armadura común, luego vida directa.

**Motivo:**
Crea una progresión de resistencia significativa: el escudo se agota primero, la armadura proporciona una capa secundaria, y la vida es el último recurso. Esto da más valor a los efectos que otorgan escudo vs armadura.

**Consecuencia:**
Implementado en `Tropa.RecibirDaño()`. Un daño que sobrepase el escudo pero no la armadura no quitará vida directa.

## 2026-09-18 — Estructuración de AbilityData y EffectData

**Decisión:**
Separar las habilidades en dos niveles: AbilityData (metadatos: trigger/filter/target/condition + lista de efectos) y EffectData (efecto simple: effectType + value).

**Motivo:**
Permite combinar múltiples efectos por habilidad y aplicar lógica condicional (filter/target/condition) antes de ejecutar cada efecto. Facilita la expansión de nuevos tipos de efectos sin modificar la estructura de la carta.

**Consecuencia:**
Se creó `CardData` con `List<AbilityData> abilities` y `List<EffectData> effects`. El executor central `AbilityEffectExecutor` usa (trigger, effect) como clave diccionario.

## 2026-09-18 — Registros de habilidades en AbilityRegistrator

**Decisión:**
Registrar todas las combinaciones posibles de (trigger, effect) en un método estático `RegisterBasicAbilities()` en lugar de dispersar la lógica en cada ScriptableObject.

**Motivo:**
Centraliza la lógica de ejecución, evita code duplication y permite agregar/quitar efectos sin modificar los datos de las cartas. Facilita testing de cada combinación.

**Consecuencia:**
Se creó `AbilityEffectExecutor` con diccionario estático `_executors`. Cada executor recibe (AbilityData, Tropa, BoardManager). Falta implementar todos los triggers/efectos posibles.