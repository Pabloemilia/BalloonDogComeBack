using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Passive, responsive air meter with a short damage trail and low-air warning.</summary>
public sealed class BalloonDogAirHud : MonoBehaviour
{
    private AirController source;
    private Image fill;
    private Image trail;
    private Image shell;
    private TMP_Text percentage;
    private TMP_Text status;
    private float target = 1f;
    private float displayed = 1f;
    private float trailing = 1f;
    private float trailDelay;

    public void Configure(AirController controller, Image airFill, Image damageTrail,
        Image background, TMP_Text valueLabel, TMP_Text statusLabel)
    {
        Unsubscribe();
        source = controller;
        fill = airFill;
        trail = damageTrail;
        shell = background;
        percentage = valueLabel;
        status = statusLabel;
        if (isActiveAndEnabled) Subscribe();
        Refresh();
    }

    private void OnEnable() { Subscribe(); Refresh(); }
    private void OnDisable() => Unsubscribe();
    private void Subscribe()
    {
        if (source == null) return;
        source.AirChanged -= HandleAirChanged;
        source.AirChanged += HandleAirChanged;
    }
    private void Unsubscribe()
    {
        if (source != null) source.AirChanged -= HandleAirChanged;
    }
    private void Refresh()
    {
        if (source == null) return;
        target = displayed = trailing = Mathf.Clamp01(source.NormalizedAir);
        trailDelay = 0f;
        Draw();
    }
    private void HandleAirChanged(float current, float maximum)
    {
        float next = maximum > 0f ? Mathf.Clamp01(current / maximum) : 0f;
        if (next < target - .001f)
        {
            trailing = Mathf.Max(trailing, displayed);
            trailDelay = .3f;
        }
        else if (next > target) trailing = Mathf.Max(trailing, next);
        target = next;
        Draw();
    }
    private void Update()
    {
        displayed = Mathf.Lerp(displayed, target, 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime));
        if (Mathf.Abs(displayed - target) < .001f) displayed = target;
        trailDelay = Mathf.Max(0f, trailDelay - Time.unscaledDeltaTime);
        if (trailDelay <= 0f) trailing = Mathf.MoveTowards(trailing, target, Time.unscaledDeltaTime * .55f);
        Draw();
    }
    private void Draw()
    {
        bool low = target <= .25f;
        if (fill != null)
        {
            fill.rectTransform.anchorMax = new Vector2(displayed, 1f);
            fill.enabled = displayed > .001f;
            fill.color = low ? new Color(1f, .31f, .22f) :
                Color.Lerp(new Color(1f, .76f, .24f), new Color(.20f, .96f, .82f), Mathf.InverseLerp(.25f, 1f, target));
        }
        if (trail != null)
        {
            trail.rectTransform.anchorMax = new Vector2(Mathf.Max(trailing, displayed), 1f);
            trail.enabled = trailing > .001f;
        }
        if (percentage != null) percentage.text = Mathf.RoundToInt(target * 100f) + "%";
        if (status != null)
        {
            status.text = low ? "LOW AIR" : "AIR";
            status.color = low ? new Color(1f, .56f, .42f) : new Color(.67f, .91f, 1f);
        }
        if (shell != null)
        {
            float pulse = low ? .08f + .06f * (1f + Mathf.Sin(Time.unscaledTime * 7f)) : 0f;
            shell.color = new Color(.025f + pulse, .075f, .16f, .94f);
        }
    }
}
