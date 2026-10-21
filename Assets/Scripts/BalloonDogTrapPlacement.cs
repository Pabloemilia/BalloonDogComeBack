using System;
using UnityEngine;

/// <summary>Per-model placement data, outside every authored animation transform.</summary>
[Serializable]
public sealed class BalloonDogTrapPlacement
{
    public int id;
    public float yaw;
    public float targetWidth = 1.65f;
    public float targetHeight = 2.2f;
    public float groundOffset;
    [Serializable] private sealed class Catalog { public BalloonDogTrapPlacement[] traps; }
    private static Catalog catalog;

    public static BalloonDogTrapPlacement For(int id)
    {
        if(catalog==null)
        {
            var data=Resources.Load<TextAsset>("Endless/Placement");
            if(data==null) throw new InvalidOperationException("Missing endless placement catalog");
            catalog=JsonUtility.FromJson<Catalog>(data.text);
        }
        foreach(var trap in catalog.traps) if(trap.id==id) return trap;
        throw new InvalidOperationException("Missing placement for trap "+id);
    }

    public float Scale(Bounds reference,Bounds swept,float laneWidth)
    {
        // One uniform factor preserves the model and all authored motion ratios.
        // Both the visual target and the complete moving footprint must fit.
        float available=Mathf.Max(.1f,laneWidth-.35f);
        return Mathf.Min(Mathf.Min(targetWidth,available)/Mathf.Max(.01f,reference.size.x),
            targetHeight/Mathf.Max(.01f,reference.size.y),
            available/Mathf.Max(.01f,swept.size.x),
            5.2f/Mathf.Max(.01f,swept.size.z));
    }
}
