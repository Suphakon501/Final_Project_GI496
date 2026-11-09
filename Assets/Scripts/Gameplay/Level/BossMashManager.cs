using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BossMashManager : MonoBehaviour
{
    public static BossMashManager instance;
    public static bool IsMashActive => instance != null && instance.isMashActive;

    [Header("Boss Prefab & Spawn Settings")]
    [SerializeField] private GameObject bossPrefab;
    [SerializeField] private float spawnDistance = 1.5f;
    [SerializeField] private float qtePlayerOffset = 1.5f;
    [SerializeField] private float qteEnemyOffset = 1.5f;
    [SerializeField] private float playerMoveSpeed = 30f;
    [SerializeField] private float yOffset;
    [SerializeField] private float moveSpeed = 50f;
    [Header("QTE Camera Transition")]
    [SerializeField, Min(0.05f)] private float playerFocusDuration = 0.45f;
    [SerializeField, Min(0.05f)] private float enemyFocusDuration = 0.45f;
    [SerializeField, Min(0.05f)] private float faceoffDuration = 0.7f;
    [SerializeField, Min(0.1f)] private float closeupCameraSize = 2.7f;
    [SerializeField, Min(0.1f)] private float faceoffCameraSize = 4f;
    [SerializeField, Min(0.1f)] private float qteBossScaleMultiplier = 0.85f;
    [SerializeField] private int qteBossSortingOrder = 20;
    [SerializeField, Min(0.05f)] private float qteRevealDuration = 0.8f;
    [SerializeField, Range(0f, 0.8f)] private float qteRevealStartRadius = 0.35f;
    [SerializeField, Range(0.1f, 1.1f)] private float qteRevealRadius = 0.82f;
    [SerializeField, Min(0.05f)] private float qteExitDuration = 0.6f;

    [Header("QTE Hit Impact")]
    [SerializeField, Min(0.01f)] private float bossImpactDuration = 0.18f;
    [SerializeField, Min(0f)] private float bossRecoilDistance = 0.3f;
    [SerializeField, Min(0f)] private float bossTiltDegrees = 8f;
    [SerializeField, Range(0f, 0.5f)] private float bossSquashAmount = 0.12f;

    [Header("Mash Feedback")]
    [SerializeField] private GameObject bossHitEffectPrefab;
    [SerializeField, Min(0.1f)] private float bossHitEffectScale = 2.5f;
    [SerializeField] private Image keyFeedbackBackground;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hitColor = Color.yellow;
    [SerializeField] private int scorePerPress = 50;

    private bool isMashActive;
    private bool isExitingMash;
    private bool isBossMovingIn;
    private bool finishLevelOnEnd;
    private bool qteIntroComplete;
    private float qteIntroElapsed;
    private Camera qteCamera;
    private Vector3 cameraStartPosition;
    private float cameraStartSize;
    private CameraImpact cameraImpact;
    private LowHealthCameraShake lowHealthCameraShake;
    private bool cameraImpactWasEnabled;
    private bool lowHealthCameraShakeWasEnabled;
    private Vector3 cameraExitStartPosition;
    private float cameraExitStartSize;
    private float remainingSeconds;
    private float flashTimer;
    private GameObject currentBossInstance;
    private Transform qtePlayer;
    private SpriteRenderer[] qtePlayerSprites;
    private int[] playerSortingOrders;
    private Vector3 playerStartPosition;
    private Vector3 playerTargetPosition;
    private Vector3 bossTargetPosition;
    private Vector3 bossBasePosition;
    private Vector3 bossBaseScale;
    private Quaternion bossBaseRotation;
    private float bossImpactElapsed;
    private float bossImpactDirection = -1f;
    private GameObject qteRevealCanvas;
    private Material qteRevealMaterial;
    private float qteRevealElapsed;
    private Graphic[] hiddenGraphics;
    private bool[] graphicEnabledStates;
    private Canvas[] qtePersistentCanvases;
    private int[] persistentCanvasSortingOrders;
    private bool[] persistentCanvasOverrideSorting;
    private Renderer[] hiddenGameplayRenderers;
    private bool[] gameplayRendererEnabledStates;
    private MonoBehaviour[] pausedEnemyScripts;
    private bool[] enemyScriptEnabledStates;
    private GameObject hiddenHitline;
    private bool hitlineWasActive;
    void Awake() => instance = this;

    void OnDestroy()
    {
        RestoreQTECamera();
        if (instance == this) instance = null;
    }

    // Final encounter: keep the existing win-after-mash behavior.
    public void StartMash(float duration) => BeginMash(duration, true);

    // Chart encounter: leave music running and resume the chart at the current song time.
    public void StartChartQTE(float duration) => BeginMash(duration, false);

    void BeginMash(float duration, bool finishLevel)
    {
        if (isMashActive) return;

        isMashActive = true;
        isExitingMash = false;
        finishLevelOnEnd = finishLevel;
        remainingSeconds = Mathf.Max(0.1f, duration);
        isBossMovingIn = true;
        qteIntroComplete = false;
        qteIntroElapsed = 0f;
        flashTimer = 0f;

        if (PlayerController.instance != null) PlayerController.instance.SetQTEComboVisible(true);
        qtePlayer = PlayerController.instance != null ? PlayerController.instance.transform : null;
        playerStartPosition = qtePlayer != null ? qtePlayer.position : Vector3.zero;
        qtePlayerSprites = qtePlayer != null ? qtePlayer.GetComponentsInChildren<SpriteRenderer>(true) : null;
        if (qtePlayerSprites != null)
        {
            playerSortingOrders = new int[qtePlayerSprites.Length];
            for (int i = 0; i < qtePlayerSprites.Length; i++)
            {
                playerSortingOrders[i] = qtePlayerSprites[i].sortingOrder;
                qtePlayerSprites[i].sortingOrder = qteBossSortingOrder + 1;
            }
        }
        float centerX = playerStartPosition.x;
        Camera gameCamera = Camera.main;
        if (gameCamera != null)
        {
            qteCamera = gameCamera;
            cameraStartPosition = qteCamera.transform.position;
            cameraStartSize = qteCamera.orthographicSize;
            cameraImpact = qteCamera.GetComponent<CameraImpact>();
            lowHealthCameraShake = qteCamera.GetComponent<LowHealthCameraShake>();
            cameraImpactWasEnabled = cameraImpact != null && cameraImpact.enabled;
            lowHealthCameraShakeWasEnabled = lowHealthCameraShake != null && lowHealthCameraShake.enabled;
            if (cameraImpact != null) cameraImpact.enabled = false;
            if (lowHealthCameraShake != null) lowHealthCameraShake.enabled = false;
            float depth = Mathf.Abs(playerStartPosition.z - gameCamera.transform.position.z);
            centerX = gameCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, depth)).x;
        }
        playerTargetPosition = new Vector3(centerX - qtePlayerOffset, playerStartPosition.y, playerStartPosition.z);
        bossTargetPosition = new Vector3(centerX + qteEnemyOffset, playerStartPosition.y + yOffset, playerStartPosition.z);

        if (bossPrefab != null)
        {
            Vector3 spawnPosition = new Vector3(centerX + spawnDistance, bossTargetPosition.y, bossTargetPosition.z);
            currentBossInstance = Instantiate(bossPrefab, spawnPosition, Quaternion.identity);
            currentBossInstance.SetActive(true);
            currentBossInstance.transform.localScale *= qteBossScaleMultiplier;
            bossBaseScale = currentBossInstance.transform.localScale;
            bossBaseRotation = currentBossInstance.transform.localRotation;
            bossImpactElapsed = 0f;
            foreach (Renderer renderer in currentBossInstance.GetComponentsInChildren<Renderer>(true))
                renderer.enabled = true;
            foreach (SpriteRenderer sprite in currentBossInstance.GetComponentsInChildren<SpriteRenderer>(true))
                sprite.sortingOrder = qteBossSortingOrder;

            if (qtePlayer != null && TryGetSpriteBounds(qtePlayer, out Bounds playerBounds)
                && TryGetSpriteBounds(currentBossInstance.transform, out Bounds bossBounds))
            {
                bossTargetPosition.y += playerBounds.min.y - bossBounds.min.y;
                currentBossInstance.transform.position = new Vector3(spawnPosition.x, bossTargetPosition.y, spawnPosition.z);
            }
            bossBasePosition = currentBossInstance.transform.position;
        }
        else
            Debug.LogWarning("[BossMashManager] Boss prefab is not assigned");

        HideGameplayUIAndEnemies();
        EnsureQTEBossVisible();
        BeginQTEReveal();
    }

    void Update()
    {
        if (!isMashActive) return;
        if (PlayerController.instance != null) PlayerController.instance.KeepQTEHudVisible();
        if (isExitingMash)
        {
            UpdateQTEExit();
            return;
        }

        UpdateQTEReveal();

        UpdateQTECameraIntro();
        if (qteIntroComplete)
        {
            if (qtePlayer != null)
                qtePlayer.position = Vector3.MoveTowards(qtePlayer.position, playerTargetPosition, playerMoveSpeed * Time.unscaledDeltaTime);
            UpdateBossMovement();
        }
        EnsureQTEBossVisible();
        UpdateBossImpact();

        // Let the camera finish its player -> enemy -> faceoff sequence before accepting mash input or counting QTE time.
        if (!qteIntroComplete) return;

        if (!PlayerController.isGameOver && Input.GetKeyDown(KeyCode.Space))
        {
            if (PlayerController.instance != null) PlayerController.instance.PlayRandomQTEAttack();
            TriggerBossImpact();
            TriggerVisualFeedback();
            SpawnBossHitEffect();
            if (PlayerController.instance != null) PlayerController.instance.RegisterQTEHit(scorePerPress);
        }

        if (flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;
            if (flashTimer <= 0f && keyFeedbackBackground != null) keyFeedbackBackground.color = normalColor;
        }

        remainingSeconds -= Time.unscaledDeltaTime;
        if (remainingSeconds <= 0f)
        {
            if (qteRevealMaterial == null) EndMash();
            else
            {
                isExitingMash = true;
                qteRevealElapsed = 0f;
                if (qteCamera != null)
                {
                    cameraExitStartPosition = qteCamera.transform.position;
                    cameraExitStartSize = qteCamera.orthographicSize;
                }
            }
        }
    }

    void LateUpdate()
    {
        if (isMashActive && !isExitingMash)
            EnsureQTEBossVisible();
    }

    void UpdateBossMovement()
    {
        if (!isBossMovingIn || currentBossInstance == null) return;

        bossBasePosition = Vector3.MoveTowards(bossBasePosition, bossTargetPosition, moveSpeed * Time.deltaTime);
        if (Vector3.Distance(bossBasePosition, bossTargetPosition) < 0.01f) isBossMovingIn = false;
    }

    void UpdateQTECameraIntro()
    {
        if (qteIntroComplete)
        {
            if (qteCamera != null)
            {
                Vector3 target = playerTargetPosition + (bossTargetPosition - playerTargetPosition) * 0.5f;
                qteCamera.transform.position = Vector3.Lerp(qteCamera.transform.position, target + Vector3.forward * (cameraStartPosition.z - target.z), 1f - Mathf.Exp(-10f * Time.unscaledDeltaTime));
                qteCamera.orthographicSize = Mathf.Lerp(qteCamera.orthographicSize, Mathf.Min(cameraStartSize, faceoffCameraSize), 1f - Mathf.Exp(-10f * Time.unscaledDeltaTime));
            }
            return;
        }

        qteIntroElapsed += Time.unscaledDeltaTime;
        float playerEnd = Mathf.Max(0.05f, playerFocusDuration);
        float enemyEnd = playerEnd + Mathf.Max(0.05f, enemyFocusDuration);
        float introEnd = enemyEnd + Mathf.Max(0.05f, faceoffDuration);
        if (qteCamera != null)
        {
            Vector3 playerFocus = GetFocusPoint(qtePlayer, playerTargetPosition);
            Vector3 enemyFocus = GetFocusPoint(currentBossInstance != null ? currentBossInstance.transform : null, bossTargetPosition);
            float closeSize = Mathf.Min(cameraStartSize, closeupCameraSize);
            if (qteIntroElapsed < playerEnd)
            {
                float t = SmoothStep(qteIntroElapsed / playerEnd);
                qteCamera.transform.position = Vector3.Lerp(cameraStartPosition, playerFocus, t);
                qteCamera.orthographicSize = Mathf.Lerp(cameraStartSize, closeSize, t);
            }
            else if (qteIntroElapsed < enemyEnd)
            {
                float t = SmoothStep((qteIntroElapsed - playerEnd) / (enemyEnd - playerEnd));
                qteCamera.transform.position = Vector3.Lerp(playerFocus, enemyFocus, t);
                qteCamera.orthographicSize = closeSize;
            }
            else
            {
                float t = SmoothStep((qteIntroElapsed - enemyEnd) / (introEnd - enemyEnd));
                Vector3 meetingPoint = (playerTargetPosition + bossTargetPosition) * 0.5f;
                meetingPoint.z = cameraStartPosition.z;
                qteCamera.transform.position = Vector3.Lerp(enemyFocus, meetingPoint, t);
                qteCamera.orthographicSize = Mathf.Lerp(closeSize, Mathf.Min(cameraStartSize, faceoffCameraSize), t);

                if (qtePlayer != null)
                    qtePlayer.position = Vector3.MoveTowards(qtePlayer.position, playerTargetPosition, playerMoveSpeed * Time.unscaledDeltaTime);
                UpdateBossMovement();
            }
        }
        else
        {
            // Still stage the two characters together when no active camera is available.
            if (qteIntroElapsed >= enemyEnd)
            {
                if (qtePlayer != null)
                    qtePlayer.position = Vector3.MoveTowards(qtePlayer.position, playerTargetPosition, playerMoveSpeed * Time.unscaledDeltaTime);
                UpdateBossMovement();
            }
        }

        if (qteIntroElapsed >= introEnd) qteIntroComplete = true;
    }

    static Vector3 GetFocusPoint(Transform target, Vector3 fallback)
    {
        if (target != null && TryGetSpriteBounds(target, out Bounds bounds))
            return new Vector3(bounds.center.x, bounds.center.y, Camera.main != null ? Camera.main.transform.position.z : target.position.z);
        return fallback;
    }

    static float SmoothStep(float value)
    {
        value = Mathf.Clamp01(value);
        return value * value * (3f - 2f * value);
    }

    void TriggerBossImpact()
    {
        if (currentBossInstance == null) return;
        bossImpactDirection = -bossImpactDirection;
        bossImpactElapsed = Mathf.Max(0.01f, bossImpactDuration);
    }

    void UpdateBossImpact()
    {
        if (currentBossInstance == null) return;

        float impact = 0f;
        if (bossImpactElapsed > 0f)
        {
            bossImpactElapsed = Mathf.Max(0f, bossImpactElapsed - Time.unscaledDeltaTime);
            float progress = 1f - bossImpactElapsed / Mathf.Max(0.01f, bossImpactDuration);
            impact = Mathf.Sin(progress * Mathf.PI);
        }

        Transform boss = currentBossInstance.transform;
        boss.position = bossBasePosition + Vector3.right * (bossRecoilDistance * impact * bossImpactDirection);
        boss.localRotation = bossBaseRotation * Quaternion.Euler(0f, 0f, bossTiltDegrees * impact * -bossImpactDirection);
        boss.localScale = Vector3.Scale(bossBaseScale, new Vector3(1f - bossSquashAmount * impact, 1f + bossSquashAmount * impact, 1f));
    }

    void BeginQTEReveal()
    {
        Shader shader = Resources.Load<Shader>("QTERadialReveal");
        if (shader == null)
        {
            Debug.LogError("[BossMashManager] Missing Resources/QTERadialReveal shader");
            return;
        }

        qteRevealCanvas = new GameObject("QTERadialReveal", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(RawImage));
        Canvas canvas = qteRevealCanvas.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = -1000;

        CanvasScaler scaler = qteRevealCanvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        RawImage overlay = qteRevealCanvas.GetComponent<RawImage>();
        overlay.texture = Texture2D.whiteTexture;
        overlay.color = Color.white;
        overlay.raycastTarget = false;
        RectTransform rect = overlay.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        qteRevealMaterial = new Material(shader);
        qteRevealMaterial.SetFloat("_Radius", qteRevealStartRadius);
        qteRevealMaterial.SetFloat("_Edge", 0.04f);
        qteRevealMaterial.SetFloat("_SpeedLineIntensity", 1f);
        overlay.material = qteRevealMaterial;
        qteRevealElapsed = 0f;
    }

    void UpdateQTEReveal()
    {
        if (qteRevealMaterial == null) return;

        qteRevealElapsed += Time.unscaledDeltaTime;
        float progress = Mathf.Clamp01(qteRevealElapsed / qteRevealDuration);
        qteRevealMaterial.SetFloat("_Radius", Mathf.Lerp(qteRevealStartRadius, qteRevealRadius, progress));
        qteRevealMaterial.SetFloat("_SpeedLineTime", Time.unscaledTime);
    }

    void UpdateQTEExit()
    {
        qteRevealElapsed += Time.unscaledDeltaTime;
        float progress = Mathf.Clamp01(qteRevealElapsed / qteExitDuration);
        if (qteRevealMaterial != null)
        {
            qteRevealMaterial.SetFloat("_Radius", Mathf.Lerp(qteRevealRadius, 1.2f, progress));
            qteRevealMaterial.SetFloat("_SpeedLineIntensity", 1f - SmoothStep(progress));
            qteRevealMaterial.SetFloat("_SpeedLineTime", Time.unscaledTime);
        }
        if (qtePlayer != null)
            qtePlayer.position = Vector3.MoveTowards(qtePlayer.position, playerStartPosition, playerMoveSpeed * Time.unscaledDeltaTime);
        if (qteCamera != null)
        {
            qteCamera.transform.position = Vector3.Lerp(cameraExitStartPosition, cameraStartPosition, SmoothStep(progress));
            qteCamera.orthographicSize = Mathf.Lerp(cameraExitStartSize, cameraStartSize, SmoothStep(progress));
        }
        UpdateBossImpact();
        if (progress >= 1f) EndMash();
    }

    void EndQTEReveal()
    {
        if (qteRevealCanvas != null) Destroy(qteRevealCanvas);
        if (qteRevealMaterial != null) Destroy(qteRevealMaterial);
        qteRevealCanvas = null;
        qteRevealMaterial = null;
    }

    static bool TryGetSpriteBounds(Transform root, out Bounds bounds)
    {
        SpriteRenderer[] sprites = root.GetComponentsInChildren<SpriteRenderer>(true);
        if (sprites.Length == 0)
        {
            bounds = default;
            return false;
        }

        bounds = sprites[0].bounds;
        for (int i = 1; i < sprites.Length; i++) bounds.Encapsulate(sprites[i].bounds);
        return true;
    }

    void HideGameplayUIAndEnemies()
    {
        hiddenGraphics = FindObjectsByType<Graphic>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        graphicEnabledStates = new bool[hiddenGraphics.Length];
        for (int i = 0; i < hiddenGraphics.Length; i++)
        {
            graphicEnabledStates[i] = hiddenGraphics[i].enabled;
            if (PlayerController.instance == null || !PlayerController.instance.IsQTEPersistentUI(hiddenGraphics[i]))
                hiddenGraphics[i].enabled = false;
        }
        RaiseQTEPersistentCanvases();
        if (PlayerController.instance != null) PlayerController.instance.KeepQTEHudVisible();

        hiddenHitline = GameObject.Find("Hitline");
        if (hiddenHitline != null)
        {
            hitlineWasActive = hiddenHitline.activeSelf;
            hiddenHitline.SetActive(false);
        }

        var scripts = new List<MonoBehaviour>();
        var gameplayRenderers = new List<Renderer>();
        var seenRenderers = new HashSet<Renderer>();
        foreach (var enemy in LipidMovement.Active)
        {
            if (enemy == null) continue;
            scripts.Add(enemy);
            AddGameplayRenderers(enemy.transform, gameplayRenderers, seenRenderers);
        }
        foreach (var mini in MiniLipid.Active)
        {
            if (mini == null) continue;
            scripts.Add(mini);
            AddGameplayRenderers(mini.transform, gameplayRenderers, seenRenderers);
        }

        hiddenGameplayRenderers = gameplayRenderers.ToArray();
        gameplayRendererEnabledStates = new bool[hiddenGameplayRenderers.Length];
        for (int i = 0; i < hiddenGameplayRenderers.Length; i++)
        {
            gameplayRendererEnabledStates[i] = hiddenGameplayRenderers[i].enabled;
            hiddenGameplayRenderers[i].enabled = false;
        }

        pausedEnemyScripts = scripts.ToArray();
        enemyScriptEnabledStates = new bool[pausedEnemyScripts.Length];
        for (int i = 0; i < pausedEnemyScripts.Length; i++)
        {
            enemyScriptEnabledStates[i] = pausedEnemyScripts[i].enabled;
            pausedEnemyScripts[i].enabled = false;
        }
    }

    void RestoreGameplayUIAndEnemies()
    {
        if (hiddenGraphics != null)
            for (int i = 0; i < hiddenGraphics.Length; i++)
                if (hiddenGraphics[i] != null) hiddenGraphics[i].enabled = graphicEnabledStates[i];
        if (qtePersistentCanvases != null)
            for (int i = 0; i < qtePersistentCanvases.Length; i++)
                if (qtePersistentCanvases[i] != null)
                {
                    qtePersistentCanvases[i].overrideSorting = persistentCanvasOverrideSorting[i];
                    qtePersistentCanvases[i].sortingOrder = persistentCanvasSortingOrders[i];
                }
        if (hiddenHitline != null) hiddenHitline.SetActive(hitlineWasActive);
        if (hiddenGameplayRenderers != null)
            for (int i = 0; i < hiddenGameplayRenderers.Length; i++)
                if (hiddenGameplayRenderers[i] != null) hiddenGameplayRenderers[i].enabled = gameplayRendererEnabledStates[i];
        if (pausedEnemyScripts != null)
            for (int i = 0; i < pausedEnemyScripts.Length; i++)
                if (pausedEnemyScripts[i] != null)
                {
                    if (pausedEnemyScripts[i] is MiniLipid mini && mini.DiscardIfExpiredDuringQTE()) continue;
                    pausedEnemyScripts[i].enabled = enemyScriptEnabledStates[i];
                }

        hiddenGraphics = null;
        qtePersistentCanvases = null;
        persistentCanvasSortingOrders = null;
        persistentCanvasOverrideSorting = null;
        hiddenGameplayRenderers = null;
        pausedEnemyScripts = null;
    }

    static void AddGameplayRenderers(Transform root, List<Renderer> renderers, HashSet<Renderer> seen)
    {
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            if (seen.Add(renderer)) renderers.Add(renderer);
    }

    void RaiseQTEPersistentCanvases()
    {
        var canvases = new List<Canvas>();
        foreach (Graphic graphic in hiddenGraphics)
        {
            if (PlayerController.instance == null || !PlayerController.instance.IsQTEPersistentUI(graphic)) continue;
            Canvas canvas = graphic.GetComponentInParent<Canvas>(true);
            if (canvas != null && !canvases.Contains(canvas)) canvases.Add(canvas);
        }

        qtePersistentCanvases = canvases.ToArray();
        persistentCanvasSortingOrders = new int[qtePersistentCanvases.Length];
        persistentCanvasOverrideSorting = new bool[qtePersistentCanvases.Length];
        for (int i = 0; i < qtePersistentCanvases.Length; i++)
        {
            Canvas canvas = qtePersistentCanvases[i];
            persistentCanvasSortingOrders[i] = canvas.sortingOrder;
            persistentCanvasOverrideSorting[i] = canvas.overrideSorting;
            canvas.overrideSorting = true;
            canvas.sortingOrder = short.MaxValue;
        }
    }

    void EnsureQTEBossVisible()
    {
        if (currentBossInstance == null) return;
        foreach (Renderer renderer in currentBossInstance.GetComponentsInChildren<Renderer>(true))
        {
            renderer.enabled = true;
            renderer.forceRenderingOff = false;
        }
        foreach (SpriteRenderer sprite in currentBossInstance.GetComponentsInChildren<SpriteRenderer>(true))
            sprite.sortingOrder = qteBossSortingOrder;
    }

    void SpawnBossHitEffect()
    {
        if (bossHitEffectPrefab == null || currentBossInstance == null) return;

        GameObject effect = Instantiate(bossHitEffectPrefab, currentBossInstance.transform.position, Quaternion.identity);
        effect.transform.localScale = Vector3.one * bossHitEffectScale;
        SpriteRenderer effectSprite = effect.GetComponent<SpriteRenderer>();
        if (effectSprite != null)
        {
            effectSprite.sortingOrder = qteBossSortingOrder - 1;
            if (TryGetSpriteBounds(currentBossInstance.transform, out Bounds bossBounds))
                effect.transform.position += bossBounds.center - effectSprite.bounds.center;
        }
    }

    void TriggerVisualFeedback()
    {
        flashTimer = 0.1f;
        if (keyFeedbackBackground != null) keyFeedbackBackground.color = hitColor;
    }

    void EndMash()
    {
        isMashActive = false;
        isExitingMash = false;
        isBossMovingIn = false;
        if (qtePlayer != null) qtePlayer.position = playerStartPosition;
        if (qtePlayerSprites != null)
            for (int i = 0; i < qtePlayerSprites.Length; i++)
                if (qtePlayerSprites[i] != null) qtePlayerSprites[i].sortingOrder = playerSortingOrders[i];
        qtePlayer = null;
        qtePlayerSprites = null;
        playerSortingOrders = null;
        EndQTEReveal();
        RestoreQTECamera();
        if (currentBossInstance != null) Destroy(currentBossInstance);
        currentBossInstance = null;
        RestoreGameplayUIAndEnemies();
        if (PlayerController.instance != null) PlayerController.instance.RefreshComboUI();

        if (finishLevelOnEnd && LevelManager.instance != null)
            LevelManager.instance.FinishBossPhase();
    }

    void RestoreQTECamera()
    {
        if (qteCamera != null)
        {
            qteCamera.transform.position = cameraStartPosition;
            qteCamera.orthographicSize = cameraStartSize;
        }
        if (cameraImpact != null) cameraImpact.enabled = cameraImpactWasEnabled;
        if (lowHealthCameraShake != null) lowHealthCameraShake.enabled = lowHealthCameraShakeWasEnabled;
        qteCamera = null;
        cameraImpact = null;
        lowHealthCameraShake = null;
    }
}
