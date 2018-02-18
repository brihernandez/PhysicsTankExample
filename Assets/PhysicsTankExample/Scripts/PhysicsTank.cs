using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class PhysicsTank : MonoBehaviour
{
   [Tooltip("Top speed of the tank in m/s.")]
   public float topSpeed = 10.0f;
   [Tooltip("For tanks with front/rear wheels defined, this is how far those wheels turn.")]
   public float steeringAngle = 30.0f;
   [Tooltip("Power of any wheel listed under powered wheels.")]
   public float motorTorque = 10.0f;
   [Tooltip("Turn rate that is \"magically\" applied regardless of what the physics state of the tank is.")]
   public float magicTurnRate = 45.0f;

   [Tooltip("Assign this to override the center of mass. This can be useful to make the tank more stable and prevent it from flipping over. \n\nNOTE: THIS TRANSFORM MUST BE A CHILD OF THE ROOT TANK OBJECT.")]
   public Transform centerOfMass;

   [Tooltip("This prefab will be instantiated as a child of each wheel object and mimic the position/rotation of that wheel. If the prefab has a diameter of 1m, it will scale correct to match the wheel radius.")]
   public Transform wheelModelPrefab;

   [Tooltip("Front wheels used for steering by rotating the wheels left/right.")]
   public WheelCollider[] front;
   [Tooltip("Rear wheels for steering by rotating the wheels left/right.")]
   public WheelCollider[] rear;
   [Tooltip("Wheels that provide power and move the tank forwards/reverse.")]
   public WheelCollider[] poweredWheels;

   [Tooltip("Wheels on the left side of the tank that are used for differential steering.")]
   public WheelCollider[] left;
   [Tooltip("Wheels on the right side of the tank that are used for differential steering.")]
   public WheelCollider[] right;

   private Rigidbody rigid;
   private float forwardInput, turnInput = 0.0f;

   // Used to associate a wheel with a one of the model prefabs.
   private Dictionary<WheelCollider, Transform> WheelToTransformMap;

   private void Awake()
   {
      rigid = GetComponent<Rigidbody>();
      WheelToTransformMap = new Dictionary<WheelCollider, Transform>(poweredWheels.Length);
   }

   private void Start()
   {
      // Override center of mass when a reference is passed in.
      if (centerOfMass != null)
      {
         if (centerOfMass.parent == transform)
            rigid.centerOfMass = centerOfMass.localPosition;
         else
            Debug.LogWarning(name + ": PhysicsTank cannot override center of mass when " + centerOfMass.name + " is not a child of " + transform.name);
         
      }

      if (wheelModelPrefab != null)
      {
         InstantiateWheelModelsFromPrefab(front);
         InstantiateWheelModelsFromPrefab(rear);
         InstantiateWheelModelsFromPrefab(poweredWheels);
         InstantiateWheelModelsFromPrefab(left);
         InstantiateWheelModelsFromPrefab(right);
      }
   }

   private void Update()
   {
      // Capture input in the Update, not the FixedUpdate!
      forwardInput = Input.GetAxis("Vertical");
      turnInput = Input.GetAxis("Horizontal");
   }

   private void FixedUpdate()
   {
      RunPoweredWheels();
      RunDifferentialSteeringWheels();
      RunFourWheelSteeringWheels();
      RunMagicRotation();
   }

   /// <summary>
   /// POWERED WHEELS
   /// Sets the motor torque of the wheel based on forward input. This moves
   /// the tank forwards and backwards. 
   /// </summary>
   private void RunPoweredWheels()
   {
      foreach (WheelCollider wheel in poweredWheels)
      {
         // To create a top speed for the tank, the motor torque just
         // cuts out when the tank starts moving fast enough.
         if (rigid.velocity.magnitude <= topSpeed)
            wheel.motorTorque = forwardInput * motorTorque;
         else
            wheel.motorTorque = 0.0f;

         // Update wheel mesh positions to match the physics wheels.
         if (wheelModelPrefab != null && WheelToTransformMap.ContainsKey(wheel))
         {
            Vector3 position;
            Quaternion rotation;
            wheel.GetWorldPose(out position, out rotation);
            WheelToTransformMap[wheel].position = position;
            WheelToTransformMap[wheel].rotation = rotation;
         }
      }
   }

   /// <summary>
   /// DIFFERENTIAL STEERING
   /// When turning, the left/right wheel colliders will apply an extra
   /// torque in opposing directions and rotate the tank.
   /// 
   /// Note: Wheel sideways friction can easily prevent the tank from
   /// rotating when this is done. Lowering side friction for wheels that
   /// don't need it (i.e., wheels away from the center) can mitigate this.
   /// </summary>
   private void RunDifferentialSteeringWheels()
   {
      foreach (WheelCollider wheel in left)
         wheel.motorTorque += motorTorque * turnInput;
      foreach (WheelCollider wheel in right)
         wheel.motorTorque -= motorTorque * turnInput;
   }

   /// <summary>
   /// FOUR WHEEL STEERING
   /// Wheels assigned as front and rear wheels rotate to turn the tank.
   /// This works great in motion, but will not turn the tank when standing
   /// still. 
   /// 
   /// Note: If only one set of wheels is filled out, only that set will
   /// rotate.
   /// </summary>
   private void RunFourWheelSteeringWheels()
   {
      foreach (WheelCollider wheel in front)
         wheel.steerAngle = turnInput * steeringAngle;
      foreach (WheelCollider wheel in rear)
         wheel.steerAngle = -turnInput * steeringAngle;
   }

   /// <summary>
   /// MAGIC ROTATION
   /// Simply rotates the Rigidbody itself using a predefined rotation rate
   /// and turning input. This has no connection to physics in any way, but
   /// is very controllable and predictable.
   /// 
   /// Note: Since there is no connection to the physics, the tank could
   /// turn even if it wasn't on the ground. A simple way to counter this
   /// would be to check how many wheels are on the ground and then reduce
   /// the turning speed depending on how many are touching the ground.
   /// </summary>
   private void RunMagicRotation()
   {
      Quaternion magicRotation = transform.rotation * Quaternion.AngleAxis(magicTurnRate * turnInput * Time.deltaTime, transform.up);
      rigid.MoveRotation(magicRotation);
   }

   /// <summary>
   /// Instantiates wheel model prefabs on each of the wheels and moves
   /// them to match the physics wheels.
   /// </summary>
   /// <param name="wheels"></param>
   private void InstantiateWheelModelsFromPrefab(WheelCollider[] wheels)
   {
      foreach (WheelCollider wheel in wheels)
      {
         // Don't double instantiate wheels. Check to make sure that this wheel doesn't already
         // have a model before instantiating one.
         if (WheelToTransformMap.ContainsKey(wheel) == false)
         {
            Transform temp = Instantiate(wheelModelPrefab, wheel.transform, false);
            
            // Scale the model prefab to match the radius. (Assumes prefab has diameter of 1m.)
            temp.localScale = Vector3.one * wheel.radius * 2.0f;
            WheelToTransformMap.Add(wheel, temp);
         }
      }
   }
}
