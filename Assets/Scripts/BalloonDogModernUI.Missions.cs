using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class BalloonDogModernUI
{
    private GameObject missionsPopup;
    private RectTransform missionsWindow;
    private RectTransform missionsList;
    private TMP_Text missionsTimer;
    private TMP_Text missionsFooter;
    private Button dailyTab, normalTab;
    private CanvasGroup missionsMenuGroup;
    private bool normalMissions;
    private string missionsDisplayedDay;
    private float missionsNextUpdate;
    private readonly List<GameObject> missionRows = new List<GameObject>();
    private static readonly Color MissionInk = new Color(.025f,.19f,.45f);

    private void BuildMissionsPopup()
    {
        BalloonDogMissions.EnsureDay();
        missionRows.Clear();
        missionsMenuGroup = mainScreen.GetComponent<CanvasGroup>();
        if (missionsMenuGroup == null) missionsMenuGroup = mainScreen.AddComponent<CanvasGroup>();
        Button entry = CreateButton(mainScreen.transform, "MainMissionsButton", "MISSIONS",
            Vector2.zero, new Vector2(250f,110f), Cyan, Color.white, ShowMissionsPopup, 36f);
        RectTransform entryRect = (RectTransform)entry.transform;
        entryRect.anchorMin = entryRect.anchorMax = new Vector2(1f,1f);
        entryRect.anchoredPosition = new Vector2(-160f,-790f);

        RectTransform overlay = CreateRect("MissionsPopup", safeRoot);
        Stretch(overlay);
        missionsPopup = overlay.gameObject;
        Image scrim = overlay.gameObject.AddComponent<Image>();
        scrim.color = new Color(.015f,.08f,.22f,.58f);
        Button outside = overlay.gameObject.AddComponent<Button>();
        outside.transition = Selectable.Transition.None;
        outside.onClick.AddListener(CloseMissionsPopup);
        missionsWindow = CreateRect("MissionsWindow",overlay);
        SetRect(missionsWindow,Vector2.zero,new Vector2(940f,1260f));
        Image rim = missionsWindow.gameObject.AddComponent<Image>();
        rim.sprite = GetRoundedSprite(); rim.type = Image.Type.Sliced; rim.color = Cyan;
        Button panelHit = missionsWindow.gameObject.AddComponent<Button>();
        panelHit.targetGraphic = rim; panelHit.transition = Selectable.Transition.None;
        panelHit.navigation = new Navigation { mode = Navigation.Mode.None };
        AddGraphicShadow(rim,new Color(0f,.1f,.3f,.3f),new Vector2(0f,-10f));
        // This surface consumes empty-space taps so they never hit the outside-close button.
        Image inner = CreateImage(missionsWindow,"Inner",Vector2.zero,new Vector2(922f,1242f),new Color(.90f,.99f,1f),false);
        inner.raycastTarget = true;
        Image header = CreateImage(missionsWindow,"Header",new Vector2(0f,535f),new Vector2(920f,170f),Cyan,false);
        header.gameObject.AddComponent<UiVerticalGradient>().Configure(new Color(.02f,.90f,1f),new Color(0f,.52f,1f));
        CreateText(missionsWindow,"Heading","MISSIONS",new Vector2(0f,545f),new Vector2(660f,100f),65f,Color.white,FontStyles.Normal,TextAlignmentOptions.Center);
        CreateButton(missionsWindow,"MissionsClose","X",new Vector2(390f,545f),new Vector2(90f,90f),Cyan,Color.white,CloseMissionsPopup,43f);
        dailyTab = CreateButton(missionsWindow,"DailyTab","DAILY",new Vector2(-210f,405f),new Vector2(400f,112f),Cyan,Color.white,()=>SetMissionsTab(false),43f);
        normalTab = CreateButton(missionsWindow,"NormalTab","MISSIONS",new Vector2(210f,405f),new Vector2(400f,112f),Cyan,Color.white,()=>SetMissionsTab(true),43f);
        missionsTimer = CreateText(missionsWindow,"ResetTimer","",new Vector2(0f,307f),new Vector2(840f,54f),29f,new Color(.28f,.45f,.65f),FontStyles.Normal,TextAlignmentOptions.Center);

        RectTransform viewport = CreateRect("MissionViewport",missionsWindow);
        SetRect(viewport,new Vector2(0f,-75f),new Vector2(850f,690f));
        Image hit = viewport.gameObject.AddComponent<Image>(); hit.color = new Color(1f,1f,1f,.001f);
        viewport.gameObject.AddComponent<RectMask2D>();
        missionsList = CreateRect("MissionList",viewport);
        missionsList.anchorMin = new Vector2(0f,1f); missionsList.anchorMax = Vector2.one;
        missionsList.pivot = new Vector2(.5f,1f);
        missionsList.sizeDelta = new Vector2(0f,690f);
        ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport; scroll.content = missionsList;
        scroll.horizontal = false; scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 45f;
        missionsFooter = CreateText(missionsWindow,"PermanentHint","",new Vector2(0f,-520f),new Vector2(840f,80f),28f,MissionInk,FontStyles.Normal,TextAlignmentOptions.Center);
        missionsPopup.SetActive(false);
    }

    private void ShowMissionsPopup()
    {
        BalloonDogMissions.EnsureDay();
        missionsPopup.transform.SetAsLastSibling();
        missionsPopup.SetActive(true);
        // Existing menu buttons also poll raw input. CanvasGroup blocks those while the modal is open.
        if (missionsMenuGroup != null) missionsMenuGroup.interactable = false;
        SetMissionsTab(false);
        FitMissionsWindow();
    }
    private void CloseMissionsPopup()
    {
        SetActive(missionsPopup,false);
        if (missionsMenuGroup != null) missionsMenuGroup.interactable = true;
    }
    private void FitMissionsWindow()
    {
        float width = safeRoot.rect.width, height = safeRoot.rect.height;
        float scale = Mathf.Min(1f,Mathf.Min(width/1000f,height/1560f));
        missionsWindow.localScale = Vector3.one * Mathf.Max(.1f,scale);
    }
    private void SetMissionsTab(bool permanent)
    {
        normalMissions = permanent;
        StyleMissionTab(dailyTab,!permanent); StyleMissionTab(normalTab,permanent);
        missionsList.GetComponentInParent<ScrollRect>().verticalNormalizedPosition = 1f;
        RebuildMissionRows();
    }
    private static void StyleMissionTab(Button button, bool selected)
    {
        Image image = button.GetComponent<Image>();
        if (selected) BalloonDogMenuArtwork.ApplyButtonBackground(image,false);
        else { image.sprite = GetRoundedSprite(); image.type = Image.Type.Sliced; image.color = new Color(.73f,.94f,1f); }
        button.GetComponentInChildren<TMP_Text>().color = selected ? Color.white : MissionInk;
    }
    private void UpdateMissionsPopup()
    {
        if (missionsPopup == null || !missionsPopup.activeSelf || Time.unscaledTime < missionsNextUpdate) return;
        missionsNextUpdate = Time.unscaledTime + .5f;
        BalloonDogMissions.EnsureDay();
        if (missionsDisplayedDay != BalloonDogMissions.Progress.Day) RebuildMissionRows();
        FitMissionsWindow();
        if (normalMissions) missionsTimer.text = "Permanent progress";
        else
        {
            DateTime day = DateTime.ParseExact(BalloonDogMissions.Progress.Day,"yyyy-MM-dd",CultureInfo.InvariantCulture);
            TimeSpan remaining = day.AddDays(1) - DateTime.UtcNow;
            missionsTimer.text = "Resets in " + string.Format("{0:00}:{1:00}:{2:00}",Math.Max(0,(int)remaining.TotalHours),Math.Max(0,remaining.Minutes),Math.Max(0,remaining.Seconds));
        }
    }
    private static void DestroyMissionView(GameObject view)
    {
        if (Application.isPlaying) Destroy(view);
        else DestroyImmediate(view);
    }
    private void RebuildMissionRows()
    {
        foreach (GameObject row in missionRows) { row.SetActive(false); DestroyMissionView(row); }
        missionRows.Clear();
        var state = BalloonDogMissions.Progress;
        missionsDisplayedDay = state.Day;
        int count = normalMissions ? 12 : 3;
        missionsList.sizeDelta = new Vector2(0f,normalMissions ? count*215f : 690f);
        for (int i=0;i<count;i++) BuildMissionRow(i,state);
        Transform previousBonus = missionsWindow.Find("DailyBonus");
        if (previousBonus != null) { previousBonus.gameObject.SetActive(false); DestroyMissionView(previousBonus.gameObject); }
        missionsFooter.gameObject.SetActive(normalMissions);
        missionsFooter.text = "Stage 5 reward: 5 gems\nSwipe up for more missions";
        if (!normalMissions) BuildDailyBonus(state);
        missionsNextUpdate = 0f;
    }
    private void BuildMissionRow(int id, BalloonDogMissionProgress state)
    {
        RectTransform row = CreateRect("Mission_"+id,missionsList);
        row.anchorMin = row.anchorMax = new Vector2(.5f,1f); row.pivot = new Vector2(.5f,1f);
        row.sizeDelta = new Vector2(850f,215f); row.anchoredPosition = new Vector2(0f,-id*215f);
        missionRows.Add(row.gameObject);
        bool permanent = normalMissions;
        int scale = BalloonDogMissions.Scale;
        int owned = BalloonDogMissions.OwnedSkins;
        bool done = permanent ? state.Finished(id) : state.DailyClaimed[id];
        bool ready = permanent ? state.CanClaimNormal(id,owned,scale) : state.CanClaimDaily(id,scale);
        string title = permanent ? state.NormalTitle(id,scale) : state.DailyTitle(id,scale);
        if (permanent) CreateText(row,"Stage",done ? "COMPLETED" : "STAGE "+(state.Stages[id]+1),new Vector2(-125f,-22f),new Vector2(570f,32f),23f,new Color(.25f,.50f,.68f),FontStyles.Normal,TextAlignmentOptions.Left);
        TMP_Text label = CreateText(row,"Task",title,new Vector2(-125f,permanent ? -79f : -63f),new Vector2(570f,permanent ? 70f : 84f),33f,MissionInk,FontStyles.Normal,TextAlignmentOptions.Left);
        label.enableWordWrapping = true; label.enableAutoSizing = true; label.fontSizeMin = 27f; label.fontSizeMax = 33f;
        double target = permanent ? state.NormalTarget(id,scale) : state.DailyTarget(id,scale);
        double value = permanent ? state.NormalValue(id,owned) : state.DailyValue(id);
        if (permanent && id==4) { target /= 60d; value /= 60d; }
        MissionBar(row,new Vector2(-125f,-146f),new Vector2(570f,48f),Math.Min(1d,value/target),done ? "DONE" : Math.Floor(Math.Min(value,target)).ToString("N0",CultureInfo.InvariantCulture)+" / "+target.ToString("N0",CultureInfo.InvariantCulture));
        int gems = permanent ? state.NormalGemReward(id) : 0;
        int coins = permanent ? state.NormalCoinReward(id,scale) : state.DailyReward(id,scale);
        if (!done) MissionReward(row,new Vector2(300f,-52f),gems>0?gems:coins,gems>0);
        if (ready)
            CreateButton(row,"Claim","CLAIM",new Vector2(300f,-133f),new Vector2(230f,80f),MenuGreen,Color.white,()=>
            {
                if (permanent) BalloonDogMissions.ClaimNormal(id); else BalloonDogMissions.ClaimDaily(id);
                RebuildMissionRows();
            },30f);
        else CreateText(row,"Status",done ? "CLAIMED" : "IN PROGRESS",new Vector2(300f,-130f),new Vector2(240f,70f),24f,new Color(.32f,.50f,.67f),FontStyles.Normal,TextAlignmentOptions.Center);
        CreateImage(row,"Divider",new Vector2(0f,-204f),new Vector2(840f,2f),new Color(.25f,.75f,.9f,.5f),false);
    }
    private void BuildDailyBonus(BalloonDogMissionProgress state)
    {
        RectTransform bonus = CreateRect("DailyBonus",missionsWindow);
        SetRect(bonus,new Vector2(0f,-500f),new Vector2(850f,150f));
        CreateText(bonus,"Title","Complete all 3",new Vector2(-125f,35f),new Vector2(570f,65f),34f,MissionInk,FontStyles.Normal,TextAlignmentOptions.Left);
        int completed = state.CompletedDailies(BalloonDogMissions.Scale);
        MissionBar(bonus,new Vector2(-125f,-28f),new Vector2(570f,48f),completed/3d,completed+" / 3");
        MissionReward(bonus,new Vector2(300f,38f),60*BalloonDogMissions.Scale,false);
        Button claim = CreateButton(bonus,"ClaimBonus",state.BonusClaimed ? "CLAIMED" : state.CanClaimBonus ? "CLAIM" : "LOCKED",new Vector2(300f,-33f),new Vector2(230f,80f),MenuGreen,Color.white,()=>{ BalloonDogMissions.ClaimBonus(); RebuildMissionRows(); },30f);
        claim.interactable = state.CanClaimBonus;
    }
    private static void MissionReward(Transform parent, Vector2 position, int amount, bool gems)
    {
        Image icon = CreateResourceImage(parent,"RewardIcon",gems ? "MarketUI/Extras/Icon_Gems" : "MarketUI/Extras/Icon_Coins",position+new Vector2(-80f,0f),new Vector2(48f,48f));
        icon.preserveAspect = true; icon.raycastTarget = false;
        CreateText(parent,"RewardValue",amount.ToString("N0",CultureInfo.InvariantCulture),position+new Vector2(32f,0f),new Vector2(155f,62f),36f,MissionInk,FontStyles.Normal,TextAlignmentOptions.Left);
    }
    private static void MissionBar(Transform parent, Vector2 position, Vector2 size, double fraction, string value)
    {
        Image track = CreateImage(parent,"ProgressTrack",position,size,new Color(.63f,.88f,.96f),false);
        Image fill = CreateImage(track.transform,"ProgressFill",Vector2.zero,Vector2.zero,new Color(.03f,.85f,.49f),false);
        RectTransform rect = fill.rectTransform;
        rect.anchorMin = Vector2.zero; rect.anchorMax = new Vector2((float)fraction,1f);
        rect.offsetMin = new Vector2(3f,3f); rect.offsetMax = new Vector2(-3f,-3f);
        fill.gameObject.SetActive(fraction > .015d);
        CreateText(track.transform,"ProgressValue",value,Vector2.zero,size-new Vector2(16f,2f),27f,MissionInk,FontStyles.Normal,TextAlignmentOptions.Center);
    }
}
