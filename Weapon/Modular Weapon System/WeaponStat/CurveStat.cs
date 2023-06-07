using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CurveStat : WeaponStat
{
    public AnimationCurve value;
    public AnimationCurve curveUpgrade;
    
    
    public CurveStat(AnimationCurve value)
    {
        weaponStatType = WeaponStatType.Curve;
        this.value = value;
    }
    
    public override object GetValue()
    {
        return value;
    }
    
    public override void SetValue(object value)
    {
        if (value is AnimationCurve curve_value)
        {
            this.value = curve_value;
        }
    }

}
