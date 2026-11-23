/// Passive upgrade kinds. The numbers are saved in the card assets, so keep the order and add new ones at the end.
public enum UpgradeType
{
    EnemySlow,
    Damage,         // character, towers (fire zone too) and the base
    BaseMaxHP,
    MoveSpeed,      // character move speed
    PickupRadius,
    AttackSpeed,    // character attacks per second
    TowerRange      // Bolt, AoE and Heal range (not the AoE blast radius or the base)
}
