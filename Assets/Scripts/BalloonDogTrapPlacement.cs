using System;
using UnityEngine;

/// <summary>Exact authored transforms. Dimensions are never fitted or recentered.</summary>
[Serializable]
public sealed class BalloonDogTrapPlacement
{
    public int id;
    public Vector3 scale = Vector3.one;
    public Vector3 position;
    public float yaw;
    public string alignment = "fixed";
    public int damagePart;
    [Serializable] private sealed class Catalog { public BalloonDogTrapPlacement[] traps; }
    private static Catalog catalog;

    public static BalloonDogTrapPlacement[] All
    {
        get
        {
            if(catalog==null)
            {
                var data=Resources.Load<TextAsset>("Endless/Placement");
                if(data==null) throw new InvalidOperationException("Missing endless placement catalog");
                catalog=JsonUtility.FromJson<Catalog>(data.text);
                if(catalog==null || catalog.traps==null || catalog.traps.Length==0)
                    throw new InvalidOperationException("Empty endless placement catalog");
            }
            return catalog.traps;
        }
    }
    public static BalloonDogTrapPlacement For(int id)
    {
        foreach(var trap in All) if(trap.id==id) return trap;
        throw new InvalidOperationException("Trap is not enabled in the placement catalog: "+id);
    }
    public static string ResourcePath(int id,int variant)
        => "Endless/Trap"+id+(variant==1 ? "" : "V"+variant);

    public bool CanDamage(Transform part,Transform visualRoot)
    {
        if(damagePart==0) return true;
        string expected="O"+id+" Part "+damagePart;
        for(Transform current=part;current!=null;current=current.parent)
        {
            if(current.name==expected) return true;
            if(current==visualRoot) break;
        }
        return false;
    }

    public Vector3 Position(float roadCenter,float floorY,float rowZ,float width,Bounds reference,bool right)
    {
        float x=position.x;
        // Align the outside of the fixed support/resting model to the road edge.
        // Bounds are measured after the exact authored rotation and scale.
        if(alignment=="right" || alignment=="either" && right) x=width*.5f-reference.max.x;
        else if(alignment=="either") x=-width*.5f-reference.min.x;
        return new Vector3(roadCenter+x,floorY+position.y,rowZ+position.z);
    }
}
