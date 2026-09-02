using NUnit.Framework;
using UnityEngine;

public class CollideAndSlide : MonoBehaviour
{
    [Header("Settings")]
    public float skinWidth = 0.015f;
    public int maxBounces = 5;
    public float maxSlope = 55f;
    public float stepHeight = 0.35f;
    public float stepOffset = 0.1f;
    [Header("References")]
    public CapsuleCollider col;

    //internal state
    public bool isGrounded {get; private set;}

    private void Awake()
    {
        //ensure there is a collider available
        if(!col) col = GetComponent<CapsuleCollider>();
    }

    private Vector3 ColSlide(Vector3 vel, Vector3 pos, int depth, bool gravityPass, Vector3 velInit)
    {
        //cap recursion depth
        if (depth >= maxBounces){
            return Vector3.zero;
        }
        //define variables for capsule cast
        RaycastHit hit;
        Vector3 p1 = pos + col.center + Vector3.up * (col.height * 0.5f - col.radius);
        Vector3 p2 = pos + col.center - Vector3.up * (col.height * 0.5f - col.radius);
        if (
            Physics.CapsuleCast(p1, p2, col.radius - skinWidth, vel.normalized, out hit, vel.magnitude + skinWidth)
        )
        {
            //define distance to travel before collision
            float distance = Mathf.Max(0, hit.distance - skinWidth);
            Vector3 snapToSurface = vel.normalized * distance;
            //define distance left to travel after colliosion
            Vector3 leftover = vel - snapToSurface;
            float angle = Vector3.Angle(Vector3.up, hit.normal);

            //flat ground or low slope
            if(angle <= maxSlope)
            {
                isGrounded = true;
                if (gravityPass)
                {
                    return snapToSurface;
                }
                leftover = ProjectAndScale(leftover, hit.normal);
            }
            //wall or steep slope
            else
            {
                float scale = 1 - Vector3.Dot(
                    new Vector3(hit.normal.x, 0, hit.normal.z).normalized,
                    -new Vector3(velInit.x, 0, velInit.z).normalized
                );
                
                if (isGrounded && hit.normal.y < 0)
                {
                    Vector3 ceilingAsWall = new Vector3(hit.normal.x, 0, hit.normal.z).normalized;
                    leftover = ProjectAndScale(
                        new Vector3(leftover.x, 0, leftover.z),
                        ceilingAsWall
                    );
                    leftover *= scale;
                }
                else
                {
                    //stop jittering when against steep walls
                    bool isVertWall = Mathf.Abs(hit.normal.y) < 0.9f;
                    if(isGrounded && !gravityPass && isVertWall)
                    {
                        leftover = ProjectAndScale(
                            new Vector3(leftover.x, 0, leftover.z),
                            new Vector3(hit.normal.x, 0, hit.normal.z)
                        ).normalized;
                        leftover *= scale;
                    }
                    else
                    {
                        leftover = ProjectAndScale(leftover, hit.normal) * scale;
                    }
                }
            }
            //trigger next recursion
            return snapToSurface + ColSlide(leftover, pos + snapToSurface, depth+1, gravityPass, velInit);
        }

        return vel;
    }

    private Vector3 ProjectAndScale(Vector3 left, Vector3 norm)
    {
        float mag = left.magnitude;
        left = Vector3.ProjectOnPlane(left, norm).normalized;
        left *= mag;
        return left;
    }
    
    //actually transform player position
    public void Move(Vector3 velocity, Vector3 gravity)
    {
        bool wasGrounded = isGrounded;
        isGrounded = false; // reset for movement passes

        // step probe
        if (wasGrounded && new Vector3(velocity.x, 0, velocity.z).sqrMagnitude > 0.00001f)
        {
            Vector3 moveDir = new Vector3(velocity.x, 0, velocity.z).normalized;
            float castRad = col.radius - skinWidth;
            float probeDist = 0.25f;

            Vector3 P1(Vector3 pos) => pos + col.center + Vector3.up * (col.height * 0.5f - castRad);
            Vector3 P2(Vector3 pos) => pos + col.center - Vector3.up * (col.height * 0.5f - castRad);
            if (Physics.CapsuleCast(P1(transform.position), P2(transform.position), castRad, moveDir, out RaycastHit blockHit, probeDist))
            {
                if (Vector3.Angle(Vector3.up, blockHit.normal) > maxSlope)
                {
                    Vector3 liftedPos = transform.position;
                    if (Physics.CapsuleCast(P1(transform.position), P2(transform.position), castRad, Vector3.up, out RaycastHit liftHit, stepHeight)) {
                        liftedPos += Vector3.up * Mathf.Max(0, liftHit.distance - skinWidth);
                    } else {
                        liftedPos += Vector3.up * stepHeight;
                    }

                    Vector3 advancedPos = liftedPos;
                    if (Physics.CapsuleCast(P1(liftedPos), P2(liftedPos), castRad, moveDir, out RaycastHit fwdHit, probeDist)) {
                        advancedPos += moveDir * Mathf.Max(0, fwdHit.distance - skinWidth);
                    } else {
                        advancedPos += moveDir * probeDist;
                    }
                    if (Physics.CapsuleCast(P1(advancedPos), P2(advancedPos), castRad, Vector3.down, out RaycastHit dropHit, stepHeight * 2f))
                    {
                        if (Vector3.Angle(Vector3.up, dropHit.normal) <= maxSlope)
                        {
                            //Calculate step height
                            float heightDiff = (advancedPos.y - dropHit.distance) - transform.position.y;

                            if (heightDiff > 0.01f && heightDiff <= stepHeight + 0.05f)
                            {
                                transform.position += Vector3.up * (heightDiff + 0.02f);
                            }
                        }
                    }
                }
            }
        }

        // velocity pass
        Vector3 moveVel = ColSlide(velocity, transform.position, 0, false, velocity);
        transform.position += moveVel;

        // gravity pass
        Vector3 moveGrav = ColSlide(gravity, transform.position, 0, true, gravity);
        transform.position += moveGrav;
    }
}
