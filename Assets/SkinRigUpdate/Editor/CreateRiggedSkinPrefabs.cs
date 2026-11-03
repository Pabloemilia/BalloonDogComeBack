#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
public static class CreateRiggedSkinPrefabs {
 [MenuItem("Balloon Dog/Skins/Create Rigged Prefabs")]
 public static void Build() {
  const string root="Assets/SkinRigUpdate";
  Directory.CreateDirectory(root+"/Prefabs"); Directory.CreateDirectory(root+"/Controllers"); AssetDatabase.Refresh();
  foreach(string path in Directory.GetFiles(root+"/Models","*.fbx",SearchOption.AllDirectories)) {
   string p=path.Replace('\\','/'); var importer=AssetImporter.GetAtPath(p) as ModelImporter;
   importer.animationType=ModelImporterAnimationType.Generic; importer.importAnimation=true; importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
   var clips=importer.defaultClipAnimations; foreach(var c in clips) {c.loopTime=true;c.loopPose=true;} importer.clipAnimations=clips; importer.SaveAndReimport();
   string name=Path.GetFileNameWithoutExtension(p); string prefabPath=root+"/Prefabs/skin_"+name+".prefab";
   // Preserve any prefab the user already adjusted on a previous run.
   if(File.Exists(prefabPath)) continue;
   string controllerPath=root+"/Controllers/"+name+".controller";
   var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
   if(controller==null) {
    controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);controller.AddParameter("Speed",AnimatorControllerParameterType.Float);
    var sm=controller.layers[0].stateMachine;
    var all=AssetDatabase.LoadAllAssetsAtPath(p).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
    AnimatorState idle=null,walk=null,run=null;
    foreach(string label in new[]{"Idle","Walk","Run"}) {
     var state=sm.AddState(label);state.motion=all.FirstOrDefault(c=>c.name.EndsWith(label,StringComparison.OrdinalIgnoreCase));
     if(label=="Idle")idle=state; else if(label=="Walk")walk=state;else run=state;
    }
    sm.defaultState=idle;
    Add(idle,walk,AnimatorConditionMode.Greater,.1f);Add(walk,idle,AnimatorConditionMode.Less,.1f);
    Add(walk,run,AnimatorConditionMode.Greater,1.5f);Add(run,walk,AnimatorConditionMode.Less,1.5f);
   }
   var model=AssetDatabase.LoadAssetAtPath<GameObject>(p);var instance=(GameObject)PrefabUtility.InstantiatePrefab(model);
   try {
    instance.name="skin_"+name;instance.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);instance.transform.localScale=Vector3.one;
    var animator=instance.GetComponent<Animator>();if(animator==null)animator=instance.AddComponent<Animator>();
    animator.avatar=AssetDatabase.LoadAllAssetsAtPath(p).OfType<Avatar>().FirstOrDefault();animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;
    PrefabUtility.SaveAsPrefabAsset(instance,prefabPath);
   } finally {UnityEngine.Object.DestroyImmediate(instance);}
  }
  AssetDatabase.SaveAssets();AssetDatabase.Refresh();Debug.Log("Rigged skin prefabs ready: Assets/SkinRigUpdate/Prefabs. Animator Speed: 0=Idle, 1=Walk, 2=Run.");
 }
 static void Add(AnimatorState from,AnimatorState to,AnimatorConditionMode mode,float limit) {
  var t=from.AddTransition(to);t.hasExitTime=false;t.duration=.15f;t.AddCondition(mode,limit,"Speed");
 }
}
#endif
