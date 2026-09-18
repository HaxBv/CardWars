# 03_TODO.md — CardWars

## 🔴 CRÍTICO

### [CRITICO] Revisar firma de AbilityEffectExecutor.Execute()
- **Descripción**: Verificar que el método Execute(TriggerType, EffectType, AbilityData, Tropa, BoardManager) sea consistente con cómo se llama en RuntimeAbility.Inicializar().
- **Estado**: ABIERTO
- **Dependencias**: RuntimeAbility.cs, AbilityEffectExecutor.cs
- **Prioridad**: Alta - bloquea ejecución de habilidades

### [CRITICO] Implementar ejecutores de habilidades faltantes
- **Descripción**: Faltan registradores para los triggers: OnTakeDamage, OnKill, OnDeath, OnUnitDeath, Fusion, Evolution y otros.
- **Estado**: ABIERTO
- **Dependencias**: AbilityRegistraror.cs
- **Prioridad**: Alta - sistema de habilidades incompleto

## 🟠 IMPORTANTE

### [IMPORTANTE] Crear ScriptableObjects de tipo Spell
- **Descripción**: Crear activos SpellCardData en el proyecto. Las carpetas existen pero están vacías.
- **Estado**: PENDIENTE
- **Dependencias**: Nenuna
- **Prioridad**: Media - necesario para sistema completo de cartas

### [IMPORTANTE] Crear ScriptableObjects de tipo Environment
- **Descripción**: Crear activos EnvironmentCardData. Igual que Spell, carpetas vacías.
- **Estado**: PENDIENTE
- **Dependencias**: Nenuna
- **Prioridad**: Media - necesario para sistema completo de cartas

### [IMPORTANTE] Implementar sistema de objetivo para OnAttack
- **Descripción**: Completar la lógica de target selection en el executor de OnAttack + DealDamage. Actualmente hay TODO para implementar objetivo específico.
- **Estado**: INVESTIGANDO
- **Dependencias**: AbilityEffectExecutor.cs, AbilityRegistrator.cs
- **Prioridad**: Alta - combate incompleto

### [IMPORTANTE] Implementar interfaz de UI básica
- **Descripción**: Botón "Terminar Turno" y display de fase actual.
- **Estado**: PENDIENTE
- **Dependencias**: PhasesSystem.cs, Canvas
- **Prioridad**: Media - necesario para jugar

## 🟡 PENDIENTE

### [PENDIENTE] Probar despliegue de carta con habilidades
- **Descripción**: Verificar que TropaEnTablero.AlEntrarAlTablero() cree correctamente los RuntimeAbility y ejecuten efectos.
- **Estado**: PENDIENTE
- **Dependencias**: TropaEnTablero.cs, AbilityRegistrator.cs, RuntimeAbility.cs
- **Prioridad**: Media

### [PENDIENTE] Implementar sistema de carriles y alcance
- **Descripción**: Asegurar que laneRadius y ContarEnemigosEnCarril funcionen correctamente.
- **Estado**: PENDIENTE
- **Dependencies**: BoardManager.cs, Tropa.cs
- **Prioridad**: Media

### [PENDIENTE] Agregar más tropas ScriptableObjects
- **Descripción**: Crear más activos TroopCardData aparte de Konquest y VirusFox.
- **Estado**: PENDIENTE
- **Dependencias**: Project Settings, ScriptableObject workflow
- **Prioridad**: Baja

## 🟢 MEJORAS

### [MEJORA] Optimizar diccionario de ejecutadores
- **Descripción**: Considerar usar KeyValuePair cache o structs optimizados en lugar de tuplas para las claves del diccionario.
- **Estado**: IDEA
- **Prioridad**: Baja

### [MEJORA] Añadir eventos de progreso de fase
- **Descripción**: Más eventos detallados en PhasesSystem para UI (ej: OnFaseTropoIniciada, OnFaseCombateProgreso).
- **Estado**: IDEA
- **Prioridad**: Baja

## 💡 IDEAS

### [IDEA] Sistema de calidad de cartas (Quality)
- **Descripción**: Añadir un campo 'quality' a CardData que afecte el tamaño de la imagen o efectos visuales al desplegar.
- **Estado**: No decidido. Requiere definición de diseño visual.
- **Prioridad**: Muy baja - concepto futuro

### [IDEA] Sistema de experiencia y nivelación de tropas
- **Descripción**: Las tropas ganan experiencia al combatir y suben de nivel, aumentando estadísticas base.
- **Estado**: Idea inicial. No se ha empezado a diseñar.
- **Prioridad**: Muy baja - concepto futuro

### [IDEA] Sistema de mana por turno
- **Descripción**: Cada jugador tiene un recurso mana que aumenta por turno para jugar cartas.
- **Estado**: Idea inicial. El sistema de costo ya existe en CardData pero no hay mecánica de generación de mana.
- **Prioridad**: Baja - requeriría nuevo manager

### [IDEA] Sistema de posiciónamiento diagonal
- **Descripción**: Permitir tropas en posiciones diagonales o carriles múltiples ocupados.
- **Estado**: Idea conceptual. El sistema actual asume carriles lineales simples.
- **Prioridad**: Muy baja