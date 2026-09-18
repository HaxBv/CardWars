# 05_MEMORIA_PROYECTO.md — CardWars

## Concepto general
CardWars es un juego de cartas por turnos estratégico donde los jugadores colocan tropas en carriles de un tablero y estas atacan automáticamente al final de cada ronda. El juego utiliza un sistema de fases en 5 pasos para estructurar cada ronda. Las cartas tienen habilidades definidas por triggers, efectos, filtros y condiciones.

## Género
Juego de cartas estratégico / Juegos de tablero táctico. Mezcla elementos de juegos de cartas coleccionables con colocación táctica en carriles.

## Mecánicas principales
- **Sistema de fases en 5 pasos**: TroubTurn (jugador 1), TroopTurn (jugador 2), SpellAndEnvironmentTurn (jugador 1), SpellAndEnvironmentTurn (jugador 2), Combate automático.
- **Colocación en carriles**: Las tropas se colocan en carriles indexados. La distancia entre carriles afecta el alcance de efectos.
- **Sistema de daño**: Daño → escudo → armadura → vida. Tres niveles de mitigación.
- **Habilidades por trigger**: Cada carta puede tener múltiples habilidades que se activan bajo triggers específicos (StartTurn, EndTurn, OnDeploy, OnAttack, OnDeath, etc.).
- **Efectos con filtros y condiciones**: Los efectos pueden tener TargetType (a quién afecta), FilterType (condición adicional) y ConditionType (condición especial como MissingHealth).

## Filosofía de diseño
- **Datos separados de lógica**: La información de las cartas (costos, descripciones, estadísticas, habilidades) vive en ScriptableObjects, no en prefabs o código monolítico.
- **Sistema extensible**: Nuevo efectos se añaden registrando combinaciones (trigger, effect) en el executor central, sin modificar datos existentes.
- **Turnos claros**: Las 5 fases proporcionan estructura predecible y facilitan la creación de IA o lógica humana para cada paso.
- **Mitigación escalonada**: El sistema escudo→armadura→vida crea decisiones tácticas sobre cuándo gastar recursos defensivos.

## Estructura general
- `Assets/CardWars/Scripts/`: Código C# principal.
- `Assets/CardWars/Data/ScriptableObjects/`: Datos de cartas ScriptableObjects.
- `Assets/CardWars/Scenes/`: Escenas del proyecto.
- `Vault/`: Memoria persistente de desarrollo (sistema nuevo).

## Convenciones de código
- Los scripts usan Sirenix.OdinInspector para inspecciones mejoradas en el editor.
- Los nombres de clases siguen la convención de PascalCase.
- Los enumarados viven en `Enums.cs` para compartir referencia entre todos los scripts.
- Los singleton usan patrón `instance` estático con verificación `null`.
- Los eventos usan el patrón `event Action` de C#.
- Los ScriptableObjects de tipo CardData tienen atributo `[CreateAssetMenu]`.

## Decisiones permanentes
1. ScriptableObjects para datos de cartas (2026-09-18).
2. Sistema de fases en 5 pasos (2026-09-18).
3. Mitigación de daño: escudo → armadura → vida (2026-09-18).
4. Singleton BoardManager para gestión de tropas (2026-09-18).
5. Ejecutores centralizados AbilityEffectExecutor por (trigger, effect) (2026-09-18).

## Restricciones
- Proyecto en Unity (versión actual).
- Usa Sirenix.OdinInspector para atributos del editor.
- No se pueden modificar ProjectSettings innecesariamente.
- Los ScriptableObjects son la fuente principal de datos de cartas.

## Preferencias importantes del desarrollo
- Mantener los datos de carta separados de la lógica de ejecución.
- Documentar decisiones en 02_DECISIONES.md cada vez que se tomen decisiones arquitectónicas significativas.
- Usar Vault para contexto entre sesiones, pero siempre verificar contra el código real del proyecto.
- Priorizar la implementación de ejecutores de habilidades antes de nuevos tipos de cartas.

## Sistemas existentes
- **PhasesSystem**: Controla Ronda/Fase/ Paso/ Turno actual. Eventos: OnFinDeTurno, OnFaseCombateIniciada, OnFinDelCombate.
- **BoardManager**: Singleton. Listas: tropasAliadas, tropasEnemigas. Métodos: RegistrarTropa, QuitarTropa, ContarEnemigosEnCarril.
- **Tropa**: Componentes en gameobjects. Estadísticas: ataqueActual, vidaActual, armadura, escudo. Métodos: RecibirDaño, Curar.
- **TropaEnTablero**: Link entre CardData (ScriptableObject) y Tropa. Inicializa estadísticas y crea RuntimeAbility por habilidades.
- **AbilityRegistrator**: Registra todas las combinaciones (trigger, effect) en AbilityEffectExecutor al Awake.
- **AbilityEffectExecutor**: Diccionario estático <(TriggerType, EffectType), Action<AbilityData, Tropa, BoardManager>>.
- **RuntimeAbility**: Instancia por-habilidad por-tropa. Inicializa ejecutando el efecto registrado.
- **CardData**: ScriptableObject base. Campos: cardID, cardName, cost, descriptionAbility, phrase, type, rarity, family, imgType, imgRarity, imgCard, abilities, effects.
- **EffectData**: Efecto simple. Campos: effectType, value.
- **AbilityData**: Metadatos de habilidad. Campos: trigger, filter, target, condition, family, laneRadius, effects.
- **Enums.cs**: Todos los enumarados del proyecto (CardType, PlayerTurn, ConditionType, FilterType, CardRarity, CardFamily, TriggerType, EffectType, AbilityPassive, TargetType, TurnPhase, Team, MarkType).