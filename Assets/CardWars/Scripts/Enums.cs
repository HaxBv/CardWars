public enum CardType
{
    None,
    Troop,
    Spell,
    Environment
}
public enum PlayerTurn
{
    Player1,
    Player2
}
public enum ConditionType
{
    None,

    MissingHealth,

    Fury,

    Bleed,
    TargetBleed,

    Armor,

    AlliedFamilyCount,

    AlliesInLane,

    EnemiesInLane,

    CardsInHand
}
public enum FilterType
{
    None,

    Ally,
    Enemy,

    NotSelf,

    SummonedFamily,

    AllyFamily,

    EnemyFamily,

}
public enum CardRarity
{
    None,
    Common,
    Rare,
    Epic,
    Mythic,
    Legendary,
    Divine,
    Secret,
}
public enum CardFamily
{
    None,
    Food,
    Beast,
    Chaos,
    Tech,
    Structure,
    Specialist,
    Party,
    Gadget,
    Hero,
    Villain,
    Historic,
    Toy,
    Arcane,
    Aquatic,
    Monster,
    Nature,
    Rodent,
    Flying,
    Visitor,
    Fox,
}
public enum TriggerType
{
    None,

    // Turnos
    StartTurn,
    EndTurn,

    // Carta
    OnDeploy,
    OnDraw,

    // Combate
    OnAttack,
    OnTakeDamage,
    OnKill,
    OnDeath,

    // Invocación global
    OnUnitSummoned,
    OnUnitDeath,

    // Carriles
    OnLaneChanged,
    EnemyEnteredLane,
    AllyEnteredLane,

    // Especiales
    Fusion,
    Evolution,

    // Permanente
    Passive
}
public enum EffectType
{
    GainAttack,
    GainHealth,
    GainArmor,
    GainShield,
    GainMana,
    GainAbility,
    GainInmunity,
    Heal,
    DealDamage,
    DrawCard,
    SummonCard,
    Move,
    Stun,
    Transform,
    Destroy,
    Execute,



}
public enum AbilityPassive
{
    None,
    Team,
    Freeze,
    Hunter,
    Poison,
    Charm,
    Lethal,
    AntiSpell,
    Break,
    DoubleAttack,
    PiercingAttack,
    AreaDamage,
    ThreeLineAttack,
    AllTerrain,
    Frenzy,
}
public enum TargetType
{
    None,

    // Propios
    Self,
    Player,
    EnemyPlayer,

    // Individuales
    Ally,
    Enemy,

    // Masivos
    AllAllies,
    AllEnemies,
    AllUnits,

    // Carriles
    AllyInLane,
    EnemyInLane,
    AlliesInLane,
    EnemiesInLane,

    // Selección especial
    HighestAttackEnemy,
    LowestAttackEnemy,

    HighestHealthEnemy,
    LowestHealthEnemy,

    RandomEnemy,
    RandomAlly
}
public enum TurnPhase
{
    StartTurn,
    TroopTurn,
    SpellAndEnvironmentTurn,
    Combat,
    EndTurn,
}
public enum Team
{
    None,
    Player,
    Enemy
}
public enum MarkType
{
    None,
    Bleed,
    Freeze,
    Burn,
    Terror,
    Inmunity,
    Isolated,

    //Special Marks
    Maximwolf,
    Wendy,
    Sophie,



}
