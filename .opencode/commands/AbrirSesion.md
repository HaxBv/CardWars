# /AbrirSesion

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
   - Qué quedó incompleto
   - Qué problemas siguen abiertos
   - Qué tarea probablemente corresponde continuar

3. **PASO 3 — Verificar contra el proyecto:** Inspeccionar SOLO los archivos del proyecto de Unity necesarios. Buscar scripts, prefabs, ScriptableObjects o escenas relacionados con el contexto recuperado. Si Vault contradice al proyecto, indicar la discrepancia y actualizar Vault.

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