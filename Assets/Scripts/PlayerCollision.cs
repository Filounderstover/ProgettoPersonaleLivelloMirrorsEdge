using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCollision : MonoBehaviour
{
    public float FloorCheckRadius; //detection size of the floor
    public float bottomOffset; //offset from player centre
    public float WallCheckRadius; //detection size of the walls
    public float frontOffset; //offset from the player centre
    public float RoofCheckRadius; //the amount we check before standing up
    public float upOffset; //offset upwards

    public float LedgeGrabForwardPos; //the position in front of the player where we check for ledges
    public float LedgeGrabUpwardsPos; //the position in above of the player where we check for ledges
    public float LedgeGrabDistance; //the distance the ledge can be from our raycast before we grab it


    public LayerMask FloorLayers; //what layers we can stand on
    public LayerMask WallLayers; //what layers we can wallrun on
    public LayerMask RoofLayers; //what layers we cannot stand up under (for crouching)
    public LayerMask LedgeGrabLayers; //what layers we will grab onto

    public bool CheckFloor(Vector3 Dir)
    {
        Vector3 pos = transform.position + (Dir * bottomOffset);
        Collider[] ColHit = Physics.OverlapSphere(pos, FloorCheckRadius, FloorLayers);
        if(ColHit.Length > 0)
        {
            //there is ground below us
            return true;
        }

        return false;
    }

    //stessa funziona ma solo per Wall layers
    public bool CheckWalls(Vector3 Dir)
    {
        Vector3 pos = transform.position + (Dir * frontOffset);
        Collider[] ColHit = Physics.OverlapSphere(pos, WallCheckRadius, WallLayers);
        if (ColHit.Length > 0)
        {
            //there is ground below us
            return true;
        }

        return false;
    }

    public bool CheckRoof(Vector3 Dir)
    {
        Vector3 pos = transform.position + (Dir * upOffset);
        Collider[] ColHit = Physics.OverlapSphere(pos, RoofCheckRadius, RoofLayers);
        if (ColHit.Length > 0)
        {
            //there is ground below us
            return true;
        }

        return false;
    }

    public Vector3 CheckLedges()
    {
        Vector3 RayPos = transform.position + (transform.forward * LedgeGrabForwardPos) + (transform.up * LedgeGrabUpwardsPos);

        RaycastHit Hit;
        if (Physics.Raycast(RayPos, -transform.up, out Hit, LedgeGrabDistance, LedgeGrabLayers))
        {
            return Hit.point;
        }

        return Vector3.zero;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 pos = transform.position + (-transform.up * bottomOffset);
        Gizmos.DrawSphere(pos, FloorCheckRadius);

        Gizmos.color = Color.red;
        pos = transform.position + (transform.forward * frontOffset);
        Gizmos.DrawSphere(pos, WallCheckRadius);

        Gizmos.color = Color.cyan;

        pos = transform.position + (transform.forward * LedgeGrabForwardPos) + (transform.up * LedgeGrabUpwardsPos);
        Gizmos.DrawLine(pos, pos + (transform.up));

    }
}
