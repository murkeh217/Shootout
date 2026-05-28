using UnityEngine;

[CreateAssetMenu(fileName = "Perk", menuName = "Roguelike/Perk")]
public class PerkDefinition : ScriptableObject
{
    [Header("Identity")]
    public string id;
    public string displayName;
    [TextArea] public string description;

    [Header("Modifiers")]
    [Tooltip("Multiplicative. 1.10 = +10% damage.")]
    public float damageMultiplier = 1f;

    [Tooltip("Multiplicative. 1.10 = +10% fire rate.")]
    public float fireRateMultiplier = 1f;

    [Tooltip("Multiplicative. 1.10 = +10% move speed.")]
    public float moveSpeedMultiplier = 1f;

    [Tooltip("Additive. +10 = +10 max HP.")]
    public int maxHpBonus = 0;

    [Tooltip("Additive. 0.05 = +5% crit chance.")]
    public float critChanceBonus = 0f;
}

