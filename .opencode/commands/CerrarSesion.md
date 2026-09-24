# /CerrarSesion

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