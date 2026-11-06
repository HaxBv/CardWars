
public readonly struct DamageResult
{
    public int HealthDamage { get; }
    public int ShieldDamage { get; }
    public int BlockedDamage { get; }
    public bool ShieldDestroyed { get; }

    public bool DamagedHealth => HealthDamage > 0;

    public DamageResult(
        int healthDamage,
        int shieldDamage,
        int blockedDamage,
        bool shieldDestroyed)
    {
        HealthDamage = healthDamage;
        ShieldDamage = shieldDamage;
        BlockedDamage = blockedDamage;
        ShieldDestroyed = shieldDestroyed;
    }
}
