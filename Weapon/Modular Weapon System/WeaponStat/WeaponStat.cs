using UnityEngine;

public enum WeaponStatType
{
    Float,
    Curve
}

[System.Serializable]
public class WeaponStat
{
    [HideInInspector] public WeaponStatType weaponStatType;
    
    public virtual object GetValue()
    {
        return null;
    }
    
    public virtual void SetValue(object value)
    {
    }
}
 