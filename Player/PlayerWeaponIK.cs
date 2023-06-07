using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerWeaponIK : MonoBehaviour
{
    public Transform weaponDoor;
    public Vector3 doorTargetRotation;
    public Transform arm1;
    public Transform arm2;

    private Vector3 _initialDoorRotation;

    private void Awake()
    {
        _initialDoorRotation = weaponDoor.localRotation.eulerAngles;

        StartCoroutine( Deploy() );
    }

    public IEnumerator Deploy()
    {
        while( true )
        {
            weaponDoor.localRotation = MathHelper.Smooth( weaponDoor.localRotation, Quaternion.Euler( doorTargetRotation ), 25.0f );

            yield return null;
        }
    }

    public IEnumerator Store()
    {
        while( true )
        {
            weaponDoor.localRotation = MathHelper.Smooth( weaponDoor.localRotation, Quaternion.Euler( _initialDoorRotation ), 25.0f );

            yield return null;
        }
    }
}
