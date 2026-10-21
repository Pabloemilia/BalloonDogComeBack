#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

/// <summary>Optional deterministic input smoke check; needs no EventSystem or running scene.</summary>
public static class BalloonDogButtonInputCheck
{
    [MenuItem("Balloon Dog/Check End Screen Button Input")]
    private static void Run()
    {
        if (EditorApplication.isPlaying) { Debug.LogWarning("Exit Play mode before running the button input check."); return; }
        GameObject root = null;
        Mouse mouse = null;
        Mouse previousMouse = Mouse.current;
        float oldTimeScale = Time.timeScale;
        try
        {
            mouse = InputSystem.AddDevice<Mouse>("BalloonDogButtonCheckMouse");
            mouse.MakeCurrent();
            root = new GameObject("ButtonInputCheck", typeof(RectTransform), typeof(Canvas));
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            RectTransform rect = new GameObject("TestButton", typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<RectTransform>();
            rect.SetParent(root.transform, false);
            rect.sizeDelta = new Vector2(240, 100);
            Button button = rect.GetComponent<Button>();
            int clicks = 0;
            button.onClick.AddListener(() => clicks++);
            BalloonDogDirectPressButton driver = rect.gameObject.AddComponent<BalloonDogDirectPressButton>();
            Vector2 center = RectTransformUtility.WorldToScreenPoint(null, rect.position);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = center }); InputSystem.Update();
            driver.Configure(button, rect, Vector2.zero, false);
            Canvas.ForceUpdateCanvases();
            center = RectTransformUtility.WorldToScreenPoint(null, rect.position);
            Time.timeScale = 0f;
            Action<Vector2, bool> send = (pos, pressed) =>
            {
                InputSystem.QueueStateEvent(mouse, new MouseState { position = pos, buttons = (ushort)(pressed ? 1 : 0) });
                InputSystem.Update();
                typeof(BalloonDogDirectPressButton).GetMethod("Update", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(driver, null);
            };
            send(center, true);
            if (rect.localScale.x >= .99f || rect.GetComponent<Image>().color.r >= .99f) throw new Exception("No immediate visual feedback while timeScale is zero.");
            send(center, false);
            if (clicks != 1) throw new Exception("A short tap did not invoke exactly one action.");
            send(center, false);
            if (clicks != 1) throw new Exception("Release dispatched a duplicate action.");
            driver.enabled = false; driver.enabled = true;
            if (Mathf.Abs(rect.localScale.x - 1f) > .001f) throw new Exception("Visual state did not reset on disable.");
            send(center, true); send(center + Vector2.right * 1000, true); send(center + Vector2.right * 1000, false);
            if (clicks != 1) throw new Exception("Dragging off the button incorrectly clicked it.");
            button.interactable = false;
            send(center, true); send(center, false);
            if (clicks != 1) throw new Exception("A disabled control incorrectly clicked.");
            Debug.Log("PASS: direct input, frozen-time press animation, short tap, exactly-once action, drag cancellation and disabled-state protection.");
        }
        catch (Exception error) { Debug.LogError("Button input check failed: " + error); }
        finally
        {
            Time.timeScale = oldTimeScale;
            if (root != null) UnityEngine.Object.DestroyImmediate(root);
            if (mouse != null) InputSystem.RemoveDevice(mouse);
            if (previousMouse != null && previousMouse.added) previousMouse.MakeCurrent();
        }
    }
}
#endif
