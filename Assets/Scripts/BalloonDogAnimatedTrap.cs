using UnityEngine;

/// <summary>Keeps authored animation and its moving hitboxes inside one fitted lane.</summary>
public sealed class BalloonDogAnimatedTrap : MonoBehaviour
{
    private Animator[] animators;
    private float phase;
    private Transform staticVisual;
    private Quaternion staticRotation;
    public void Configure(GameObject view,Animator[] controllers,float initialPhase)
    {
        animators=controllers;phase=initialPhase;
        // The spike-only model has no authored Animator. Its gentle turning
        // motion stays inside the square horizontal envelope measured below.
        if(controllers.Length==0 && view.GetComponentsInChildren<MeshFilter>().Length>0
            && view.GetComponentsInChildren<UnityEngine.VFX.VisualEffect>(true).Length==0)
        { staticVisual=view.transform;staticRotation=staticVisual.localRotation; }
    }
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
    private void Update()
    {
        if(staticVisual!=null)
            staticVisual.localRotation=staticRotation*Quaternion.Euler(0,Mathf.Sin(Time.time*1.4f+phase*6.28f)*12f,0);
    }
    public static Vector3 FitScale(Bounds bounds)
    {
        float scale=Mathf.Min(1.7f/Mathf.Max(.01f,bounds.size.x),
            5.2f/Mathf.Max(.01f,bounds.size.z),3.2f/Mathf.Max(.01f,bounds.size.y));
        return Vector3.one*scale;
    }
    public static Bounds ReferenceBounds(GameObject view,Transform space)
        => GeometryBounds(view.GetComponentsInChildren<MeshRenderer>(true),space);
    public static Bounds MeasureAnimation(GameObject view,Transform mount,Animator[] animators)
        => MeasureAnimation(view,mount,animators,out _);
    public static Bounds MeasureAnimation(GameObject view,Transform mount,Animator[] animators,out Bounds support)
    {
        MeshRenderer[] renderers=view.GetComponentsInChildren<MeshRenderer>(true);
        var initial=new Matrix4x4[renderers.Length];
        var stationary=new bool[renderers.Length];
        for(int i=0;i<renderers.Length;i++)
        { initial[i]=mount.worldToLocalMatrix*renderers[i].transform.localToWorldMatrix;stationary[i]=true; }
        Bounds result=GeometryBounds(renderers,mount);
        Bounds reference=result;
        foreach(var animator in animators)
        {
            if(animator.runtimeAnimatorController==null) continue;
            animator.enabled=false;
            foreach(var clip in animator.runtimeAnimatorController.animationClips)
            {
                if(clip==null) continue;
                // Sample a complete cycle, including both extrema and the seam.
                // A padded envelope absorbs interpolation between samples.
                for(int frame=0;frame<=48;frame++)
                {
                    clip.SampleAnimation(animator.gameObject,clip.length*frame/48f);
                    result.Encapsulate(GeometryBounds(renderers,mount));
                    for(int i=0;i<renderers.Length;i++)
                    {
                        Matrix4x4 current=mount.worldToLocalMatrix*renderers[i].transform.localToWorldMatrix;
                        for(int element=0;element<16;element++)
                            if(Mathf.Abs(current[element]-initial[i][element])>.0001f) stationary[i]=false;
                    }
                }
            }
            animator.enabled=true;animator.Rebind();animator.Update(0);
        }
        // A stationary support determines the floor even when a blade swings
        // below it. Models without a fixed support use their reference pose.
        support=reference;bool foundSupport=false;
        for(int i=0;i<renderers.Length;i++)
        {
            if(!stationary[i] || !renderers[i].enabled || !renderers[i].gameObject.activeInHierarchy) continue;
            Bounds part=GeometryBounds(new[]{renderers[i]},mount);
            if(!foundSupport) { support=part;foundSupport=true; } else support.Encapsulate(part);
        }
        if(animators.Length==0 && view.GetComponentsInChildren<UnityEngine.VFX.VisualEffect>(true).Length==0)
        {
            // Static spikes may turn by twelve degrees; include their swept corners.
            float radius=new Vector2(result.extents.x,result.extents.z).magnitude;
            result.SetMinMax(new Vector3(-radius,result.min.y,-radius),new Vector3(radius,result.max.y,radius));
        }
        result.Expand(new Vector3(result.size.x*.08f,result.size.y*.02f,result.size.z*.08f));
        return result;
    }
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
