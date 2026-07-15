using UnityEngine;

/// <summary>
/// Loại nhân vật trên prefab: Knight / Ninja / Sorcerer.
/// </summary>
public class CharacterType : MonoBehaviour
{
    public enum Character
    {
        Knight,
        Ninja,
        Sorcerer
    }

    public Character character;
}