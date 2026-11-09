using UnityEngine;

/// <summary>
/// One difficulty preset (GAME_DESIGN.md section 7, "Difficulty presets").
/// Make three assets: Design doc, Normal (default), Hard.
/// </summary>
[CreateAssetMenu(fileName = "DifficultyPreset", menuName = "One Minute Siege/Difficulty Preset")]
public class DifficultyPreset : ScriptableObject
{
    public string DisplayName = "Normal";

    [Tooltip("Monster HP multiplier at loop 1")]
    public float HpStart = 1.2f;

    [Tooltip("Monster HP multiplier per loop, compounding")]
    public float HpGrowth = 1.5f;

    [Tooltip("Monster damage multiplier")]
    public float DamageMultiplier = 1.4f;

    [Tooltip("Monster damage multiplier per loop, compounding (1.15 in every preset)")]
    public float DamageGrowth = 1.15f;

    public float BaseHP = 400f;
    public float BossHP = 8000f;
}
