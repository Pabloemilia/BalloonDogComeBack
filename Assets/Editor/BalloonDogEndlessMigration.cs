using UnityEditor;

// The old example map has been retired. Guard by GUID so a user's replacement
// scene with the same filename can never be deleted by this migration.
[InitializeOnLoad]
public static class BalloonDogEndlessMigration
{
    static BalloonDogEndlessMigration() { EditorApplication.delayCall += RemoveRetiredExample; }
    private static void RemoveRetiredExample()
    {
        const string path="Assets/Scenes/SampleScene.unity";
        if(!EditorApplication.isPlayingOrWillChangePlaymode &&
           AssetDatabase.AssetPathToGUID(path)=="99c9720ab356a0642a771bea13969a05")
            AssetDatabase.DeleteAsset(path);
    }
}
