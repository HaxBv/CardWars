---
description: Retoma el proyecto donde quedamos: lee solo la carpeta del juego en Assets/CardWars y el vault, resume el estado actual y recomienda qué continuar o mejorar.
---

Comando de ENTRADA: retoma el contexto del proyecto CardWars de forma ligera.

Al iniciar, EMPIEZA tu respuesta con la frase exacta: "Te recuerdo" y luego continúa normalmente con la recapitulación y retomando donde quedó la sesión pasada.

1. Lee ÚNICAMENTE la carpeta del juego `Assets/CardWars/`:
   - `Assets/CardWars/Scripts/` (Enums, CardData*, RuntimeAbility, Tropa, TropaEnTablero, BoardManager, PhasesSystem, GameManager).
   - `Assets/CardWars/Data/ScriptableObjects/` (cartas creadas).
   - NO leas el resto del proyecto (Feel, Plugins, Library, Temp, Logs, etc.).
2. Lee el vault `Vault/`, priorizando `Vault/Notas/Bitacora/` (última nota con fecha) para ver el avance más reciente registrado.
3. Registra la HORA DE ENTRADA en `Vault/Notas/Bitacora/YYYY-MM-DD.md` (fecha de hoy):
   - Si la nota no existe, créala con una sección de la sesión actual (título `## Sesión HH:MM` usando la hora de entrada).
   - Si ya existe (sesiones previas del mismo día), añade una nueva sección `## Sesión HH:MM` con la hora actual.
4. Pregunta/recuérdale al usuario: si cambió algo FUERA de `Assets/CardWars/` (paquetes, settings, escenas, plugins), que te lo indique para revisarlo; si no, no leas nada más.
5. Entrega en español:
   - Dónde quedamos (última sesión registrada y su conclusión).
   - Qué hay implementado y qué falta.
   - Recomendaciones ordenadas por prioridad de qué continuar o mejorar.
   - Pregunta brevemente por dónde quiere empezar el usuario.