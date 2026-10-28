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
| `GameEngine.cs` | `CreateSetup(seed)`, `StartGame`, `DeployTroop`, `EndStep`, `CreateView`, `RedactFor`. Aquí están las reglas. |
| `Replay.cs` | `GameSetup` (todo el azar de una partida, sorteado una vez), `GameAction` (una acción aceptada) y `Replay(setup, acciones)`, que reconstruye la partida desde cero. |
| `Tests/GameEngineTests.cs`, `Tests/ReplayTests.cs` | 49 pruebas EditMode: reglas, información oculta, replay de partidas al azar y una de estrés con jugadas válidas, inválidas y fuera de turno. |

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
2. `Window > General > Test Runner > EditMode > Run All`. Deben pasar las 49.

(Las pruebas también se verificaron fuera de Unity compilando los mismos archivos con .NET 8 y NUnit: 49/49 en verde. Además se comprobó que fallan si se rompe una regla, por ejemplo invirtiendo el orden de los objetivos.)

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

## Pensado para un servidor (no está en este PR)

Lo que viene después es un servidor que ejecute **este mismo código** y un protocolo de mensajes entre los dos clientes. El motor ya está preparado para eso:

| Necesidad de un servidor | Cómo lo cubre el motor |
|---|---|
| Compilar las reglas dentro del módulo, sin duplicarlas | Son C# puro. El módulo puede incluir los `.cs` de `Assets/CardWars/Rules/` (sin la carpeta `Tests`) en su propio proyecto. Los `.asmdef` solo los usa Unity. Se comprobó compilándolos en un proyecto `net9.0` aparte. |
| No guardar una "foto" del estado | La partida es `GameSetup` + lista de `GameAction` (solo se agregan acciones aceptadas). `GameEngine.Replay` reconstruye todo, de forma determinista. Una acción inválida al reproducir lanza una excepción: el registro está corrupto. |
| Que el azar lo decida el servidor y quede registrado | `CreateSetup(seed)` sortea los mazos y el primer jugador **una sola vez** y lo deja escrito en el `GameSetup`. Después el motor no usa azar, así que el replay no depende de la versión de .NET. **El `GameSetup` es secreto del servidor** (contiene el orden de los mazos): jamás se envía a un cliente. |
| Mostrar a cada jugador solo lo suyo | `CreateView(estado, jugador)` entrega su mano y el número de cartas del rival, nada más. `RedactFor(eventos, jugador)` tapa qué carta robó el rival. |
| Contar al cliente lo que pasó desde su última consulta | `Replay` devuelve los eventos de cada acción (`EventsPerAction`). Con "cuántas acciones he visto" se devuelve solo lo nuevo. |
| Reintentos de red | `DeployTroop` y `EndStep` aceptan `expectedTurnNumber`. Si no coincide con el turno actual devuelven `StaleTurn` y no cambian nada. Es necesario porque al cambiar de ronda el mismo jugador juega dos pasos seguidos, y un `EndStep` reenviado se aplicaría otra vez. |
| Identidad | La acción lleva el `PlayerId` como argumento. El servidor lo obtiene de la sesión autenticada (nunca de un dato que mande el cliente) y el motor comprueba que sea su turno. |
| Abandono y tiempo por paso | No están en el motor, a propósito: dependen del reloj del servidor. Se resuelven por encima del resultado del replay, sin meterlos como una acción falsa en la lista. |

**Dos cosas que el motor no resuelve y habrá que decidir** cuando llegue ese momento:

1. **El catálogo de cartas.** El motor recibe `CardDefinition`, pero las cartas hoy viven como `ScriptableObject` dentro de Unity, que un servidor no puede leer. Hará falta exportarlas (por ejemplo a un JSON generado desde un menú del Editor) y **guardar con cada partida la versión del catálogo**: si se cambia el ataque de una carta a mitad de partida, el replay daría otro resultado.
2. **Qué datos son privados.** El `GameSetup` y la lista de acciones son del servidor. A los clientes solo les llega la vista.

El profesor compartirá la guía del multijugador cuando termines las tareas 1 a 9.
