# 01_ARQUITECTURA.md — CardWars

## Sistemas principales
- **PhasesSystem**: Controla la ronda de 5 pasos (TroopTurn x2, SpellAndEnvironmentTurn x2, Combate).
- **BoardManager**: Singleton que gestiona listas de tropas aliadas y enemigas en el tablero.
- **AbilityEffectExecutor**: Diccionario central que ejecuta efectos basados en (TriggerType, EffectType).
- **RuntimeAbility**: Instancia independiente por cada habilidad de una tropa en el tablero.

## Managers
- **BoardManager**: Instancia única en escena. Registra/desregistra tropas. Proporciona métodos de consulta (ContarEnemigosEnCarril).
- **GameManager**: Placeholder actualmente sin lógica relevante.

## ScriptableObjects
- **CardData**: ScriptableObject base para todas las cartas. Contiene: ID, nombre, costo, descripción, frase, tipo, rareza, familia, imágenes, habilidades (List<AbilityData>) y efectos (List<EffectData>).
- **AbilityData**: Datos de una habilidad (trigger, filter, target, condition, family, laneRadius, lista de EffectData).
- **EffectData**: Efecto simple (effectType, value).
- **TroopCardData**: Subclase/extension de CardData para tropas (atributos de combate: ataque, vida, armadura, escudo, etc.).
- **EnvironmentCardData**: Pending (aún no creado en el proyecto).
- **SpellCardData**: Pending (aún no creado en el proyecto).

## Datos
- Los datos de las cartas viven en archivos `.asset` de ScriptableObjects en `Assets/CardWars/Data/ScriptableObjects/`.
- Actualmente hay 2 TroopCardData: Konquest y VirusFox.
- Las carpetas Spell y Environment existen pero están vacías (sin assets creados).

## Gameplay
- Sistema de turnos en 5 fases secuenciales.
- Tropas se colocan en carriles (0, 1, 2, etc.).
- Combate automático: las tropas atacan al final del fase de Combate.
- Daño se mitiga primero con escudo, luego armadura, luego vida directa.
- Las habilidades se registran por combinaciones de (trigger, effect) y se ejecutan al desplegar una tropa.

## Cartas
- Definidas mediante ScriptableObjects `CardData`.
- Cada carta puede tener múltiples habilidades (AbilityData), cada una con trigger, filter, target, condition y efectos.
- Los datos de estadísticas (vida, ataque, armadura) están en el TroopCardData asociado.

## Habilidades
- Los triggers incluyen: StartTurn, EndTurn, OnDeploy, OnDraw, OnAttack, OnTakeDamage, OnKill, OnDeath, OnUnitSummoned, OnUnitDeath, OnLaneChanged, EnemyEnteredLane, AllyEnteredLane, Fusion, Evolution, Passive.
- Los efectos incluyen: GainAttack, GainHealth, GainArmor, GainShield, GainMana, GainInmunity, Heal, DealDamage, DrawCard, SummonCard, Move, Stun, Transform, Destroy, Execute.
- Cada efecto puede tener filtros (TargetType, FilterType) y condiciones (ConditionType) que se evalúan antes de ejecutarse.

## Lanes
- El tablero tiene carriles indexados (0, 1, 2, etc.).
- La distancia entre carriles se calcula con `Mathf.Abs(enemigo.laneIndex - carrilOrigen)`.
- Los efectos pueden tener un `laneRadius` que determina qué carriles afectan.

## Combate
- El daño se calcula en `Tropa.RecibirDaño()`: primero escudo, luego armadura, luego vida.
- Cuando la vida llega a 0, la tropa es destruida (`Destroy(gameObject)`).
- BoardManager cuenta enemigos en un carril considerando el radio de cada tropa.
- El evento `OnFinDelCombate` se dispara cuando la fase de combate concluye.

## UI
- Pendiente de implementación. Se espera un botón "Terminar Turno" que invoque `PhasesSystem.AvanzarTurno()`.

## Recursos
- ScriptableObjects en `Assets/CardWars/Data/ScriptableObjects/`.
- Prefabs/Instancias de tropas en escenas.
- Diccionario de ejecutadores en `AbilityEffectExecutor._executors`.

## Dependencias entre sistemas
1. **TropaEnTablero** → registra con **BoardManager.Instance** en `AlEntrarAlTablero()`.
2. **TropaEnTablero** → crea **RuntimeAbility** para cada habilidad en `datosDeLaCarta.Abilities`.
3. **RuntimeAbility.Inicializar()** → llama a **AbilityEffectExecutor.Execute()** con BoardManager.Instance.
4. **AbilityRegistrator** → registra todas las combinaciones (trigger, effect) en `AbilityEffectExecutor._executors` al iniciar.
5. **PhasesSystem** → controla el flujo de turnos, dispara eventos al cambiar de fase.
6. **BoardManager.ContarEnemigosEnCarril()** → usa el radio de las tropas para determinar alcance.
7. **Tropa.RecibirDaño()** → maneja mitigación de daño (escudo → armadura → vida).