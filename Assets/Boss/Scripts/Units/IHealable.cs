/// <summary>Targets the heal tower can heal: the base and towers, never the character.</summary>
public interface IHealable
{
    void Heal(float amount);
    void AddShield(float amount, float cap);
}
