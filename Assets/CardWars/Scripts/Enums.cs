public enum CardType
{
    None,
    Troop,
    Spell,
    Domain
}
public enum PlayerTurn
{
    Player1,
    Player2
}
public enum ConditionType
{
    None,

    Damaged,

    Fury,


    Bleed,
    TargetBleed,

    HasArmor,

    HasShield,

    AlliedFamilyCount,

    AlliesInLane,

    EnemiesInLane,

    CardsInHand,

    IsOnHeights,
    IsOnLands,

    IsOnWater,

    SecondHit,

    IsOnEnvironment,




}
public enum FilterType
{
    None,

    Self,
    AllyUnit,
    EnemyUnit,

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
    Token,
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
    Court,
    Monster,
    Nature,
    Rodent,
    Flying, 
    Brawler,
    Darkness,
    Potion,
    Visitor,
    Ink,
    Fox,
}
public enum TriggerType
{
    None,

    // Turnos
    StartRound,
    EndRound,

    // Carta
    OnDeploy,
    OnDraw,

    // Combate
    OnHit,
    OnPortalHit,
    OnDamaged,
    OnKill,
    OnDeath,

    // Invocación global
    OnUnitSummoned,
    OnUnitDeath,
    

    // Carriles
    OnLaneChanged,
    OnEnemyEnteredOwnLane,
    OnAllyEnteredOwnLane,

    // Especiales
    Fusion,
    Ascension,

    // Permanente
    Passive,

    // Turnos de fase
    StartPhase1,
    StartPhase2,

    EndPhase1,
    EndPhase2,

    StartPhase3,
    EndPhase3,

    StartCombatInThisLane,
    EndCombatInThisLane,

    // Sistema de períodos
    StartPeriod,
    EndPeriod,

    StartAlba,
    EndAlba,

    StartOcaso,
    EndOcaso,

    //Inkman
    Inkman,

}
public enum EffectType
{
    None,
    ModifyAttack,
    ModifyHealth,
    ModifyArmor,
    ModifyShield,
    ModifyShieldArmor,
    GainCrystal,
    LoseCrystal,
    GainAbility,
    LoseAbility,
    GainInmunity,
    Heal,
    ModifyDamage,
    DealDamage,
    DrawCard,
    SummonUnit,
    GenerateCard,
    ShuffleCard,
    ModifyCost,
    Move,
    Bounce,
    Stun,
    Transform,
    Destroy,
    Execute,
    DoExtraAttack,

    SetInactive,

    ModifyOverstrike,
    ModifyBurn,
    ModifyHealing,

    SummonEnvironment,

    GrantAbility,

    Revive,

    AttackWithHealth,
    AttackWithArmor,



}
public enum AbilityPassive
{
    None,
    Team, 
    Freeze, 
    Burn, 
    Hunter, 
    Poison, 
    Charm, 
    Lethal,
    AntiSpell, 
    AntiAbility, 
    Break, 
    BleedingAttack, 
    DoubleAttack,
    PiercingAttack,
    AreaDamage,
    LifeSteal, 
    Aquatic, 
    HighGround, 
    AllTerrain, 
    Frenzy, 
    Armored,
    Shielded,
    ArmoredShield,
    Overstrike,
    TripleAttack,


}
public enum TargetType
{
    None,

    
    Self,
    //Portals
    OwnPortal,
    EnemyPortal,

    // Individuales
    SelectedAlly,
    SelectedEnemy,
   

    // Masivos Solo unidades
    AllAlliesUnit,
    AllEnemiesUnit,
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
    RandomAlly,

    // Contextual Targets
    EnemyAttacker,
    DamagedEnemy,
    FusionUnit,

    // Targets invocados, generados o barajados
    SummonedEnemyUnit,
    GeneratedEnemyCard,
    ShuffledEnemyCard,

    SummonedAllyUnit,
    GeneratedAllyCard,
    ShuffledAllyCard,


    // Targets de carril
    EnemyEnteredLane,
    AllyEnteredLane,

    //ExtraMasivos
    AllAlliesCards,
    AllEnemiesCards,

    AllCards,

    //Jugadores
    Player,
    EnemyPlayer,

}
public enum LaneDestination
{
    None,
    OwnLane,
    AdjacentLane,
    RandomLane,
    SelectedLane,
}
public enum TurnPhase
{
    StartTurn,
    TroopTurn,
    SpellAndEnvironmentTurn,
    Combat,
    EndTurn,
}
public enum PeriodType
{
    Alba,
    Ocaso
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
    Bleeding,
    Freezing,
    Burning,
    Poisoned,
    Immunity
}
public enum ComparisonType
{
    None,
    Equal,
    NotEqual,
    Greater,
    GreaterOrEqual,
    Less,
    LessOrEqual
}
public enum StatType
{
    None,
    Attack,
    Health,
    Armor,
    Cost,

    Shield,
    ShieldArmor,
    Burn,
    Overstrike
}
public enum EffectDuration
{
    None,
    Permanent,
    ThisPhase,
    ThisRound,
    UntilPhase3,
    WhileSourceInPlay,
    WhileConditionIsMet,
    WhileInPlay,
    UntilPhase1,
    UntilPhase2,

}
public enum ModifyType
{
    None,
    Increase,
    Decrease,
    Multiply,
    Divide,
    Set
}
public enum ScalingType
{
    None,

    // Cantidad de unidades
    EnemiesInLane,
    AlliesInLane,
    EnemiesInRange,
    AlliesInRange,

    // Estadísticas
    MissingHealth,
    CurrentHealth,
    CurrentAttack,
    CurrentArmor,
    CurrentShield,

    // Combate
    DamageDealt,
    DamageReceived,
    UnitsKilled,

    // Recursos
    CardsInHand,
    CardsInDeck,
    AvailableCrystals,

    Distance
}
public enum CardSelectionType
{
    None,
    Specific,
    Random
}

public enum CardRol
{
    None,
    Fighter,
    Tank,
    Assassin,
    Support,
    Strategist,
    Summoner,
    Harasser,
    Titan
}

public enum TimeOfDay
{
    Alba,
    Ocaso
}