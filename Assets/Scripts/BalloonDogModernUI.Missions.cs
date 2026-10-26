using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class BalloonDogModernUI
{
    private const float MissionWindowWidth = 940f;
    private const float MissionWindowHeight = 1340f;
    private const float MissionRowHeight = 236f;
    private const float MissionViewportHeight = 736f;
    private GameObject missionsPopup;
    private RectTransform missionsWindow;
    private RectTransform missionsList;
    private TMP_Text missionsTimer;
    private TMP_Text missionsFooter;
    private Button dailyTab, normalTab;
    private CanvasGroup missionsMenuGroup;
    private ScrollRect missionsScroll;
    private bool normalMissions;
    private bool resetMissionScroll;
    private string missionsDisplayedDay;
    private float missionsNextUpdate;
    private readonly List<GameObject> missionRows = new List<GameObject>();
    private static readonly Color MissionInk = new Color(.025f,.19f,.45f);
    private static Sprite missionRoundedSprite;

    private void BuildMissionsPopup()
    {
        BalloonDogMissions.EnsureDay();
        missionRows.Clear();
        missionsMenuGroup = mainScreen.GetComponent<CanvasGroup>();
        if (missionsMenuGroup == null) missionsMenuGroup = mainScreen.AddComponent<CanvasGroup>();
        Button entry = CreateButton(mainScreen.transform,"MainMissionsButton","MISSIONS",
            Vector2.zero,new Vector2(250f,110f),Cyan,Color.white,ShowMissionsPopup,36f);
        RectTransform entryRect = (RectTransform)entry.transform;
        entryRect.anchorMin = entryRect.anchorMax = new Vector2(1f,1f);
        entryRect.anchoredPosition = new Vector2(-160f,-790f);

        RectTransform overlay = CreateRect("MissionsPopup",safeRoot);
        Stretch(overlay);
        missionsPopup = overlay.gameObject;
        Image scrim = overlay.gameObject.AddComponent<Image>();
        scrim.color = new Color(.015f,.08f,.22f,.60f);
        Button outside = overlay.gameObject.AddComponent<Button>();
        outside.transition = Selectable.Transition.None;
        outside.navigation = new Navigation { mode = Navigation.Mode.None };
        outside.onClick.AddListener(CloseMissionsPopup);

        missionsWindow = CreateRect("MissionsWindow",overlay);
        SetRect(missionsWindow,Vector2.zero,new Vector2(MissionWindowWidth,MissionWindowHeight));
        Image rim = missionsWindow.gameObject.AddComponent<Image>();
        RoundMissionImage(rim,66f);
        rim.color = new Color(.02f,.37f,.72f);
        AddGraphicShadow(rim,new Color(0f,.10f,.26f,.30f),new Vector2(0f,-12f));
        // Stop empty panel taps from bubbling up to the outside-close button.
        Button panelHit = missionsWindow.gameObject.AddComponent<Button>();
        panelHit.targetGraphic = rim;
        panelHit.transition = Selectable.Transition.None;
        panelHit.navigation = new Navigation { mode = Navigation.Mode.None };
        MissionSurface(missionsWindow,"CyanRim",Vector2.zero,new Vector2(932f,1332f),64f,
            new Color(.12f,1f,1f),new Color(0f,.61f,.98f));
        MissionSurface(missionsWindow,"WhiteRim",Vector2.zero,new Vector2(916f,1316f),56f,
            new Color(.91f,1f,1f),new Color(.44f,.91f,1f));
        MissionSurface(missionsWindow,"HeaderBase",Vector2.zero,new Vector2(906f,1306f),52f,
            new Color(0f,.81f,1f),new Color(0f,.52f,.98f));
        MissionSurface(missionsWindow,"Header",new Vector2(0f,574f),new Vector2(906f,154f),52f,
            new Color(.02f,.90f,1f),new Color(0f,.55f,1f));
        Image body = MissionSurface(missionsWindow,"Inner",new Vector2(0f,-65.5f),new Vector2(906f,1171f),42f,
            new Color(.92f,1f,1f),new Color(.86f,.97f,1f));
        body.raycastTarget = true;
        Image shine = MissionSurface(missionsWindow,"CornerShine",new Vector2(-393f,622f),new Vector2(47f,12f),6f,
            Color.white,new Color(1f,1f,1f,.8f));
        shine.rectTransform.localEulerAngles = new Vector3(0f,0f,32f);
        TMP_Text heading = CreateText(missionsWindow,"Heading","MISSIONS",new Vector2(-12f,581f),new Vector2(680f,116f),84f,
            Color.white,FontStyles.Normal,TextAlignmentOptions.Center);
        AddTextShadow(heading,new Color(0f,.36f,.68f,.35f),new Vector2(0f,-3f));
        BuildMissionClose();
        dailyTab = CreateButton(missionsWindow,"DailyTab","DAILY",new Vector2(-211f,445f),new Vector2(402f,114f),Cyan,Color.white,()=>SetMissionsTab(false),48f);
        normalTab = CreateButton(missionsWindow,"NormalTab","MISSIONS",new Vector2(211f,445f),new Vector2(402f,114f),Cyan,Color.white,()=>SetMissionsTab(true),48f);
        missionsTimer = CreateText(missionsWindow,"ResetTimer","",new Vector2(0f,337f),new Vector2(840f,52f),32f,
            new Color(.28f,.45f,.65f),FontStyles.Normal,TextAlignmentOptions.Center);

        RectTransform viewport = CreateRect("MissionViewport",missionsWindow);
        SetRect(viewport,new Vector2(0f,-86f),new Vector2(850f,MissionViewportHeight));
        Image hit = viewport.gameObject.AddComponent<Image>();
        hit.color = new Color(1f,1f,1f,.001f);
        viewport.gameObject.AddComponent<RectMask2D>();
        missionsList = CreateRect("MissionList",viewport);
        missionsList.anchorMin = new Vector2(0f,1f);
        missionsList.anchorMax = Vector2.one;
        missionsList.pivot = new Vector2(.5f,1f);
        missionsList.sizeDelta = new Vector2(0f,MissionViewportHeight);
        // Changing anchors alone preserves the old local position. Always pin the content's top explicitly.
        missionsList.anchoredPosition = Vector2.zero;
        missionsScroll = viewport.gameObject.AddComponent<ScrollRect>();
        missionsScroll.viewport = viewport;
        missionsScroll.content = missionsList;
        missionsScroll.horizontal = false;
        missionsScroll.vertical = false;
        missionsScroll.movementType = ScrollRect.MovementType.Clamped;
        missionsScroll.scrollSensitivity = 45f;
        missionsFooter = CreateText(missionsWindow,"PermanentHint","Stage 5 reward: "+BalloonDogMissionProgress.MilestoneGemReward+" gems\nSwipe up for more missions",
            new Vector2(0f,-550f),new Vector2(840f,100f),32f,MissionInk,FontStyles.Normal,TextAlignmentOptions.Center);
        missionsPopup.SetActive(false);
    }

    private void BuildMissionClose()
    {
        Button close = CreateButton(missionsWindow,"MissionsClose","X",new Vector2(389f,582f),new Vector2(104f,104f),Cyan,Color.white,CloseMissionsPopup,53f);
        Image image = close.GetComponent<Image>();
        RoundMissionImage(image,52f);
        image.color = new Color(0f,.39f,.80f);
        MissionSurface(close.transform,"CloseRim",Vector2.zero,new Vector2(96f,96f),48f,new Color(.68f,1f,1f),new Color(0f,.63f,.98f)).transform.SetAsFirstSibling();
        MissionSurface(close.transform,"CloseFace",Vector2.zero,new Vector2(86f,86f),43f,new Color(.02f,.92f,1f),new Color(0f,.52f,1f)).transform.SetSiblingIndex(1);
        close.GetComponentInChildren<TMP_Text>().transform.SetAsLastSibling();
    }

    private void ShowMissionsPopup()
    {
        BalloonDogMissions.EnsureDay();
        missionsPopup.transform.SetAsLastSibling();
        missionsPopup.SetActive(true);
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
        float scale = Mathf.Min(1f,Mathf.Min(width/1000f,height/1580f));
        missionsWindow.localScale = Vector3.one*Mathf.Max(.1f,scale);
    }
    private void SetMissionsTab(bool permanent)
    {
        normalMissions = permanent;
        resetMissionScroll = true;
        StyleMissionTab(dailyTab,!permanent);
        StyleMissionTab(normalTab,permanent);
        RebuildMissionRows();
    }
    private static void StyleMissionTab(Button button, bool selected)
    {
        Image image = button.GetComponent<Image>();
        Transform idle = button.transform.Find("IdleFace");
        if (idle == null)
        {
            Image face = MissionSurface(button.transform,"IdleFace",Vector2.zero,new Vector2(394f,106f),53f,
                new Color(.92f,1f,1f),new Color(.70f,.91f,.99f));
            idle = face.transform;
            idle.SetAsFirstSibling();
        }
        idle.gameObject.SetActive(!selected);
        if (selected) BalloonDogMenuArtwork.ApplyButtonBackground(image,false);
        else
        {
            RoundMissionImage(image,57f);
            image.color = new Color(.40f,.80f,.94f);
        }
        TMP_Text label = button.GetComponentInChildren<TMP_Text>();
        label.color = selected ? Color.white : MissionInk;
        label.transform.SetAsLastSibling();
    }
    private void RefreshMissionsTimer()
    {
        if (normalMissions) { missionsTimer.text = "Permanent progress"; return; }
        DateTime day = DateTime.ParseExact(BalloonDogMissions.Progress.Day,"yyyy-MM-dd",CultureInfo.InvariantCulture);
        TimeSpan remaining = day.AddDays(1)-DateTime.UtcNow;
        missionsTimer.text = "Resets in "+string.Format("{0:00}:{1:00}:{2:00}",Math.Max(0,(int)remaining.TotalHours),Math.Max(0,remaining.Minutes),Math.Max(0,remaining.Seconds));
    }
    private void UpdateMissionsPopup()
    {
        if (missionsPopup == null || !missionsPopup.activeSelf || Time.unscaledTime < missionsNextUpdate) return;
        missionsNextUpdate = Time.unscaledTime+.5f;
        BalloonDogMissions.EnsureDay();
        if (missionsDisplayedDay != BalloonDogMissions.Progress.Day)
        {
            resetMissionScroll = true;
            RebuildMissionRows();
        }
        FitMissionsWindow();
        RefreshMissionsTimer();
    }
    private static void DestroyMissionView(GameObject view)
    {
        if (Application.isPlaying) Destroy(view);
        else DestroyImmediate(view);
    }
    private void RebuildMissionRows()
    {
        float previousOffset = resetMissionScroll ? 0f : missionsList.anchoredPosition.y;
        missionsScroll.StopMovement();
        foreach (GameObject row in missionRows) { row.SetActive(false); DestroyMissionView(row); }
        missionRows.Clear();
        var state = BalloonDogMissions.Progress;
        missionsDisplayedDay = state.Day;
        int count = normalMissions ? 12 : 3;
        float height = normalMissions ? count*MissionRowHeight : MissionViewportHeight;
        missionsList.sizeDelta = new Vector2(0f,height);
        missionsScroll.vertical = normalMissions;
        for (int i=0;i<count;i++) BuildMissionRow(i,state);
        Transform previousBonus = missionsWindow.Find("DailyBonus");
        if (previousBonus != null) { previousBonus.gameObject.SetActive(false); DestroyMissionView(previousBonus.gameObject); }
        missionsFooter.gameObject.SetActive(normalMissions);
        if (!normalMissions) BuildDailyBonus(state);
        // Claims preserve a permanent list's scroll position; switching tabs starts at the first row.
        missionsList.anchoredPosition = new Vector2(0f,Mathf.Clamp(previousOffset,0f,Mathf.Max(0f,height-MissionViewportHeight)));
        resetMissionScroll = false;
        RefreshMissionsTimer();
        missionsNextUpdate = 0f;
    }
    private void BuildMissionRow(int id, BalloonDogMissionProgress state)
    {
        RectTransform row = CreateRect("Mission_"+id,missionsList);
        row.anchorMin = row.anchorMax = new Vector2(.5f,1f);
        row.pivot = new Vector2(.5f,1f);
        row.sizeDelta = new Vector2(850f,MissionRowHeight);
        row.anchoredPosition = new Vector2(0f,-id*MissionRowHeight);
        missionRows.Add(row.gameObject);
        bool permanent = normalMissions;
        int scale = BalloonDogMissions.Scale;
        int owned = BalloonDogMissions.OwnedSkins;
        bool done = permanent ? state.Finished(id) : state.DailyClaimed[id];
        bool ready = permanent ? state.CanClaimNormal(id,owned,scale) : state.CanClaimDaily(id,scale);
        string title = permanent ? state.NormalTitle(id,scale) : state.DailyTitle(id,scale);
        if (permanent) CreateText(row,"Stage",done ? "COMPLETED" : "STAGE "+(state.Stages[id]+1),
            new Vector2(-127f,-20f),new Vector2(562f,32f),25f,new Color(.25f,.50f,.68f),FontStyles.Normal,TextAlignmentOptions.Left);
        TMP_Text label = CreateText(row,"Task",title,new Vector2(-127f,permanent ? -86f : -66f),
            new Vector2(562f,permanent ? 96f : 108f),44f,MissionInk,FontStyles.Normal,TextAlignmentOptions.Left);
        label.enableWordWrapping = true;
        label.enableAutoSizing = true;
        label.fontSizeMin = 38f;
        label.fontSizeMax = 44f;
        double target = permanent ? state.NormalTarget(id,scale) : state.DailyTarget(id,scale);
        double value = permanent ? state.NormalValue(id,owned) : state.DailyValue(id);
        if (permanent && id == 4) { target /= 60d; value /= 60d; }
        MissionBar(row,new Vector2(-127f,-174f),new Vector2(562f,62f),Math.Min(1d,value/target),done ? "DONE" :
            Math.Floor(Math.Min(value,target)).ToString("N0",CultureInfo.InvariantCulture)+" / "+target.ToString("N0",CultureInfo.InvariantCulture));
        int gems = permanent ? state.NormalGemReward(id) : 0;
        int coins = permanent ? state.NormalCoinReward(id,scale) : state.DailyReward(id,scale);
        if (!done) MissionReward(row,new Vector2(307f,-57f),gems > 0 ? gems : coins,gems > 0);
        if (ready)
            CreateButton(row,"Claim","CLAIM",new Vector2(307f,-155f),new Vector2(232f,96f),MenuGreen,Color.white,()=>
            {
                if (permanent) BalloonDogMissions.ClaimNormal(id); else BalloonDogMissions.ClaimDaily(id);
                RebuildMissionRows();
            },38f);
        else CreateText(row,"Status",done ? "CLAIMED" : "IN PROGRESS",new Vector2(307f,-157f),new Vector2(232f,76f),
            27f,new Color(.32f,.50f,.67f),FontStyles.Normal,TextAlignmentOptions.Center);
        CreateImage(row,"Divider",new Vector2(0f,-231f),new Vector2(840f,2f),new Color(.25f,.75f,.9f,.5f),false);
        // Rows pivot at their top. Children must use the same origin, rather than the row's centre.
        // Mixing those origins previously added half a row of blank space and clipped the final bar.
        foreach (Transform child in row)
        {
            RectTransform rect = child as RectTransform;
            if (rect == null) continue;
            Vector2 position = rect.anchoredPosition;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f,1f);
            rect.anchoredPosition = position;
        }
    }
    private void BuildDailyBonus(BalloonDogMissionProgress state)
    {
        RectTransform bonus = CreateRect("DailyBonus",missionsWindow);
        SetRect(bonus,new Vector2(0f,-550f),new Vector2(850f,176f));
        MissionSurface(bonus,"BonusWell",Vector2.zero,new Vector2(850f,176f),26f,
            new Color(.80f,.94f,1f),new Color(.84f,.97f,1f));
        CreateText(bonus,"Title","Complete all 3",new Vector2(-127f,40f),new Vector2(562f,66f),42f,
            MissionInk,FontStyles.Normal,TextAlignmentOptions.Left);
        int completed = state.CompletedDailies(BalloonDogMissions.Scale);
        MissionBar(bonus,new Vector2(-127f,-30f),new Vector2(562f,62f),completed/3d,completed+" / 3");
        MissionReward(bonus,new Vector2(307f,41f),60*BalloonDogMissions.Scale,false);
        Button claim = CreateButton(bonus,"ClaimBonus",state.BonusClaimed ? "CLAIMED" : state.CanClaimBonus ? "CLAIM" : "LOCKED",
            new Vector2(307f,-40f),new Vector2(232f,94f),MenuGreen,Color.white,()=>{ BalloonDogMissions.ClaimBonus(); RebuildMissionRows(); },36f);
        claim.interactable = state.CanClaimBonus;
        if (!claim.interactable)
        {
            Image face = claim.GetComponent<Image>();
            RoundMissionImage(face,47f);
            face.color = new Color(.47f,.64f,.76f);
            MissionSurface(claim.transform,"DisabledFace",Vector2.zero,new Vector2(222f,84f),42f,
                new Color(.83f,.92f,.97f),new Color(.56f,.71f,.82f)).transform.SetAsFirstSibling();
            ColorBlock colors = claim.colors;
            colors.disabledColor = Color.white;
            claim.colors = colors;
            claim.GetComponentInChildren<TMP_Text>().color = new Color(.96f,.99f,1f);
        }
    }
    private static void MissionReward(Transform parent, Vector2 position, int amount, bool gems)
    {
        Image icon = CreateResourceImage(parent,"RewardIcon",gems ? "MarketUI/Extras/Icon_Gems" : "MarketUI/Extras/Icon_Coins",
            position+new Vector2(-91f,0f),new Vector2(56f,56f));
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        TMP_Text value = CreateText(parent,"RewardValue",amount.ToString("N0",CultureInfo.InvariantCulture),
            position+new Vector2(27f,0f),new Vector2(178f,70f),46f,MissionInk,FontStyles.Normal,TextAlignmentOptions.Left);
        value.enableAutoSizing = true;
        value.fontSizeMin = 36f;
        value.fontSizeMax = 46f;
    }
    private static void MissionBar(Transform parent, Vector2 position, Vector2 size, double fraction, string value)
    {
        fraction = Math.Max(0d,Math.Min(1d,fraction));
        Image track = MissionSurface(parent,"ProgressTrack",position,size,size.y*.5f,
            new Color(.13f,.68f,.88f),new Color(.06f,.53f,.75f));
        MissionSurface(track.transform,"TrackWell",Vector2.zero,size-new Vector2(6f,6f),(size.y-6f)*.5f,
            new Color(.75f,.92f,.99f),new Color(.57f,.81f,.93f));
        float fillWidth = Mathf.Max(0f,(size.x-8f)*(float)fraction);
        Image fill = MissionSurface(track.transform,"ProgressFill",new Vector2(-(size.x-8f-fillWidth)*.5f,0f),
            new Vector2(fillWidth,size.y-8f),Mathf.Max(1f,Mathf.Min(fillWidth*.5f,(size.y-8f)*.5f)),
            new Color(.15f,1f,.67f),new Color(0f,.72f,.39f));
        fill.gameObject.SetActive(fillWidth >= 2f);
        CreateText(track.transform,"ProgressValue",value,Vector2.zero,size-new Vector2(14f,2f),33f,
            MissionInk,FontStyles.Normal,TextAlignmentOptions.Center);
    }
    private static Image MissionSurface(Transform parent, string name, Vector2 position, Vector2 size, float radius, Color top, Color bottom)
    {
        Image image = CreateImage(parent,name,position,size,Color.white,false);
        RoundMissionImage(image,radius);
        image.gameObject.AddComponent<UiVerticalGradient>().Configure(top,bottom);
        return image;
    }
    private static void RoundMissionImage(Image image, float radius)
    {
        image.sprite = GetMissionRoundedSprite();
        image.type = Image.Type.Sliced;
        image.preserveAspect = false;
        image.pixelsPerUnitMultiplier = 60f/Mathf.Max(1f,radius);
    }
    private static Sprite GetMissionRoundedSprite()
    {
        if (missionRoundedSprite != null) return missionRoundedSprite;
        const int size = 128;
        const float radius = 60f;
        Texture2D texture = new Texture2D(size,size,TextureFormat.RGBA32,false);
        texture.name = "MissionRoundedSurface";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        texture.hideFlags = HideFlags.HideAndDontSave;
        Color32[] pixels = new Color32[size*size];
        float half = (size-1)*.5f;
        for (int y=0;y<size;y++)
            for (int x=0;x<size;x++)
            {
                float dx = Mathf.Max(Mathf.Abs(x-half)-(half-radius),0f);
                float dy = Mathf.Max(Mathf.Abs(y-half)-(half-radius),0f);
                byte alpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(radius+.5f-Mathf.Sqrt(dx*dx+dy*dy))*255f);
                pixels[y*size+x] = new Color32(255,255,255,alpha);
            }
        texture.SetPixels32(pixels);
        texture.Apply(false,true);
        missionRoundedSprite = Sprite.Create(texture,new Rect(0f,0f,size,size),new Vector2(.5f,.5f),100f,0,
            SpriteMeshType.FullRect,new Vector4(radius,radius,radius,radius));
        missionRoundedSprite.name = "MissionRoundedSurface";
        return missionRoundedSprite;
    }
}
