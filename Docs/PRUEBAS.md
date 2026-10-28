# CardWars — Actividades de testing

Estas actividades son para ti, Jeremy. Cada una te pide **probar algo en Unity**, comparar con un resultado esperado y **reportar** lo que veas (aunque todo salga bien). Tus reportes sirven de retroalimentación: dicen qué funciona de verdad en tu máquina y qué hay que corregir. Responde en el PR o en un archivo `Docs/REPORTE-PRUEBAS.md` en tu rama.

**Cómo reportar** (una línea por actividad): `P1: OK` o `P1: FALLA`, y si falla: el paso donde falló, el mensaje exacto de la Consola de Unity (copiado, no resumido), la versión de Unity y, si ayuda, una captura.

> Estas pruebas se prepararon sin abrir el Editor de Unity: las marcadas con 🔎 verifican justo lo que no pudo comprobarse desde fuera. Los resultados "esperados" vienen de compilar y ejecutar el motor con .NET, y de la documentación de Unity.

## Parte A — El motor de reglas (`Assets/CardWars/Rules/`)

### P1 🔎 Abre el proyecto y compila
- **Pasos:** abre el proyecto con esta rama. Espera a que Unity termine de importar.
- **Esperado:** la Consola no muestra errores rojos. Aparecen archivos `.meta` nuevos en `Assets/CardWars/Rules/` (y en su subcarpeta `Tests`).
- **Reporta si falla:** el primer error de la Consola, completo. Si el error es de `Packages/manifest.json` (ruta `C:/Users/JEREMY/...`), es el problema conocido del paquete local; di cómo lo resolviste.
- **Haz commit** de los `.meta` nuevos.

### P2 🔎 Corre las 49 pruebas EditMode
- **Pasos:** `Window > General > Test Runner > EditMode`. Pulsa `Run All`.
- **Esperado:** dos fixtures (`GameEngineTests` y `ReplayTests`), **49 pruebas en verde**, en menos de 2 segundos.
- **Reporta si falla:** el nombre de cada prueba en rojo y su mensaje. No es normal que falle ninguna: significaría que algo del Editor se comporta distinto del .NET con el que se verificó.

### P3 🔎 El motor no puede ver Unity
- **Pasos:** abre `Assets/CardWars/Rules/Types.cs` y añade arriba `using UnityEngine;`. Vuelve a Unity.
- **Esperado:** error de compilación (`The type or namespace name 'UnityEngine' could not be found`). Esa es la garantía de que las reglas pueden correr en un servidor. **Deshaz el cambio.**
- **Reporta si falla:** si compila sin error, mira `CardWars.Rules.asmdef`: debe tener `"noEngineReferences": true`.
- **Pregúntate:** ¿por qué esto es importante para un juego online?

### P4 Rompe una regla a propósito y mira cómo falla un test
- **Pasos:** en `GameEngine.cs`, método `FindTargetSlot`, cambia el bucle `for (int slot = LaneState.SlotsPerSide; slot >= 1; slot--)` por `for (int slot = 1; slot <= LaneState.SlotsPerSide; slot++)`. Corre `Run All`.
- **Esperado:** fallan al menos 2 pruebas de combate (por ejemplo `Combat_TargetsTheForwardSlot2First`). **Deshaz el cambio.**
- **Reporta:** qué pruebas fallaron y qué te dice su mensaje sobre la regla rota.
- **Pregúntate:** ¿qué prueba habrías escrito tú para detectar este error?

### P5 La mano del rival no sale en la vista
- **Pasos:** lee la prueba `View_ShowsOwnHand_ButOnlyTheCountOfTheOpponentHand` y `Redact_HidesWhichCardTheOpponentDrew`. Después, en un test nuevo tuyo, crea una partida con `engine.StartGame(1)`, pide `CreateView(state, PlayerId.Player1)` y escribe un `Assert` que compruebe que `view.OpponentHandCount` coincide con `state.Get(PlayerId.Player2).Hand.Count`.
- **Esperado:** la vista tiene `MyHand` (tus cartas) pero del rival solo `OpponentHandCount`.
- **Reporta:** si pudiste escribir la prueba sin ayuda y cuánto tardaste en entender qué campos de `PlayerView` son públicos y por qué.

### P6 🔎 ¿Qué serializador ve la vista?
- **Pasos:** en un test nuevo, ejecuta `Debug.Log(JsonUtility.ToJson(view));` con una vista creada por `CreateView`.
- **Esperado según la documentación de Unity:** `JsonUtility` solo serializa **campos**, no propiedades, así que probablemente imprime `{}`. Eso es normal: `PlayerView` usa propiedades para que otros serializadores (`System.Text.Json`, Newtonsoft) la incluyan.
- **Reporta:** lo que imprime. Si imprime `{}`, anota la conclusión: ¿qué crees que debería hacer tu cliente online para leer la respuesta del servidor? (La capa online del siguiente paso responde esto.)

## Parte B — Dudas de reglas que solo tú puedes comprobar en el juego

Estas actividades comparan **el juego actual** con **lo que asume el motor**. Si el resultado difiere, no es un error tuyo: es una regla que hay que decidir (ver `Docs/MOTOR-DE-REGLAS.md`, "Cosas que se encontraron").

### P7 🔎 El umbral del segundo escudo del portal (30 o 31)
- **Pasos:** abre `SampleScene`, entra en Play, selecciona un `Portals` y, en el Inspector (botón de Odin), pulsa `TakeDamage` con 10. Mira el texto del portal.
- **Esperado:** el primer golpe de 10 con 50 de vida lo deja en 40 con 2 escudos. El texto muestra el umbral del **siguiente** escudo entre paréntesis. El motor dice **30**; la fórmula con `float` del juego se calculó como **31** con .NET.
- **Reporta:** el número exacto que ves, `(30 HP)` o `(31 HP)`, y la versión de Unity.

### P8 🔎 ¿Cuál es la tropa de adelante, la del espacio 1 o la del 2?
- **Pasos:** en Play, despliega una tropa con `Team` en el espacio 1 y otra en el espacio 2 de un mismo bando de un carril, y pon una tropa en el bando contrario. Avanza hasta el combate. Mira en la Consola qué tropa recibe el ataque.
- **Esperado según `CombatManager`:** recibe el golpe la del **espacio 2**. Pero `LandLane` supone que cuando muere la del espacio 1 "la de atrás avanza".
- **Reporta:** qué tropa fue atacada, en qué posición dibuja el juego cada espacio (¿cuál está más cerca del centro?) y qué crees que debería ser el "frente".

### P9 Escenario paralelo: juego vs motor
- **Pasos:** monta en la escena este escenario: carril 0, jugador con prioridad con una tropa de ataque 3 y vida 5 en el espacio 1; el rival con una tropa de ataque 3 y vida 5 en el espacio 1. Corre el combate. Después escribe un test que reproduzca el mismo escenario con `Place(...)` y `PlayToCombat(...)` (copia los ayudantes `Place` y `PlayToCombat` de `GameEngineTests.cs`).
- **Esperado:** ambas tropas terminan con 2 de vida y siguen vivas.
- **Reporta:** si el juego y el motor dan el mismo resultado. Si no, qué diferencia ves y de qué regla viene.

## Parte C — Para entender el replay

### P10 Reproduce una partida a mano
- **Pasos:** en un test nuevo: crea `var setup = engine.CreateSetup(42);`, arma una lista de cuatro acciones `GameAction.End(...)` (la primera del jugador `setup.FirstPlayer`, luego del otro, y así alternando) y llama `engine.Replay(setup, acciones)`. Compara `result.State.Step` con lo que esperas.
- **Esperado:** Tras tres `End` el `Step` vale 4; el cuarto resuelve el combate (paso 5) y deja `Step` en 1 con `TimeOfDay` en `Ocaso`.
- **Reporta:** si el resultado coincide con lo que esperabas. Explica con tus palabras por qué guardar `setup + acciones` basta para reconstruir la partida y para qué sirve que el azar esté en el `setup`.
