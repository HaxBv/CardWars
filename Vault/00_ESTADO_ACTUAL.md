# 00_ESTADO_ACTUAL.md — CardWars

## Estado general
Proyecto de juego de cartas en Unity llamado **CardWars**. Se encuentra en fase de desarrollo temprano a medio, con sistemas básicos de tablero, fases y cartas ScriptableObjects implementados. El objetivo es un juego de estrategia por turnos con combate automático en carriles.

## Sistema actual
Sistema de fases basado en 5 pasos (TroopTurn x2, SpellAndEnvironmentTurn x2, Combate). Las tropas se colocan en carriles y atacan automáticamente. Las habilidades se ejecutan mediante un sistema de ejecutores registrados por trigger/efecto/filtro/condición.

## Última tarea realizada
Registró ejecutores de habilidades en `AbilityRegistrator.cs` para los triggers: Passive, EndTurn, OnAttack y OnDeploy, cubriendo efectos de GainArmor, Heal, DealDamage, SummonCard y DrawCard.

## Último cambio realizado
Modificó `AbilityRegistrator.cs` para agregar lógica de condition/filter/target en los ejecutadores de habilidades. También actualizó `TropaEnTablero.cs` para inicializar habilidades automáticamente al entrar al tablero.

## Último problema encontrado
El sistema de ejecución de efectos en `RuntimeAbility.Inicializar()` itera sobre `datosOriginales.Effects` pero el executor espera `AbilityData` (que contiene la lista de efectos), lo que puede causar discrepancias en cómo se pasan los datos. Revisar la firma del método `AbilityEffectExecutor.Execute()`.

## Próximo objetivo recomendado
Continuar implementando efectos de habilidades restantes y probar el sistema de combate en carriles con tropas reales. También asegurar que los ScriptableObjects de tipo Environment y Spell tengan datos consistentes.

## Tareas pendientes inmediatas
- [ ] Revisar consistencia entre `RuntimeAbility.Inicializar()` y `AbilityEffectExecutor.Execute()` firma
- [ ] Probar despliegue de una carta con habilidades en el tablero
- [ ] Verificar que BoardManager registre correctamente tropas nuevas

## Tareas pendientes a medio plazo
- [ ] Implementar sistema de objetivo objetivo para efectos OnAttack
- [ ] Crear ScriptableObjects de tipo Spell y Environment
- [ ] Implementar sistema de evolución/fusión de cartas
- [ ] Añadir UI para terminar turno y ver fases

## Estado de sistemas importantes
- **Fases**: Implementado (5 pasos en PhasesSystem)
- **Tablero**: Implementado (BoardManager singleton con listas de tropas aliadas/enemigas)
- **Cartas (ScriptableObjects)**: Parcial (Solo TroopCardData creado: Konquest, VirusFox)
- **Habilidades**: Parcial (Ejecutadores registrados para triggers básicos, falta OnKill, OnDeath, etc.)
- **Combate**: Básico (Daño en Tropa.RecibirDaño, combate automático pendiente)
- **UI**: Pendiente
- **Guardado**: Pendiente