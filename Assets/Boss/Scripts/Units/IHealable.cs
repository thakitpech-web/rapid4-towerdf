/// Targets the heal tower can heal: the base and towers, never the character.
public interface IHealable
{
    void Heal(float amount);
    void AddShield(float amount, float cap);
}
