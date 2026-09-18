# Resumen de la configuración de Vault y comandos

## Archivos de Vault creados

Estructura completa en `C:\Users\JEREMY\OneDrive\Desktop\Jeremy\ProyectosPersonales\UnityGames\CardWars\Vault/`:

### 00_ESTADO_ACTUAL.md
Estado resumido del proyecto con:
- Estado general (juego de cartas en desarrollo temprano a medio)
- Sistema actual (fases de 5 pasos)
- Última tarea realizada (registró ejecutores de habilidades)
- Último cambio (Actualización de AbilityRegistrator.cs)
- Último problema (firmabilidad entre RuntimeAbility y AbilityEffectExecutor)
- Próximo objetivo recomendado
- Tareas pendientes inmediatas y a medio plazo
- Estado de sistemas importantes (fases, tablero, cartas, habilidades, combate, UI)

### 01_ARQUITECTURA.md
Documentación de la arquitectura REAL del proyecto:
- Sistemas principales: PhasesSystem, BoardManager, AbilityEffectExecutor, RuntimeAbility
- Managers: BoardManager (singleton), GameManager (placeholder)
- ScriptableObjects: CardData, AbilityData, EffectData, TroopCardData, SpellCardData, EnvironmentCardData
- Datos: ScriptableObjects en Assets/CardWars/Data/ScriptableObjects/
- Gameplay: sistema de 5 fases, carriles, combate automático
- Cartas: definidas mediante ScriptableObjects con abilities y efectos
- Habilidades: triggers (StartTurn, EndTurn, OnDeploy, OnAttack, OnDeath, etc.), efectos (GainAttack, DealDamage, DrawCard, etc.), filtros y condiciones
- Lanes: carriles indexados con laneRadius
- Combate: daño escudo→armadura→vida
- Dependencias entre sistemas (TropaEnTablero → BoardManager, AbilityRegistrator, etc.)

### 02_DECISIONES.md
Decisiones importantes documentadas:
1. 2026-09-18 — Sistema de cartas basado en ScriptableObjects
2. 2026-09-18 — Sistema de fases en 5 pasos
3. 2026-09-18 — Singleton BoardManager
4. 2026-09-18 — Mitigación de daño: escudo → armadura → vida
5. 2026-09-18 — Estructuración de AbilityData y EffectData
6. 2026-09-18 — Registros de habilidades en AbilityRegistrator

### 03_TODO.md
Lista de tareas persistente con 4 categorías:
- 🔴 CRÍTICO: 2 tareas (firma de executor, ejecutores faltantes)
- 🟠 IMPORTANTE: 4 tareas (Spell/Environment ScriptableObjects, objetivo OnAttack, UI)
- 🟡 PENDIENTE: 3 tareas (probar despliegue, carriles, más tropas)
- 🟢 MEJORAS: 2 ideas (optimización diccionario, eventos de fase)
- 💡 IDEAS: 4 conceptos (calidad de cartas, experiencia, mana, posición diagonal)

### 04_PROBLEMAS.md
4 problemas registrados con fecha, descripción, síntomas, causa, hipótesis, soluciones probadas/estado:
1. Firmabilidad mismatched entre RuntimeAbility y AbilityEffectExecutor (ABIERTO)
2. Faltan ejecutores para la mayoría de triggers/efectos (ABIERTO)
3. Carpetas de ScriptableObjects vacías (ABIERTO)
4. BoardManager no persiste entre escenas (ABIERTO)

### 05_MEMORIA_PROYECTO.md
Contexto general estable de CardWars:
- Concepto: juego de cartas estratégico por turnos con colocación en carriles
- Género: cartas estratégicas / tácticas en tablero
- Mecánicas principales: sistema de 5 fases, carriles, daño escudo→armadura→vida, habilidades por trigger
- Filosofía de diseño: datos separados de lógica, sistema extensible, turns claros, mitigación escalonada
- Estructura de carpetas y convenciones de código
- Decisiones permanentes (5 decisiones del 2026-09-18)
- Restricciones y preferencias de desarrollo

### Bitacoras/README.md
Documentación del directorio de bitácoras diarias formateo YYYY-MM-DD.md.

### Bitacoras/2026-09-18.md
Primera bitácora diaria con:
- Hora de inicio (18:30)
- Objetivos de la sesión
- Trabajo realizado (creación de estructura, análisis de scripts)
- Archivos modificados (7 nuevos archivos + configuración)
- Problemas encontrados (4 problemas)
- Soluciones aplicadas
- Decisiones tomadas (5 decisiones)
- Estado al cerrar sesión
- Próximo paso recomendado
- Notas adicionales

## Cómo funcionan /AbrirSesion y /CerrarSesion

### /AbrirSesion
**Función:** Recuperar el contexto de desarrollo antes de comenzar a trabajar.

**Proceso:**
1. **PASO 1 — Leer Vault:** Lee los archivos en orden:
   - Vault/00_ESTADO_ACTUAL.md
   - Vault/05_MEMORIA_PROYECTO.md
   - Vault/03_TODO.md
   - Vault/04_PROBLEMAS.md
   - Vault/02_DECISIONES.md (cuando sea necesario)
   - La última bitácora disponible (Vault/Bitacoras/YYYY-MM-DD.md)

2. **PASO 2 — Determinar dónde quedamos:** Analiza la información y determina:
   - Qué se estaba haciendo
   - Qué fue lo último que se modificó
   -Qué quedó incompleto
   -Qué problemas siguen abiertos
   -Qué tarea probablemente corresponde continuar

3. **PASO 3 — Verificar contra el proyecto:** Inspeccionar SOLO los archivos del proyecto de Unity necesarios (no leer todo el proyecto). Buscar scripts, prefabs, ScriptableObjects o escenas relacionados con el contexto recuperado. Si Vault contradice al proyecto, indicar la discrepancia y actualizar Vault.

4. **PASO 4 — Presentar resumen:** Muestra un resumen formateado con:
   - Última sesión (fecha y hora)
   - En qué quedamos (resumen)
   - Últimos cambios (lista breve)
   - Problemas abiertos (lista)
   - Tareas pendientes (lista)
   - Discrepancias detectadas (si existen)
   - Siguiente paso recomendado

5. **PASO 5 — Mantener continuidad:** Usar el contexto recuperado en la conversación actual. No tratar cada mensaje como conversación independiente. Si el usuario dice "continúa con lo anterior", usar el contexto de la sesión actual.

**Ejemplo de salida:**
```
## CARDWARS — SESIÓN

**Última sesión:**
Fecha y hora.

**En qué quedamos:**
Resumen.

**Últimos cambios:**
Lista breve.

**Problemas abiertos:**
Lista.

**Tareas pendientes:**
Lista.

**Discrepancias detectadas:**
Si existen.

**Siguiente paso recomendado:**
Explicar qué sería lógico hacer a continuación y por qué.
```

### /CerrarSesion
**Función:** Guardar el estado completo de la sesión actual.

**Proceso:**
1. **PASO 1 — Analizar la sesión:** Revisar la conversación actual y determinar:
   - Qué pedí
   -Qué hiciste
   -Qué hice yo
   -Qué cambios se realizaron
   -Qué archivos fueron modificados
   -Qué decisiones tomamos
   -Qué problemas encontramos
   -Qué problemas solucionamos
   -Qué problemas siguen abiertos
   -Qué quedó pendiente
   -Qué ideas aparecieron
   -Qué experimentos fallaron
   -Qué debería hacerse después

2. **PASO 2 — Verificar archivos modificados:** Cuando sea posible, verificar realmente los cambios realizados en el proyecto usando git si está disponible, o la información de la sesión y la inspección de archivos. Distinguir entre cambios realizados por OpenCode y cambios realizados manualmente por el usuario.

3. **PASO 3 — Crear/actualizar bitácora:** Crear:
   - Vault/Bitacoras/YYYY-MM-DD.md
   - Si ya existe una bitácora de ese día, actualizarla en lugar de crear otra innecesariamente
   - Registrar la hora de cierre

4. **PASO 4 — Actualizar memoria:** Actualizar cuando corresponda:
   - 00_ESTADO_ACTUAL.md
   - 03_TODO.md
   - 04_PROBLEMAS.md
   - 02_DECISIONES.md
   - 01_ARQUITECTURA.md
   - 05_MEMORIA_PROYECTO.md
   - NO modificar estos archivos si no existe información nueva relevante
   - Evitar duplicar información innecesariamente

5. **PASO 5 — No perder información:** Si durante la sesión ocurrió algo importante, debe quedar registrado aunque parezca pequeño. Especialmente:
   - Bugs
   - Soluciones
   - Decisiones
   - Cambios arquitectónicos
   - Sistemas nuevos
   - Cambios de diseño
   - Enfoques descartados
   - Errores que no debemos repetir

6. **PASO 6 — Resumen final:** Mostrar:
   ```
   ## SESIÓN CERRADA

   **Fecha:**
   **Hora:**

   ### Trabajo realizado
   ...

   ### Archivos modificados
   ...

   ### Decisiones tomadas
   ...

   ### Problemas resueltos
   ...

   ### Problemas pendientes
   ...

   ### Próximo objetivo
   ...

   ### Archivos de Vault actualizados
   ...
   ```

## Cómo utilizarlos

### Flujo de trabajo típico:

**Al iniciar una nueva sesión de trabajo:**
1. Ejecutar `/AbrirSesion`
2. Revisar el resumen presentado
3. Continuar con el trabajo, usando el contexto recuperado
4. Cuando terminen de trabajar, ejecutar `/CerrarSesion`

**Ejemplo:**
```
Usuario: /AbrirSesion
AI: [muestra el resumen de la sesión anterior y el estado actual]

Usuario: "Voy a implementar el ejecutador de Passive + GainArmor"
AI: [usa el contexto de Vault para recordar el diseño actual y no duplicar trabajo]

Usuario: "Terminé por hoy"
AI: /CerrarSesion
AI: [muestra el resumen de la sesión cerrada y guarda la bitácora]
```

### Puntos clave:
- **Vault no es la fuente de verdad del código:** La jerarquía es: 1) Archivos reales del proyecto Unity, 2) Estado real del repositorio/Git, 3) Vault, 4) Memoria de la conversación. Si Vault dice "CardManager ya está terminado" pero el código demuestra lo contrario, el código es la realidad y se debe corregir Vault.
- **Lectura del proyecto:** /AbrirSesion NO debe leer todo el proyecto de Unity automáticamente. El flujo es: Vault → identificar contexto → localizar archivos relevantes → inspeccionar solamente esos archivos.
- **Bitácoras diarias:** Cada sesión de trabajo debe tener una bitácora en Vault/Bitacoras/YYYY-MM-DD.md. Si ya existe una de ese día, actualizarla en lugar de crear otra.
- **Continuidad:** Durante la conversación actual, el agente debe usar el contexto recuperado de Vault y no tratar cada mensaje como una conversación independiente.