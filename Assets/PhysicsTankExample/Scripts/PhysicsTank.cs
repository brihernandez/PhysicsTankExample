using UnityEngine;
using System.Collections.Generic;

public class PhysicsTank : MonoBehaviour
{
   public float topSpeed = 10.0f;
   public float steeringAngle = 30.0f;
   public float motorTorque = 10.0f;
   public float spinTorque = 100.0f;

   public Transform centerOfMass;
   public Transform wheelModelPrefab;

   [Tooltip("Front wheels for steering.")]
   public WheelCollider[] front;
   [Tooltip("Rear wheels for steering.")]
   public WheelCollider[] rear;
   [Tooltip("Any wheels that provide power.")]
   public WheelCollider[] poweredwheels;

   public WheelCollider[] left;
   public WheelCollider[] right;

   private Rigidbody rigid;
   private float forwardInput, turnInput = 0.0f;

   private Dictionary<WheelCollider, Transform> WheelToTransformMap;

   private void Awake()
   {
      rigid = GetComponent<Rigidbody>();
      WheelToTransformMap = new Dictionary<WheelCollider, Transform>(poweredwheels.Length);
   }

   private void Start()
   {
      if (centerOfMass != null)
      {
         rigid.centerOfMass = centerOfMass.localPosition;
      }

      if (wheelModelPrefab != null)
      {
         foreach (WheelCollider wheel in poweredwheels)
         {
            Transform temp = Instantiate(wheelModelPrefab, wheel.transform, false);
            temp.localScale = Vector3.one * wheel.radius * 2.0f;
            WheelToTransformMap.Add(wheel, temp);
         }
      }
   }

   private void Update()
   {
      forwardInput = Input.GetAxis("Vertical");
      turnInput = Input.GetAxis("Horizontal");
   }

   private void FixedUpdate()
   {
      rigid.AddRelativeTorque(Vector3.up * turnInput * spinTorque);

      foreach (WheelCollider wheel in poweredwheels)
      {
         if (rigid.velocity.magnitude < topSpeed)
         {
            wheel.motorTorque = forwardInput * motorTorque;
         }
         else
         {
            wheel.motorTorque = 0.0f;
         }

         // Update wheel meshes
         if (WheelToTransformMap.ContainsKey(wheel))
         {
            Vector3 position;
            Quaternion rotation;

            wheel.GetWorldPose(out position, out rotation);
            WheelToTransformMap[wheel].position = position;
            WheelToTransformMap[wheel].rotation = rotation;
         }
      }

      foreach (WheelCollider wheel in left)
      {
         wheel.motorTorque -= motorTorque * turnInput;
      }

      foreach (WheelCollider wheel in right)
      {
         wheel.motorTorque += motorTorque * turnInput;
      }

      foreach (WheelCollider wheel in front)
      {
         wheel.steerAngle = turnInput * steeringAngle;
      }
      foreach (WheelCollider wheel in rear)
      {
         wheel.steerAngle = -turnInput * steeringAngle;
      }
   }


}
