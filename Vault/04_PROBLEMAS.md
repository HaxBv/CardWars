# 04_PROBLEMAS.md — CardWars

## Problema 1 — Firmabilidad mismatched entre RuntimeAbility y AbilityEffectExecutor

**Fecha:** 2026-09-18

**Descripción:**
En `RuntimeAbility.Inicializar()` se llama a `AbilityEffectExecutor.Execute(datosOriginales.Trigger, effect.EffectType, datosOriginales, poseedor, BoardManager.Instance)`. Sin embargo, la firma del método es `Execute(TriggerType trigger, EffectType effect, AbilityData data, Tropa target, BoardManager board)`. El problema es que `datosOriginales` es de tipo `AbilityData` (que es la clase interna de `CardData`), pero el nombre sugiere que debería ser la referencia a los datos originales de la carta (CardData), no solo los AbilityData.

**Síntomas:**
Compilación correcta ya que los tipos coinciden, pero posible confusión lógica al pasar datos de AbilityData en lugar de CardData completo. Si algún executor necesita datos de CardData (como el nombre de la carta, costo, etc.), no tendría acceso directo.

**Causa conocida:**
Diseño inconsistente entre RuntimeAbility (que almacena `AbilityData datosOriginales`) y la firma del executor.

**Hipótesis:**
El diseño original intentaba pasar solo los datos de la habilidad, pero algunos efectos pueden necesitar información del CardData padre. Se debería pasar `this.datosOriginales` que ya es `AbilityData`, o cambiar el parámetro del executor.

**Soluciones probadas:**
Ninguna aún. Se está analizando el flujo actual.

**Solución aplicada:**
Ninguna. El problema está en revisión. Se documenta aquí para evitar que se pierda el contexto.

**Estado:** ABIERTO

---

## Problema 2 — Faltan ejecutores para la mayoría de triggers/efectos

**Fecha:** 2026-09-18

**Descripción:**
Solo se han registrado 4 combinaciones de (trigger, effect) en AbilityEffectExecutor:
1. Passive + GainArmor
2. EndTurn + Heal
3. EndTurn + DealDamage
4. OnAttack + DealDamage
5. OnAttack + GainAttack

Faltan por registrar: OnDeploy (SummonCard, DrawCard), OnTakeDamage, OnKill, OnDeath, OnUnitSummoned, OnUnitDeath, Fusion, Evolution, y muchas más combinaciones.

**Síntomas:**
Si se intenta usar una habilidad con un trigger no registrado, se mostrará un warning en consola pero el juego no fallará. El sistema está diseñado para ser extensible, pero está muy incompleto.

**Causa conocida:**
El método `RegisterBasicAbilities()` en `AbilityRegistrator.cs` fue iniciado pero no completado. Solo se cubrieron los casos inmediatos necesarios para el prototipo básico.

**Hipótesis:**
Se completará gradualmente a medida que se implementen más tipos de cartas y efectos.

**Soluciones probadas:**
Ninguna. Se requiere completar el registro de ejecutores.

**Solución aplicada:**
Ninguna. Pendiente de implementar.

**Estado:** ABIERTO

---

## Problema 3 — Carpetas de ScriptableObjects vacías

**Fecha:** 2026-09-18

**Descripción:**
Las carpetas `Assets/CardsWars/Data/ScriptableObjects/Spell` y `Assets/CardsWars/Data/ScriptableObjects/Environment` existen pero no contienen ningún archivo `.asset`. Solo hay TroopCardData (Konquest, VirusFox).

**Síntomas:**
No se pueden crear cartas de tipo Spell o Environment en el proyecto actualmente. El sistema de cartas es incompleto sin estos tipos.

**Causa conocida:**
Aún no se han creado los activos ScriptableObjects. Las carpetas pueden haberse creado por anticipado o por generación posterior.

**Hipótesis:**
Se crearán cuando se diseñen las mecánicas de hechizos y entornos.

**Soluciones probadas:**
Ninguna. Se pueden crear desde Unity Editor: Right-click → Create → CardWars → SpellCardData / EnvironmentCardData.

**Solución aplicada:**
Ninguna. Documentado para recordatorio futuro.

**Estado:** ABIERTO

---

## Problema 4 — BoardManager no persiste entre escenas

**Fecha:** 2026-09-18

**Descripción:**
BoardManager usa un patrón Singleton simple (`if (instancia == null) ... else Destroy(gameObject)`). Si se carga una nueva escena, la instancia se destruirá a menos que se use DontDestroyOnLoad.

**Síntomas:**
Los datos de las tropas (listas de aliadas/enemigas) se pierden al cambiar de escena.

**Causa conocida:**
Patrón Singleton básico sin consideraciones de persistencia de escena.

**Hipótesis:**
Se deberá decidir si BoardManager debe persistir entre escenas o recrearse en cada una.

**Soluciones probadas:**
Ninguna.

**Solución aplicada:**
Ninguna. Documentado para decisión futura.

**Estado:** ABIERTO