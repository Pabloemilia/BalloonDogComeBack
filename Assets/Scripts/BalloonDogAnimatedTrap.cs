using UnityEngine;

/// <summary>Restarts authored animation when a pooled trap is reused.</summary>
public sealed class BalloonDogAnimatedTrap : MonoBehaviour
{
    private Animator[] animators;
    private float phase;
    public void Configure(Animator[] controllers,float initialPhase)
    { animators=controllers;phase=initialPhase; }
    private void OnEnable()
    {
        if(animators==null) return;
        foreach(var animator in animators)
        {
            if(animator==null || animator.runtimeAnimatorController==null) continue;
            animator.enabled=true;animator.Rebind();animator.Update(0);
            if(animator.layerCount>0) animator.Play(0,0,phase);
        }
    }
    public static Bounds ReferenceBounds(GameObject view,Transform space)
        => GeometryBounds(view.GetComponentsInChildren<MeshRenderer>(true),space);
    private static Bounds GeometryBounds(MeshRenderer[] renderers,Transform mount)
    {
        Bounds result=new Bounds(Vector3.zero,Vector3.one*.01f);bool first=true;
        foreach(var renderer in renderers)
        {
            var filter=renderer.GetComponent<MeshFilter>();
            if(!renderer.enabled || !renderer.gameObject.activeInHierarchy || filter==null || filter.sharedMesh==null) continue;
            Bounds world=filter.sharedMesh.bounds;
            for(int corner=0;corner<8;corner++)
            {
                Vector3 point=world.center+Vector3.Scale(world.extents,new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1));
                point=mount.InverseTransformPoint(renderer.transform.TransformPoint(point));
                if(first) { result=new Bounds(point,Vector3.zero);first=false; } else result.Encapsulate(point);
            }
        }
        return result;
    }
}
