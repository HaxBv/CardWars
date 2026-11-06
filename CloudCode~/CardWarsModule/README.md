# CardWarsModule (Cloud Code)

Módulo de Cloud Code (C#, .NET 9) que convierte CardWars en un juego **online por turnos con servidor autoritativo**. El sufijo `~` de `CloudCode~/` hace que Unity ignore esta carpeta: es un proyecto .NET aparte, que se despliega a Cloud Code.

> **Estado:** compila contra el SDK real de Cloud Code y su lógica tiene 34 pruebas (`dotnet test`). **No se ha desplegado ni probado contra un proyecto de Unity Gaming Services**: las actividades P11 a P21 de [`Docs/PRUEBAS.md`](../../Docs/PRUEBAS.md) sirven para comprobarlo.

## La idea en una frase

**El cliente pide, el servidor decide.** El cliente nunca cambia el estado de la partida: envía una intención ("despliega esta carta aquí", "termino mi paso"); el servidor la valida con las mismas reglas del juego (`Assets/CardWars/Rules/`), la guarda y avisa al rival con un mensaje. El rival no confía en el mensaje: solo lo usa como señal para volver a pedir su vista de la partida.

```
Cliente A ─CreateMatch(requestId)──────────▶ Cloud Code ─▶ Cloud Save (privado): setup secreto
Cliente B ─JoinMatch(código)───────────────▶ Cloud Code ─▶ push "match_updated" ──▶ Cliente A
Cliente A ─PlayMove(deploy|end, turno)─────▶ Cloud Code: carga, reproduce, valida, guarda
                                              └─▶ push "match_updated" {matchId} ──▶ Cliente B
Cliente B ─GetMatchState(matchId, vistas)──▶ Cloud Code ─▶ SU vista (sin mano ni mazo del rival)
```

## Estructura

```
CloudCode~/
  CardWarsModule/
    CardWarsModule.csproj      net9.0. Incluye Assets/CardWars/Rules/*.cs con <Compile Include>: las reglas NO se duplican
    ModuleConfig.cs            registra IGameApiClient
    GameProtocol.cs            versión del contrato (gemela en el cliente Unity, se sube en los dos a la vez)
    Dtos.cs                    MatchRecord (lo que se guarda, SECRETO) y MatchStateDto / MatchResponse (lo que ve el cliente)
    CardCatalog.cs             catálogo de cartas y configuración, versionados (Data/cards.<versión>.json, recurso embebido)
    Ports.cs                   interfaces (IMatchStore, IMatchNotifier, IClock) e ids de partida
    MatchService.cs            LA AUTORIDAD: CreateMatch / JoinMatch / PlayMove / GetMatchState / LeaveMatch
    CloudSaveMatchStore.cs     guarda el registro en Cloud Save Custom Data PRIVADO (adaptador del SDK)
    CloudCodePushNotifier.cs   envía el push "match_updated" (adaptador del SDK)
    CardWarsFunctions.cs       las funciones [CloudCodeFunction] que llama el cliente (delgadas)
    Data/cards.v1.json         catálogo de MUESTRA (6 cartas inventadas): hay que reemplazarlo (tarea 11)
  CardWarsModule.Tests/        pruebas de la autoridad con almacén y push en memoria
```

La lógica vive en `MatchService`, que **no conoce el SDK**: solo habla con interfaces. Así la autoridad se prueba sin desplegar nada, y los adaptadores (`CloudSave...`, `CloudCodePushNotifier`) son lo único que toca la nube.

## Funciones (el contrato con el cliente)

Quién llama es **siempre `context.PlayerId`**. Ninguna función recibe un `playerId` como parámetro: si lo hiciera, un jugador podría hacerse pasar por otro. Todas reciben `protocolVersion` y devuelven un `MatchResponse { ok, errorCode, state }`.

| Función | Parámetros | Qué hace |
|---|---|---|
| `GetGameConfig` | — | Entrega el catálogo de cartas y la configuración con su versión. El cliente **no** lee las suyas: usa las del servidor. |
| `CreateMatch` | `requestId` | Crea una partida a la espera de rival. El `matchId` (8 letras, sirve de código para compartir) sale de un hash de `(quien crea, requestId)`, así que reintentar con el mismo `requestId` devuelve la misma partida. El servidor sortea los mazos y el primer jugador y los guarda. |
| `JoinMatch` | `matchId` | El rival se une. Los lados se asignan por orden alfabético de los ids (el primero es `Player1`). Avisa al creador por push. |
| `PlayMove` | `matchId`, `kind` (`deploy`/`end`), `cardId`, `lane`, `slot`, `expectedTurnNumber`, `actionsSeen` | Valida y aplica la acción (ver abajo). Avisa al rival. |
| `GetMatchState` | `matchId`, `actionsSeen` | Devuelve **tu** vista y los eventos que aún no has visto. Sirve tras un push, al reconectar y como sondeo de respaldo. También resuelve el plazo vencido. |
| `LeaveMatch` | `matchId` | Abandono: el rival gana por `forfeit`. Si aún no había rival, la partida se cancela. |

### Pasos de `PlayMove`

1. Carga el registro de la partida.
2. Comprueba que `context.PlayerId` **es jugador de esa partida** (`not_a_player`).
3. **Reproduce** la partida: `Replay(setup, acciones)` con el motor y el catálogo de la versión con la que empezó.
4. Mira el **reloj del servidor**: si el plazo del paso venció, el jugador al que le tocaba ya perdió (`match_over`).
5. Traduce el mensaje a una acción del motor y la **valida con las reglas**: turno esperado (`stale_turn`), que sea su turno (`not_your_turn`), que la carta esté **en su mano** (`card_not_in_hand`), terreno, espacios... Si es inválida, no se guarda nada.
6. Agrega la acción a la lista (solo se agregan), reinicia el plazo y guarda con candado de escritura (`conflict` si otra llamada guardó antes).
7. Envía el push al rival y devuelve **tu** vista.

## Qué sale del servidor y qué no

| Sale al cliente | NUNCA sale |
|---|---|
| Tu mano, la mesa (pública), vida y escudos de ambos portales, cuántas cartas tiene el rival en mano y mazo | La mano y el orden del mazo del rival |
| Los eventos nuevos de la partida; los robos del rival van con `cardId = -1` | El `Setup` (el sorteo de los mazos y del primer jugador) y la lista cruda de acciones |

El registro se guarda en **Cloud Save Custom Data privado** (`SetPrivateCustomItemAsync`). Contiene los mazos, así que ningún jugador debe poder leerlo ni escribirlo con su token. Si en el futuro hay monedas, nivel o ranking, lo que decide el servidor va en **Protected** o **Private**, nunca en **Default** (que el propio jugador puede escribir).

## Tiempo y abandono

Todo con el reloj del servidor, sin un temporizador por partida (Cloud Code no tiene temporizadores):

- **Plazo por paso:** `stepTimeoutSeconds` (180 por defecto, en el catálogo). Si el jugador al que le toca no actúa a tiempo, pierde por `timeout`. Se detecta cuando cualquiera de los dos llama a `GetMatchState` o `PlayMove`.
- **Abandono voluntario:** `LeaveMatch`.
- Ambos se aplican **por encima del replay**, sin inyectar acciones falsas en la lista.

## Versionado

- **`GameProtocol.Version`** (aquí y en el cliente): si cambia el contrato de una función, se sube en los dos. El servidor rechaza con `protocol_mismatch` a un cliente de otra versión.
- **`CatalogVersion`:** cada partida guarda con qué catálogo empezó. Si se cambia el ataque de una carta a mitad de partida, el replay daría otro resultado. Por eso **un catálogo publicado no se edita**: se crea otro archivo (`cards.v2.json`) y las partidas nuevas lo usan, mientras las viejas siguen con el suyo.

## Cómo probarlo

Requiere el SDK de .NET 9 (o superior).

```bash
cd CloudCode~/CardWarsModule.Tests
dotnet test
```

Deben pasar las 34 pruebas. Para ver que protegen algo, rompe una regla a propósito (por ejemplo, quita la comprobación de `not_a_player` en `PlayMoveAsync`) y mira cuáles fallan.

## Tareas para seguir desarrollando

Las que dicen 🔎 dependen de un proyecto de Unity Gaming Services real.

- [ ] **11. Exportar el catálogo de verdad.** `Data/cards.v1.json` tiene cartas de muestra con ids inventados. Escribe un menú del Editor (`CardWars/Exportar catálogo`) que recorra `CardDataBase.TroopCardByID` y genere `cards.v2.json` con el `id`, `attack`, `health`, `maxDeckCopies` y las pasivas `hasTeam`, `allTerrain`, `highground`, `aquatic` de cada `TroopCardData`. **Los ids deben ser los de tu base de datos**, porque el cliente enviará esos ids en `PlayMove`.
- [ ] **12. 🔎 Desplegar el módulo.** Enlaza tu proyecto de Unity a tu proyecto de UGS, crea el archivo de referencia al módulo que pide `Window > Deployment` y despliégalo. Anota los pasos exactos que hiciste en este README.
- [ ] **13. Cliente: `OnlineMatchService`.** Un servicio en Unity que haga `UnityServices.InitializeAsync`, el login anónimo y llame a las funciones con `CloudCodeService.Instance.CallModuleEndpointAsync<MatchResponse>("CardWarsModule", "PlayMove", args)`. Copia los DTOs (`MatchResponse`, `MatchStateDto`...) y `GameProtocol.Version` al cliente. Ojo: `JsonUtility` no lee propiedades, usa Newtonsoft (ver P6).
- [ ] **14. 🔎 Push y sondeo.** Suscríbete con `CloudCodeService.Instance.SubscribeToPlayerMessagesAsync`; al recibir `match_updated`, llama a `GetMatchState(matchId, actionsSeen)`. Añade un sondeo de respaldo cada ~10 s. Un push perdido nunca debe dejar la partida colgada.
- [ ] **15. Pantallas.** Crear partida (muestra el código), unirse con código, pantalla de espera, botón "terminar paso" que llame a `PlayMove(kind: "end")`, y reproducir `NewEvents` con las animaciones de `CombatManager` (tarea 8 de `Docs/MOTOR-DE-REGLAS.md`).
- [ ] **16. Reintentos.** Si `PlayMove` devuelve `conflict`, reintenta con **el mismo** `expectedTurnNumber`. Si devuelve `stale_turn`, pide `GetMatchState` y no reenvíes nada.
- [ ] **17. Configuración sin recompilar.** Lee el plazo por paso desde Remote Config (con `GetGameConfig`) en vez del JSON.
- [ ] **18. Barrido de partidas abandonadas.** Una partida creada a la que nadie se une, o abandonada por los dos, queda guardada para siempre. Diseña un barrido programado (un trigger de Cloud Code) que las limpie.
- [ ] **19. Hechizos, dominios y habilidades** en el motor (hoy los pasos 3 y 4 solo permiten terminar el paso).
- [ ] **20. Mover a Protected/Private** cualquier moneda, nivel o ranking que añadas, calculado en el servidor y una sola vez al terminar la partida.
