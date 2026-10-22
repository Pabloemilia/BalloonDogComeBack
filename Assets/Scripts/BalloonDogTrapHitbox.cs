using System.Collections.Generic;
using UnityEngine;

/// <summary>A box only finds candidates; visible mesh surfaces confirm damage.</summary>
[DefaultExecutionOrder(200)]
public sealed class BalloonDogTrapHitbox : MonoBehaviour
{
    private sealed class Geometry
    {
        public Vector3[] vertices;
        public int[] triangles;
    }
    private static readonly Dictionary<Mesh,Geometry> cache=new Dictionary<Mesh,Geometry>();
    private MeshRenderer visual;
    private Transform meshTransform;
    private BalloonDogEndlessContact owner;
    private BoxCollider broadphase;
    private Geometry geometry;
    private Vector3[] worldVertices;

    public void Configure(MeshFilter mesh,BalloonDogEndlessContact contact)
    {
        owner=contact;meshTransform=mesh.transform;visual=mesh.GetComponent<MeshRenderer>();
        broadphase=gameObject.AddComponent<BoxCollider>();broadphase.isTrigger=true;
        Mesh source=mesh.sharedMesh;
        if(source!=null && source.isReadable)
        {
            if(!cache.TryGetValue(source,out geometry))
            {
                geometry=new Geometry { vertices=source.vertices,triangles=source.triangles };
                cache.Add(source,geometry);
            }
            worldVertices=new Vector3[geometry.vertices.Length];
        }
        else Debug.LogWarning("Disabled trap contact without readable visible geometry: "+mesh.name,mesh);
        Refresh();
    }
    public void Refresh()
    {
        if(broadphase==null) return;
        broadphase.enabled=geometry!=null && visual!=null && visual.enabled && !visual.forceRenderingOff
            && visual.gameObject.activeInHierarchy && visual.sharedMaterial!=null;
        if(!broadphase.enabled) return;
        Bounds bounds=visual.bounds;
        // The proxy is under the unscaled contact owner, so rotating parts under
        // nonuniform scales cannot shear or leave behind a phantom trigger.
        transform.position=bounds.center;
        broadphase.center=Vector3.zero;
        broadphase.size=new Vector3(Mathf.Max(.005f,bounds.size.x),Mathf.Max(.005f,bounds.size.y),Mathf.Max(.005f,bounds.size.z));
    }
    private void FixedUpdate() => Refresh();
    private void LateUpdate() => Refresh();
    private void OnEnable() => Refresh();
    private void OnTriggerEnter(Collider other) => CheckContact(other);
    private void OnTriggerStay(Collider other) => CheckContact(other);
    private void CheckContact(Collider other)
    {
        if(owner==null || other==null || other.isTrigger || other.GetComponentInParent<AirController>()==null) return;
        if(TouchesVisibleGeometry(other)) owner.ApplyVisibleHit(other);
    }
    public bool TouchesVisibleGeometry(Collider other)
    {
        if(geometry==null || visual==null || !visual.enabled || visual.forceRenderingOff
            || !visual.gameObject.activeInHierarchy || visual.sharedMaterial==null || other==null || !other.enabled) return false;
        if(!visual.bounds.Intersects(other.bounds)) return false;
        Vector3 start,end;float radius;
        if(other is CapsuleCollider capsule)
        {
            Vector3 scale=capsule.transform.lossyScale;
            scale=new Vector3(Mathf.Abs(scale.x),Mathf.Abs(scale.y),Mathf.Abs(scale.z));
            Vector3 axis=capsule.direction==0 ? Vector3.right : capsule.direction==1 ? Vector3.up : Vector3.forward;
            float axial=scale[capsule.direction];
            float radial=capsule.direction==0 ? Mathf.Max(scale.y,scale.z) : capsule.direction==1 ? Mathf.Max(scale.x,scale.z) : Mathf.Max(scale.x,scale.y);
            radius=capsule.radius*radial;
            float half=Mathf.Max(0,capsule.height*axial*.5f-radius);
            Vector3 center=capsule.transform.TransformPoint(capsule.center);
            Vector3 offset=capsule.transform.TransformDirection(axis).normalized*half;
            start=center-offset;end=center+offset;
        }
        else if(other is SphereCollider sphere)
        {
            start=end=sphere.transform.TransformPoint(sphere.center);
            Vector3 scale=sphere.transform.lossyScale;
            radius=sphere.radius*Mathf.Max(Mathf.Abs(scale.x),Mathf.Abs(scale.y),Mathf.Abs(scale.z));
        }
        else return false;
        // A small forgiveness margin avoids grazing damage on balloon edges.
        radius=Mathf.Max(0,radius-.02f);
        Matrix4x4 matrix=meshTransform.localToWorldMatrix;
        for(int i=0;i<worldVertices.Length;i++) worldVertices[i]=matrix.MultiplyPoint3x4(geometry.vertices[i]);
        int[] triangles=geometry.triangles;
        for(int i=0;i+2<triangles.Length;i+=3)
            if(CapsuleTouchesTriangle(start,end,radius,worldVertices[triangles[i]],worldVertices[triangles[i+1]],worldVertices[triangles[i+2]])) return true;
        return false;
    }
    public static bool CapsuleTouchesTriangle(Vector3 start,Vector3 end,float radius,Vector3 a,Vector3 b,Vector3 c)
    {
        Vector3 normal=Vector3.Cross(b-a,c-a);
        if(normal.sqrMagnitude<1e-12f) return false;
        float denominator=Vector3.Dot(normal,end-start);
        if(Mathf.Abs(denominator)>1e-8f)
        {
            float t=Vector3.Dot(normal,a-start)/denominator;
            if(t>=0 && t<=1)
            {
                Vector3 point=Vector3.Lerp(start,end,t);
                if((ClosestTriangle(point,a,b,c)-point).sqrMagnitude<1e-10f) return true;
            }
        }
        float limit=radius*radius;
        return (start-ClosestTriangle(start,a,b,c)).sqrMagnitude<=limit
            || (end-ClosestTriangle(end,a,b,c)).sqrMagnitude<=limit
            || SegmentDistance(start,end,a,b)<=limit || SegmentDistance(start,end,b,c)<=limit || SegmentDistance(start,end,c,a)<=limit;
    }
    private static Vector3 ClosestTriangle(Vector3 p,Vector3 a,Vector3 b,Vector3 c)
    {
        Vector3 ab=b-a,ac=c-a,ap=p-a;
        float d1=Vector3.Dot(ab,ap),d2=Vector3.Dot(ac,ap);
        if(d1<=0 && d2<=0) return a;
        Vector3 bp=p-b;float d3=Vector3.Dot(ab,bp),d4=Vector3.Dot(ac,bp);
        if(d3>=0 && d4<=d3) return b;
        float vc=d1*d4-d3*d2;
        if(vc<=0 && d1>=0 && d3<=0) return a+ab*(d1/(d1-d3));
        Vector3 cp=p-c;float d5=Vector3.Dot(ab,cp),d6=Vector3.Dot(ac,cp);
        if(d6>=0 && d5<=d6) return c;
        float vb=d5*d2-d1*d6;
        if(vb<=0 && d2>=0 && d6<=0) return a+ac*(d2/(d2-d6));
        float va=d3*d6-d5*d4;
        if(va<=0 && d4-d3>=0 && d5-d6>=0) return b+(c-b)*((d4-d3)/((d4-d3)+(d5-d6)));
        float inverse=1/(va+vb+vc);return a+ab*(vb*inverse)+ac*(vc*inverse);
    }
    private static float SegmentDistance(Vector3 p,Vector3 q,Vector3 a,Vector3 b)
    {
        Vector3 d1=q-p,d2=b-a,r=p-a;
        float e1=Vector3.Dot(d1,d1),e2=Vector3.Dot(d2,d2),f=Vector3.Dot(d2,r),s,t;
        if(e1<=1e-12f && e2<=1e-12f) return r.sqrMagnitude;
        if(e1<=1e-12f) { s=0;t=Mathf.Clamp01(f/e2); }
        else
        {
            float c=Vector3.Dot(d1,r);
            if(e2<=1e-12f) { t=0;s=Mathf.Clamp01(-c/e1); }
            else
            {
                float cross=Vector3.Dot(d1,d2),denominator=e1*e2-cross*cross;
                s=denominator>1e-12f ? Mathf.Clamp01((cross*f-c*e2)/denominator) : 0;
                t=(cross*s+f)/e2;
                if(t<0) { t=0;s=Mathf.Clamp01(-c/e1); }
                else if(t>1) { t=1;s=Mathf.Clamp01((cross-c)/e1); }
            }
        }
        return (p+d1*s-a-d2*t).sqrMagnitude;
    }
}
