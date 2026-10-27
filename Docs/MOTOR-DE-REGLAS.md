# CardWars — Motor de reglas (C# puro)

Este documento acompaña a la carpeta `Assets/CardWars/Rules/`. Es una **propuesta de arquitectura con código y pruebas**, para que Jeremy la lea, la discuta, la adapte y la conecte al juego. Nada de lo que ya existía se tocó: el motor está al lado, sin conectar a la escena.

## Por qué existe

Hoy las reglas del juego viven repartidas entre `PhasesSystem` (pasos y prioridad), `LandLane` (despliegue), `CombatManager` (combate), `Portals` (escudos) y `DeckHandManager` (mazo y mano), mezcladas con animaciones, corrutinas y GameObjects. Eso tiene tres consecuencias:

1. **No se pueden probar** sin abrir la escena y jugar a mano.
2. **No se pueden ejecutar en un servidor**, que no tiene escena ni animaciones. Para que un juego sea online el servidor tiene que poder decidir qué es válido y qué pasa.
3. **El cliente decide cosas que no debería** (quién baraja, de quién es el turno, en qué bando se despliega).

La idea: separar **qué pasa** (reglas) de **cómo se ve** (Unity). El motor recibe una petición ("el jugador 1 despliega la carta 7 en el carril 2, espacio 1"), la valida, cambia el estado y devuelve **lo que ocurrió como una lista de eventos**. El cliente solo reproduce esos eventos con las animaciones que ya tiene.

```
Cliente (Unity)                      Motor (C# puro)
  arrastra carta ──DeployTroop──►   valida turno, mano, terreno, espacios
                 ◄─ resultado + eventos (TroopDeployed, ...)
  anima los eventos
  botón "terminar paso" ─EndStep─►  avanza; tras el paso 4 resuelve el combate
                 ◄─ eventos (Attack, TroopDied, PortalDamaged, CardDrawn, ...)
```

## Qué contiene

| Archivo | Para qué sirve |
|---|---|
| `CardWars.Rules.asmdef` | Ensamblado **sin referencias a Unity** (`noEngineReferences`). Si alguien escribe `using UnityEngine;` aquí, no compila: esa es la garantía. |
| `Types.cs` | `CardDefinition` (datos de una carta de tropa), `GameConfig` (carriles, mano, vida del portal) y enums. |
| `State.cs` | El estado de la partida en datos simples: mazos, manos, tablero (2 espacios por bando y carril), portales, ronda, paso, prioridad. |
| `Events.cs` | `GameEvent` (qué ocurrió), `ActionResult` (por qué se aceptó o rechazó una acción) y `ActionOutcome`. |
| `Views.cs` | `PlayerView`: lo que **un** jugador puede saber. No incluye la mano ni el orden del mazo del rival, solo cuántas cartas tiene. |
| `GameEngine.cs` | `StartGame(seed)`, `DeployTroop`, `EndStep`, `CreateView`, `RedactFor`. Aquí están las reglas. |
| `Tests/GameEngineTests.cs` | 37 pruebas EditMode (incluye una de estrés con jugadas válidas, inválidas y fuera de turno). |

## De dónde sale cada regla

El motor reproduce lo que hace hoy el juego. Si cambia el juego, cambia el motor (y su test).

| Regla | Motor | Código actual |
|---|---|---|
| Rondas con Alba y Ocaso, 5 pasos, prioridad que alterna cada ronda | `StartStep`, `EndTimeOfDay` | `PhasesSystem` |
| Primer jugador al azar | `StartGame` | `PhasesSystem.Start` |
| Mazo = `MaxDeckCopies` copias de cada tropa, barajado; mano inicial 4, máximo 8; se roba al terminar cada Alba u Ocaso | `BuildDeck`, `Draw` | `DeckHandManager` |
| Terreno, 2 espacios, regla Team, soltar en el espacio 1 empuja al otro | `DeployTroop` | `LandLane.DeployTroop` |
| Combate carril por carril; ataques y objetivos preparados antes del daño; primero el bando con prioridad; el espacio 2 ataca primero; las muertes se confirman al final del carril | `ResolveLane` | `CombatManager` |
| Portal: vida, 3 escudos, un golpe rompe como mucho un escudo | `DamagePortal` | `Portals.TakeDamage` |

## Cómo ejecutar las pruebas

1. Abre el proyecto en Unity. Unity generará los `.meta` de la carpeta nueva; **inclúyelos en el commit**.
2. `Window > General > Test Runner > EditMode > Run All`. Deben pasar las 37.

(Las pruebas también se verificaron fuera de Unity compilando los mismos archivos con .NET 8 y NUnit: 37/37 en verde. Además se comprobó que fallan si se rompe una regla, por ejemplo invirtiendo el orden de los objetivos.)

## Cosas que se encontraron al escribirlo (para discutir)

Al convertir las reglas a código "puro" aparecen dudas que en Unity pasan desapercibidas. Son buenas preguntas de diseño; no son reproches.

1. **¿Cuál es el espacio de adelante, el 1 o el 2?** `CombatManager` dice que `SecondSlot` es el adelantado (los enemigos atacan primero a la tropa del espacio 2). Pero `LandLane.RefreshTroopPositions` dice que, si muere la del espacio 1, "la de atrás avanza" desde el 2 al 1, lo que supone que el 1 es el frente. Con las reglas de combate actuales, ese movimiento nunca ocurre porque el espacio 1 solo recibe daño cuando el 2 está vacío. El motor sigue lo que hace `CombatManager` (es lo que decide el resultado), así que hay que **elegir un criterio y corregir el que no coincida**.
2. **El umbral del segundo escudo del portal.** `Portals.GetNextThreshold` calcula `Mathf.CeilToInt(maxHealth * (escudos + 1) * 0.2f)` con `float`. Con 50 de vida, el resultado (comprobado con aritmética `float` de .NET; conviene confirmarlo en vuestro Editor, mirando el texto "(N HP)" del portal) es 40, **31** (no 30) y 20, porque `0.2f` no es exacto. En `GameEngine.NextShieldThreshold` se hace con enteros y da 40, 30 y 20. Si el cliente y el servidor calculan con números distintos, discrepan en el resultado. Decidid cuál es la regla correcta y dejad **una sola** fórmula.
3. **El juego aún no tiene condición de victoria.** Nadie escucha `Portals.OnPortalDestroyed`. El motor añade una regla propuesta: al destruirse un portal, gana el rival y la partida termina en ese momento. Revisad si es la regla que queréis.
4. **Un objetivo ya muerto sigue "recibiendo" golpes.** Como los objetivos se eligen antes del daño, si dos tropas apuntan a la misma, la segunda pega a una tropa con 0 de vida y el golpe se pierde (no pasa al portal ni a otra tropa). Es lo que hace el juego hoy y el motor lo respeta, pero ¿es lo que queréis?
5. **Hechizos y dominios (pasos 3 y 4).** No están en el motor: en esos pasos solo se puede terminar el paso. Es a propósito, para empezar por lo más pequeño que funcione de punta a punta.
6. **Daño y armadura.** `TroopCardData` tiene armadura, escudo y quemadura, pero `TroopInGame.TakeDamage` solo resta vida. El motor hace lo mismo que el juego.

## Tareas para Jeremy (en orden)

Cada una es pequeña y se puede entregar en su propio commit o PR. Las primeras sirven para entender el motor antes de conectarlo.

- [ ] **1. Leer y ejecutar.** Abre Unity, corre los tests, haz commit de los `.meta`. Después lee `GameEngine.cs` y escribe, con tus palabras, un párrafo en este documento: ¿qué hace `EndStep` cuando el paso es el 4?
- [ ] **2. Resolver las dudas de arriba** (puntos 1 a 4): decide la regla correcta y deja el juego y el motor de acuerdo. Cada decisión debe quedar en un test.
- [ ] **3. Escribir 3 pruebas propias** para reglas que consideres importantes y que no estén cubiertas (por ejemplo: una carta que con Team ocupa los dos espacios y luego muere, o el robo con el mazo vacío).
- [ ] **4. Adaptador de cartas.** Crea una clase en `Assets/CardWars/Scripts/` que convierta cada `TroopCardData` en un `CardDefinition` (`Attack`, `Health`, `MaxDeckCopies`, y las pasivas `Team`, `AllTerrain`, `Highground`, `Aquatic`). Este ensamblado sí puede usar Unity.
- [ ] **5. Conectar el despliegue.** En `LandLane.DeployTroop`, antes de instanciar el prefab, llama a `engine.DeployTroop(...)`. Solo si el resultado es `Applied` se instancia. El bando sale del jugador, no de la posición del cursor.
- [ ] **6. Botón "Terminar paso"** que llame a `engine.EndStep(...)`, y que `DeckHandManager.CanDragCard` use `state.CurrentPlayer`.
- [ ] **7. Mazo y mano desde el motor.** Que `DeckHandManager` deje de barajar y robar por su cuenta y muestre `CreateView(...).MyHand`.
- [ ] **8. Combate por eventos.** Haz que `CombatManager` deje de calcular el daño y reproduzca la lista de `GameEvent` (`Attack`, `TroopDied`, `PortalDamaged`...) con las animaciones actuales.
- [ ] **9. Pantalla de victoria** al recibir `GameWon`.
- [ ] **10. Orden del repositorio.** Arreglar `Packages/manifest.json` (hoy apunta a `file:C:/Users/JEREMY/...` y no abre en otras máquinas), actualizar el Vault con el estado real, escribir un `README.md` con cómo abrir y probar el proyecto, y revisar la licencia de los assets de pago (Feel y Odin Inspector) antes de mantener el repositorio público.

**Lo que viene después** (no está en este PR): un servidor que ejecute este mismo motor y un protocolo de mensajes entre los dos clientes. El motor ya está pensado para eso: las acciones llevan al jugador como argumento (en un servidor saldría de la sesión), `PlayerView` es lo único que se devuelve a un cliente, `RedactFor` oculta qué carta robó el rival y `TurnNumber` servirá como "turno esperado" para evitar jugadas repetidas o atrasadas. El profesor compartirá la guía del multijugador cuando termines las tareas 1 a 9.
