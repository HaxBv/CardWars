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
    Aquatic,
    Monster,
    Nature,
    Rodent,
    Flying, 
    Fighter,
    Darkness,
    Visitor,
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
    OnDestroy,
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
    DoExtraDamage,
    DealDamage,
    DrawCard,
    SummonCard,
    GenerateCard,
    ShuffleCard,
    ModifyCost,
    Move,
    Bounce,
    Stun,
    Transform,
    Destroy,
    Execute,
    MultiplyAttack,
    MultiplyHealth,

    DivideAttack,
    DivideHealth,
    DoExtraAttack,

    SetInactive,

    ModifyDirectAttack,
    ModifyBurn,

}
public enum AbilityPassive
{
    None, //0
    Team, //1
    Freeze, //2
    Burn, //3
    Hunter, //4
    Poison, //5
    Charm, //6
    Lethal, //7
    AntiSpell, //8
    AntiAbility, //9
    Break, //10
    BleedingAttack, //11
    DoubleAttack, //12
    PiercingAttack, //13
    AreaDamage, //14
    ThreeLineAttack, //15
    Aquatic, //16
    HighGround, //17
    AllTerrain, //18
    Frenzy, //19
}
public enum TargetType
{
    None,

    // Propios
    Self,
    OwnPortal,
    EnemyPortal,

    // Individuales
    SelectedAlly,
    SelectedEnemy,
   

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
    RandomAlly,

    // Contextual Targets
    EnemyAttacker,
    DamagedEnemy,
    FusionUnit,
    SummonedUnit,
    GeneratedCard,
    ShuffledCard,





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
    Cost
}
public enum EffectDuration
{
    None,
    Permanent,
    ThisTurn,
    ThisRound,
    UntilPhase3,
    WhileSourceInPlay,

    UntilPhase1,
    UnitlPhase2,

}