using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(fileName = "CarAmmoData")]
public class CarAmmoScriptableObject : ScriptableObject

{
    [SerializeField] private float MiniBulletDamage;
    [SerializeField] private float MiniBulletMaxAmmo;
    [SerializeField] private float RocketMaxAmmo;
    [SerializeField] private float RocketDamage;


    public float GetMiniBulletdDamage => MiniBulletDamage;
    public float GetMiniBulletMaxAmmo => MiniBulletMaxAmmo;
    public float GetRocketMaxAmmo => RocketMaxAmmo;
    public float GetRocketDamage => RocketDamage;

}
