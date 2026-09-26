using UnityEngine;

public class CollideAndSlide : MonoBehaviour
{
    [Header("Settings")]
    public float skinWidth = 0.015f;
    public int maxBounces = 5;
    public float maxSlope = 55f;
    [Header("References")]
    public CapsuleCollider col;

    //internal state
    public bool isGrounded {get; private set;}
    public bool isCeiling {get; private set;}

    private void Awake()
    {
        //ensure there is a collider available
        if(!col) col = GetComponent<CapsuleCollider>();
    }

    private Vector3 ColSlide(Vector3 vel, Vector3 pos, int depth, bool gravityPass, Vector3 velInit)
    {
        //cap recursion depth
        if (depth >= maxBounces || vel.sqrMagnitude < 0.000001f){
            return Vector3.zero;
        }
        //define variables for capsule cast
        RaycastHit hit;
        Vector3 p1 = pos + col.center + Vector3.up * (col.height * 0.5f - col.radius);
        Vector3 p2 = pos + col.center - Vector3.up * (col.height * 0.5f - col.radius);
        float castDistance = vel.magnitude + skinWidth / 0.1f;
        if (
            Physics.CapsuleCast(p1, p2, col.radius - skinWidth, vel.normalized, out hit, castDistance)
        )
        {
            //define distance to travel before collision
            float hitDist = hit.distance;
            float cosTheta = Vector3.Dot(-vel.normalized, hit.normal);

            //if moving parallel to or away from the surface, ignore collision to prevent sticking
            if (cosTheta <= 0.001f)
            {
                return vel;
            }

            //clamp normal offset to prevent runaway offsets at shallow grazing angles
            float normalOffset = Mathf.Min(skinWidth / cosTheta, skinWidth * 5f);

            //if surface is further along normal than this move's reach, it's not a collision
            if (hitDist - normalOffset >= vel.magnitude)
            {
                return vel;
            }

            hitDist = Mathf.Max(0f, hitDist - normalOffset);
            Vector3 snapToSurface = vel.normalized * hitDist;            
            //define distance left to travel after collision
            Vector3 leftover = vel - snapToSurface;
            float angle = Vector3.Angle(Vector3.up, hit.normal);

            //detect ceiling collision
            if (hit.normal.y < -0.1f)
            {
                isCeiling = true;
            }

            //flat ground or low slope
            if(angle <= maxSlope)
            {
                isGrounded = true;
                if (gravityPass)
                {
                    return snapToSurface;
                }
                leftover = Vector3.ProjectOnPlane(leftover, hit.normal);
            }
            //wall, steep slope, or ceiling
            else
            {
                if (isGrounded && !gravityPass && Mathf.Abs(hit.normal.y) < 0.9f)
                {
                    Vector3 wallNormalH = new Vector3(hit.normal.x, 0, hit.normal.z).normalized;
                    if (wallNormalH.sqrMagnitude > 0.001f)
                    {
                        leftover = Vector3.ProjectOnPlane(new Vector3(leftover.x, 0, leftover.z), wallNormalH);
                    }
                    else
                    {
                        leftover = Vector3.ProjectOnPlane(leftover, hit.normal);
                    }
                }
                else
                {
                    leftover = Vector3.ProjectOnPlane(leftover, hit.normal);
                }
            }
            //trigger next recursion
            return snapToSurface + ColSlide(leftover, pos + snapToSurface, depth+1, gravityPass, velInit);
        }

        return vel;
    }
    
    //actually transform player position
    public void Move(Vector3 velocity, Vector3 gravity)
    {
        bool wasGrounded = isGrounded;
        isGrounded = false; // reset for movement passes
        isCeiling = false;

        Depenetrate();

        // velocity pass
        Vector3 moveVel = ColSlide(velocity, transform.position, 0, false, velocity);
        transform.position += moveVel;

        // gravity pass
        Vector3 moveGrav = ColSlide(gravity, transform.position, 0, true, gravity);
        transform.position += moveGrav;
    }
    
    //push player out when clipped inside a mesh
    public void Depenetrate()
    {
        Vector3 p1 = transform.position + col.center + Vector3.up * (col.height * 0.5f - col.radius);
        Vector3 p2 = transform.position + col.center - Vector3.up * (col.height * 0.5f - col.radius);
        // React once half the skin is penetrated, before core geometry overlaps
        Collider[] overlaps = Physics.OverlapCapsule(p1, p2, col.radius - skinWidth * 0.5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

        foreach (Collider overlap in overlaps)
        {
            if (overlap == col) continue;

            //Calculate separation vector
            if (Physics.ComputePenetration(
                col, transform.position, transform.rotation,
                overlap, overlap.transform.position, overlap.transform.rotation,
                out Vector3 direction, out float distance))
            {
                // Push the player out by exact penetration distance without overshoot
                transform.position += direction * distance;
            }
        }
    }

}
