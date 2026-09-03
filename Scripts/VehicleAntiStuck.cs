using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class VehicleAntiStuck : MonoBehaviour
{
    private Rigidbody rb;
    private GameObject Car;
    private CarController CarController;
    private LayerMask LayersToInclude;
    private Vector3 AntiStuckPositonCenter;
    private bool HasPushed = false;
    [SerializeField] private float Threshold;
    [SerializeField] private float RaycastDistance;
    [SerializeField] private float PushAmount;
    [SerializeField] private BoxCollider AntiStuckBoxCastCollider;
    [SerializeField] private float AntiStuckCarTime;
    [SerializeField] private float PushAmountMultiplierInAccelDirection;
    [SerializeField] private float PushAmountMultiplierInAntiStuckDirection;
    private void Awake()
    {
        LayersToInclude = LayerMask.GetMask("Default");
        Car = gameObject;
        rb = Car.GetComponent<Rigidbody>();
        CarController = Car.GetComponent<CarController>();
    }


    public void DetectCollisions()
    {
       
        Collider[] colliders = new Collider[10];
        AntiStuckPositonCenter = transform.TransformPoint(AntiStuckBoxCastCollider.center);
        //if (Physics.Raycast(transform.position, transform.right,out hit, RaycastDistance) || Physics.Raycast(transform.position,-transform.right, out hit,RaycastDistance))
        int hits = Physics.OverlapBoxNonAlloc(AntiStuckPositonCenter, AntiStuckBoxCastCollider.size / 2, colliders, transform.rotation, LayersToInclude, QueryTriggerInteraction.Ignore);
        if (hits > 0)
        {
           
            for (int i = 0; i < hits; i++)
            {
                
                if (colliders[i].transform.root == transform.root)
                {
                    return;
                }
                else
                {
                    
                    if (colliders[i].GetComponent<MeshRenderer>() != null)
                    {
                        MeshRenderer meshRenderer = colliders[i].GetComponent<MeshRenderer>();
                        Vector3 PushDirerction = meshRenderer.bounds.ClosestPoint(transform.forward).normalized;
                        float Angle = Vector3.Angle(transform.forward, PushDirerction);
                        Vector3 FinalPushDirection = PushDirerction.normalized * Mathf.Sin(Angle);

                      
                        StartCoroutine(PushVehicle(FinalPushDirection,Angle));
                    }
                }
                

            }
        }
        else
        {
            return;
        }
    }


    private IEnumerator PushVehicle(Vector3 PushDirection,float Angle)
    {
        yield return new WaitForSeconds(AntiStuckCarTime);
        if (rb != null && CarController != null)
        {
            if (rb.linearVelocity.magnitude <=0.5 && CarController.AccelInput != 0 && !HasPushed)
            {
               
                          
                //Debug.Log(PushDirection);
                rb.AddForce(PushAmount * PushAmountMultiplierInAntiStuckDirection*(PushDirection + Car.transform.up/5 )  + PushAmount* PushAmountMultiplierInAccelDirection * (Car.transform.forward * (CarController.AccelInput + CarController.SteerInput/3)), ForceMode.Acceleration);
                HasPushed = true;
                yield return new WaitForSeconds(2f);
                HasPushed = false;
            }
        }
    }

   
}
