using PirateLegends.Player;
using PirateLegends.World;
using UnityEngine;

namespace PirateLegends.Ships
{
    [DefaultExecutionOrder(-150)]
    public sealed class PirateShipHelm : MonoBehaviour
    {
        [SerializeField] private PlayerLocomotion player;
        [SerializeField] private Transform wheel;
        [SerializeField] private Terrain island;
        [SerializeField] private OceanWeather weather;
        [SerializeField] private Transform dockBoardingPoint;
        [SerializeField] private Transform deckBoardingPoint;
        [SerializeField] private Transform cabinDoorLeft;
        [SerializeField] private Transform cabinDoorRight;
        private const float ReferenceHullScale = .65f;
        private Vector3 berth;
        private float hullScale;
        private Quaternion heading, leftDoorRest, rightDoorRest;
        private float pitch, roll, doorProgress;
        private bool doorsOpen;
        [SerializeField] private float forwardSpeed = 7f;
        [SerializeField] private float reverseSpeed = 3f;
        [SerializeField] private float turnSpeed = 24f;
        private PlayerInputReader input;
        private Matrix4x4 previousWorldToLocal;
        private Quaternion wheelRest;
        private Vector3 wheelAxis;
        private float throttle, speed, wheelAngle;

        public bool IsSteering { get; private set; }
        public bool CanTakeHelm => player.RidingShip == this
            && (wheel.position - (player.transform.position + Vector3.up)).sqrMagnitude < 4f;
        public float Speed => speed;
        private bool IsAlongside => dockBoardingPoint && deckBoardingPoint && Mathf.Abs(speed) < .6f
            && Vector3.ProjectOnPlane(deckBoardingPoint.position - dockBoardingPoint.position, Vector3.up).sqrMagnitude < 8.5f * 8.5f;
        public bool CanBoard => IsAlongside && player.Grounded && !player.RidingShip
            && (player.transform.position - dockBoardingPoint.position).sqrMagnitude < 2.25f * 2.25f;
        public bool CanDisembark => IsAlongside && player.Grounded && player.RidingShip == this
            && (player.transform.position - deckBoardingPoint.position).sqrMagnitude < 2f * 2f;
        public bool CanUseCabinDoors => cabinDoorLeft && cabinDoorRight && player.RidingShip == this
            && ((cabinDoorLeft.position + cabinDoorRight.position) * .5f
                - (player.transform.position + Vector3.up)).sqrMagnitude < 1.9f * 1.9f;
        public bool CabinDoorsOpen => doorsOpen;
        public Quaternion RotationDelta { get; private set; } = Quaternion.identity;

        private void Awake()
        {
            berth = transform.position;
            hullScale = transform.lossyScale.x / ReferenceHullScale;
            heading = transform.rotation;
            if (cabinDoorLeft) leftDoorRest = cabinDoorLeft.localRotation;
            if (cabinDoorRight) rightDoorRest = cabinDoorRight.localRotation;
            input = player.GetComponent<PlayerInputReader>();
            previousWorldToLocal = transform.worldToLocalMatrix;
            wheelRest = wheel.localRotation;
            wheelAxis = wheel.InverseTransformDirection(transform.forward);
        }

        public Vector3 CarryDelta(Vector3 worldPoint)
        {
            return transform.TransformPoint(previousWorldToLocal.MultiplyPoint3x4(worldPoint)) - worldPoint;
        }

private void Update()
        {
            previousWorldToLocal = transform.worldToLocalMatrix;
            RotationDelta = Quaternion.identity;
            if (IsSteering && player.RidingShip != this) ReleaseHelm();

            bool canUseHelm = IsSteering || CanTakeHelm;
            bool canUseDoors = CanUseCabinDoors;
            bool boarding = CanBoard;
            bool disembarking = CanDisembark;
            Transform transferPoint = null;
            if (!player.MovementLocked && (canUseHelm || canUseDoors || boarding || disembarking) && input.ConsumeInteract())
            {
                if (canUseHelm)
                {
                    if (IsSteering) ReleaseHelm();
                    else
                    {
                        IsSteering = true;
                        player.SteeringLocked = true;
                        input.ConsumeJump(float.PositiveInfinity);
                    }
                }
                else if (canUseDoors) doorsOpen = !doorsOpen;
                else transferPoint = boarding ? deckBoardingPoint : dockBoardingPoint;
            }

            float dt = Time.deltaTime;
            float steering = IsSteering ? input.Move.x : 0f;
            if (IsSteering)
            {
                if (input.ConsumeJump(.15f)) throttle = 0f;
                else if (Mathf.Abs(input.Move.y) > .1f)
                    throttle = Mathf.MoveTowards(throttle, Mathf.Sign(input.Move.y), dt * Mathf.Abs(input.Move.y));
            }

            float targetSpeed = throttle * (throttle < 0f ? reverseSpeed : forwardSpeed);
            speed = Mathf.MoveTowards(speed, targetSpeed, dt * 3f);
            Quaternion oldHeading = heading;
            float turn = steering * turnSpeed * Mathf.Clamp01(Mathf.Abs(speed) / 2f);
            heading = Quaternion.AngleAxis(turn * Mathf.Sign(speed) * dt, Vector3.up) * oldHeading;
            Vector3 position = transform.position + heading * Vector3.forward * (speed * dt);
            if (Mathf.Abs(speed) > .001f && !HasClearWater(position, heading))
            {
                speed = throttle = 0f;
                position = transform.position;
                heading = oldHeading;
            }
            if (weather) ApplySwell(ref position, dt);

            Quaternion rotation = heading * Quaternion.Euler(pitch, 0f, roll);
            transform.SetPositionAndRotation(position, rotation);
            // Carry the passenger with the whole hull, but keep the character upright.
            RotationDelta = heading * Quaternion.Inverse(oldHeading);
            AnimateCabinDoors(dt);
            Physics.SyncTransforms();
            if (transferPoint) player.TransferTo(transferPoint.position);

            wheelAngle = Mathf.MoveTowards(wheelAngle, steering * 70f, 230f * dt);
            wheel.localRotation = wheelRest * Quaternion.AngleAxis(wheelAngle, wheelAxis);
        }

        private void ApplySwell(ref Vector3 position, float dt)
        {
            float halfLength = 8f * hullScale;
            float halfBeam = 3f * hullScale;
            Vector3 bowOffset = heading * Vector3.forward * halfLength;
            Vector3 sideOffset = heading * Vector3.right * halfBeam;
            float bow = weather.HeightAt(position + bowOffset);
            float stern = weather.HeightAt(position - bowOffset);
            float starboard = weather.HeightAt(position + sideOffset);
            float port = weather.HeightAt(position - sideOffset);
            float surface = (bow + stern + starboard + port) * .25f;
            float response = 1f - Mathf.Exp(-3f * dt);
            float pitchTarget = Mathf.Atan2(stern - bow, 2f * halfLength) * Mathf.Rad2Deg * 1.2f;
            float rollTarget = Mathf.Atan2(starboard - port, 2f * halfBeam) * Mathf.Rad2Deg * 1.5f;
            pitch = Mathf.Lerp(pitch, Mathf.Clamp(pitchTarget, -7f, 7f), response);
            roll = Mathf.Lerp(roll, Mathf.Clamp(rollTarget, -10f, 10f), response);
            position.y = Mathf.Lerp(position.y, berth.y + surface, response);
        }


        private bool HasClearWater(Vector3 position, Quaternion rotation)
        {
            if (!island) return true;
            // Sample the keel and both sides along the length of the hull.
            for (int section = -2; section <= 2; section++)
            for (int side = -1; side <= 1; side++)
            {
                Vector3 point = position + rotation * (new Vector3(side * 3.2f, 0f, 1.2f + section * 6.5f) * hullScale);
                if (island.SampleHeight(point) + island.transform.position.y > (weather ? weather.SeaLevel : 0f) - 2.6f) return false;
            }
            return true;
        }

        private void ReleaseHelm()
        {
            IsSteering = false;
            if (player) player.SteeringLocked = false;
        }

        private void OnDisable() => ReleaseHelm();


        private void AnimateCabinDoors(float dt)
        {
            if (!cabinDoorLeft || !cabinDoorRight) return;
            if (!doorsOpen && doorProgress > 0f && IsCabinDoorwayOccupied()) return;
            doorProgress = Mathf.MoveTowards(doorProgress, doorsOpen ? 1f : 0f, dt / .55f);
            float angle = Mathf.SmoothStep(0f, 95f, doorProgress);
            cabinDoorLeft.localRotation = leftDoorRest * Quaternion.AngleAxis(angle, Vector3.up);
            cabinDoorRight.localRotation = rightDoorRest * Quaternion.AngleAxis(-angle, Vector3.up);
        }

        private bool IsCabinDoorwayOccupied()
        {
            Vector3 center = (cabinDoorLeft.position + cabinDoorRight.position) * .5f;
            Vector3 offset = player.transform.position + Vector3.up - center;
            float across = Vector3.Dot(offset, transform.right);
            float along = Vector3.Dot(offset, transform.forward);
            float height = Vector3.Dot(offset, transform.up);
            return Mathf.Abs(across) < 1.45f && along > -1.45f && along < .42f
                && Mathf.Abs(height) < 1.9f;
        }
    }
}
