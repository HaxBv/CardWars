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

---

# Parte D — El módulo de Cloud Code (`CloudCode~/CardWarsModule/`)

Estas actividades son del segundo PR. Las marcadas con 🔎 necesitan un **proyecto de Unity Gaming Services propio** (el módulo se probó con almacén y push *en memoria*, pero nunca contra Cloud Save ni Cloud Code reales). Necesitas dos cuentas de jugador distintas: usa dos sesiones con inicio de sesión anónimo o dos instalaciones.

### P11 Corre las 34 pruebas del servidor
- **Pasos:** instala el SDK de .NET 9. En una terminal: `cd CloudCode~/CardWarsModule.Tests` y `dotnet test`.
- **Esperado:** `Correctas! - Con error: 0, Superado: 34`.
- **Reporta si falla:** versión de `dotnet --version` y el mensaje del primer error.

### P12 Rompe una regla del servidor y mira qué test falla
- **Pasos:** en `MatchService.cs`, método `PlayMoveAsync`, cambia `engine.Apply(replay.State, action, request.ExpectedTurnNumber)` por `engine.Apply(replay.State, action)`. Corre `dotnet test`. **Deshaz el cambio.**
- **Esperado:** falla `PlayMove_ARetriedMessage_IsStale_AndIsNotAppliedTwice`.
- **Reporta:** qué prueba falló y con tus palabras, qué problema real causaría esta línea en una partida con mala conexión.

### P13 🔎 Despliega y llama a `GetGameConfig`
- **Pasos:** despliega el módulo (tarea 12 del README del módulo). Llama a la función `GetGameConfig` desde el Dashboard de Cloud Code o desde un script de Unity.
- **Esperado:** un JSON con `catalogVersion: "v1"`, `protocolVersion: 1`, `stepTimeoutSeconds: 180` y las 6 cartas de muestra.
- **Reporta si falla:** el mensaje de error del Dashboard o de la Consola, completo. Anota también los pasos de despliegue que te hicieron falta.

### P14 🔎 Dos jugadores crean y se unen a una partida
- **Pasos:** con el jugador A: `CreateMatch` con `requestId = "r1"` y `protocolVersion = 1`. Copia el `matchId`. Con el jugador B: `JoinMatch` con ese `matchId`.
- **Esperado:** A recibe `status: "waiting"`; B recibe `status: "in_progress"` con `yourSide` y una `view` con **su** mano de 4 cartas.
- **Reporta:** el `matchId`, y si `yourSide` de A y B son distintos.

### P15 🔎 El registro está realmente privado
- **Pasos:** en el Dashboard de Cloud Save, busca el documento de Custom Data con el `matchId` (clave `state`). Después, desde un cliente con la sesión del jugador A, intenta leerlo: `CloudSaveService.Instance.Data.Custom.LoadAllAsync(matchId)`.
- **Esperado:** el documento existe en el Dashboard; el cliente **no** debe poder leerlo ni escribirlo (lista vacía o error de permisos). Contiene los mazos: si un jugador pudiera leerlo, vería las cartas del rival.
- **Reporta:** qué ve el cliente y la clase de acceso que muestra el Dashboard. Si el cliente puede leerlo, **es un fallo de seguridad grave**: no sigas, avísanos.

### P16 🔎 Intentos de trampa
- **Pasos:** con la partida de P14, prueba cada caso y anota el `errorCode`:
  1. B llama a `PlayMove` (`kind: "end"`) cuando le toca a A.
  2. Un tercer jugador C llama a `GetMatchState` y a `PlayMove`.
  3. El jugador al que le toca llama a `PlayMove` con `kind: "deploy"` y `cardId: 999`, y con un `cardId` que **no** esté en su mano.
  4. El jugador al que le toca llama con `lane: 7`.
- **Esperado:** `not_your_turn`, `not_a_player`, `card_not_in_hand` y `invalid_lane`, y la partida no cambia (`actionCount` igual).
- **Reporta:** los `errorCode` que obtuviste. Si alguno devuelve `ok: true`, explica qué enviaste.

### P17 🔎 Lo que el rival NO debe ver
- **Pasos:** llama a `GetMatchState` como A y como B. Mira todo el JSON.
- **Esperado:** cada uno ve su `myHand` y del rival solo `opponentHandCount` y `opponentDeckCount`. No aparece ningún `setup`, mazo ni lista de acciones. Tampoco debe aparecer la mano del rival en los **logs de Cloud Code** del Dashboard.
- **Reporta:** si encontraste algo de más en la respuesta o en los logs.

### P18 🔎 Reintentos: no se aplica dos veces
- **Pasos:** (a) llama dos veces a `CreateMatch` con el mismo `requestId` desde el mismo jugador. (b) Cuando le toque a A, llama dos veces seguidas a `PlayMove` (`kind: "end"`) con el mismo `expectedTurnNumber`.
- **Esperado:** (a) el mismo `matchId` las dos veces y una sola partida en Cloud Save. (b) la primera `ok: true`, la segunda `stale_turn`, y `actionCount` sube una sola vez.
- **Reporta:** los resultados.

### P19 🔎 El reloj es del servidor
- **Pasos:** crea una partida nueva y no juegues. Pasados 181 segundos, llama a `GetMatchState` como el jugador al que **no** le tocaba. (Si quieres acortar la espera, crea `cards.v2.json` con `stepTimeoutSeconds: 20` y despliega.)
- **Esperado:** `status: "finished"`, `result: "won"` y `endReason: "timeout"`. El jugador lento, al consultar, ve `lost`.
- **Reporta:** lo que ves y cuánto tardó. ¿Por qué el servidor no necesita un temporizador por partida?

### P20 🔎 Dos llamadas a la vez
- **Pasos:** con una partida en espera, haz que dos jugadores (B y C) llamen a `JoinMatch` casi al mismo tiempo.
- **Esperado:** solo uno entra; el otro recibe `match_full` o `conflict`. Nunca deben quedar tres jugadores.
- **Reporta:** el resultado de cada uno y la lista `players` del documento. Esto comprueba el *candado de escritura* del almacén; si el registro queda con tres jugadores, avísanos.

### P21 🔎 El push (cuando hagas la tarea 14)
- **Pasos:** con el cliente suscrito a los mensajes del jugador, haz que el rival juegue.
- **Esperado:** llega un mensaje `match_updated` con `{"type":"match_updated","matchId":"..."}` **sin estado**. Al recibirlo, tu cliente llama a `GetMatchState`. Apaga la suscripción y comprueba que el sondeo de respaldo (~10 s) recupera la partida.
- **Reporta:** qué recibe tu cliente y cuánto tarda el sondeo.
