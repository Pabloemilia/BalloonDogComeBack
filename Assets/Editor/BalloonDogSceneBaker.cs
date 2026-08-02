#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BalloonDogSceneBaker
{
    private const string MenuPath = "Tools/BalloonDog/Sahneyi Düzenlenebilir Hale Getir";

    [MenuItem(MenuPath)]
    public static void BakeActiveScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog(
                "BalloonDog",
                "Önce Play modunu kapat, sonra bu aracı tekrar çalıştır.",
                "Tamam");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            EditorUtility.DisplayDialog(
                "BalloonDog",
                "Önce Assets/Scenes/Game.unity sahnesini aç.",
                "Tamam");
            return;
        }

        GameObject player = GameObject.Find("player") ?? GameObject.Find("Player");
        if (player == null)
        {
            EditorUtility.DisplayDialog(
                "BalloonDog",
                "Sahnede player veya Player isimli nesne bulunamadı. Game.unity sahnesini açtığından emin ol.",
                "Tamam");
            return;
        }

        bool confirmed = EditorUtility.DisplayDialog(
            "BalloonDog sahnesini oluştur",
            "Yol, engeller, mavi balonlar, modeller ve arayüz sahneye kalıcı olarak yerleştirilecek. " +
            "Araç tekrar çalıştırılırsa önceki oluşturulan BalloonDog_Level yenilenecek.",
            "Oluştur ve Kaydet",
            "Vazgeç");

        if (!confirmed)
        {
            return;
        }

        try
        {
            EditorUtility.DisplayProgressBar(
                "BalloonDog",
                "Düzenlenebilir sahne oluşturuluyor...",
                0.35f);

            GameObject temporaryObject = new GameObject("__BalloonDogSceneBaker");
            BalloonDogPrototypeBootstrap bootstrap =
                temporaryObject.AddComponent<BalloonDogPrototypeBootstrap>();

            bootstrap.BuildEditableScene();
            Object.DestroyImmediate(temporaryObject);

            BalloonDogRevisionV4Installer.ApplyToActiveScene(false);

            MarkWholeSceneDirty(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            GameObject levelRoot = GameObject.Find(
                BalloonDogPrototypeBootstrap.BakedLevelRootName);

            if (levelRoot != null)
            {
                Selection.activeGameObject = levelRoot;
                EditorGUIUtility.PingObject(levelRoot);
            }

            EditorUtility.DisplayDialog(
                "BalloonDog hazır",
                "Bütün oyun nesneleri artık Hierarchy ve Scene ekranında görünüyor; Revize V4 geliştirmeleri de uygulandı. " +
                "BalloonDog_Level klasörünü açarak yol, engeller ve mavi balonları düzenleyebilirsin.",
                "Tamam");
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
            EditorUtility.DisplayDialog(
                "BalloonDog oluşturulamadı",
                "Console'daki ilk kırmızı hatayı kontrol et.\n\n" + exception.Message,
                "Tamam");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    [MenuItem(MenuPath, true)]
    private static bool ValidateBakeActiveScene()
    {
        return !EditorApplication.isPlayingOrWillChangePlaymode;
    }

    private static void MarkWholeSceneDirty(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            EditorUtility.SetDirty(root);

            foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
            {
                EditorUtility.SetDirty(transform.gameObject);

                foreach (Component component in transform.GetComponents<Component>())
                {
                    if (component != null)
                    {
                        EditorUtility.SetDirty(component);
                    }
                }
            }
        }
    }
}
#endif
