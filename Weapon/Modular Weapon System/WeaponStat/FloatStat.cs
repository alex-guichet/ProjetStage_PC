using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable]
public class FloatStat : WeaponStat
{
    public float value;
    [HideInInspector] public float calculatedValue;
    [HideInInspector] public float[] statUpgrades;
    [HideInInspector] public bool isPercentage;
    
    
    public FloatStat(float value, bool is_percentage)
    {
        weaponStatType = WeaponStatType.Float;
        this.value = value;
        isPercentage = is_percentage;
    }
    
    public FloatStat()
    {
    }
    
    public override object GetValue()
    {
        return calculatedValue;
    }
    
    public override void SetValue(object value)
    {
        if (value is float float_value)
        {
            calculatedValue = float_value;
        }
    }

}
