using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class PlayerAnimationBuilder
{
    const string SpriteRoot = "Assets/Sprites/Player";
    const string OutputFolder = "Assets/Animations/Player";
    const string ControllerPath = OutputFolder + "/PlayerMain.controller";

    const float BasePPU = 100f;
    const float PlayerTransformScale = 0.14f;
    const float SpecialAnimationSpriteScale = 0.18f;

    static readonly Dictionary<string, float> FrameScale = new Dictionary<string, float>
    {
        { "Attack/5", 1.2f },
    };
    const byte AlphaThreshold = 20;
    const float FeetBandPercent = 0.03f;

    const float IdleFps = 8f;
    const float DrawFastFrameTime = 0.08f;
    const float DrawSlowFrameTime = 0.12f;
    const int DrawSlowFrameCount = 3;
    const float DrawFinalHoldTime = 0.35f;
    static readonly int[] SheatheFrames = { 2, 1, 0 };
    const float SheatheFrameTime = 0.15f;
    const float ThrowPoseTime = 0.35f;
    static readonly bool UseReloadPose = false;
    const float ReloadPoseTime = 0.1f;
    const float HitPoseTime = 0.3f;
    const float DirectionalAttackPoseTime = 0.35f;
    const float HurtFrameTime = 0.05f;
    const float DamageFrameTime = 0.05f;
    const int HitBlinkCount = 5;

    struct FrameInfo
    {
        public string path;
        public int width, height;
        public int groundY;
        public int topY;
        public int leftX;
        public int rightX;
        public float feetX;
        public int CharHeight => topY - groundY + 1;
        public int CharWidth => rightX - leftX + 1;
    }

    [MenuItem("Tools/Build Player Animations")]
    public static void Build()
    {
        try
        {
            EditorUtility.DisplayProgressBar("Player Animations", "Measuring sprites...", 0f);

            var idle = MeasureFolder("Idle");
            var pull = MeasureFolder("PullWeapon");
            var attack = MeasureFolder("Attack");
            var hurt = MeasureOptional("Hurt1.png");
            var damage = MeasureOptional("Damage.png");
            var faint = MeasureOptional("Faint.png");
            var kickDown = MeasureOptional("Kick Down.png");
            var shooting = MeasureOptional("Shooting.png");
            var jumpAttack = MeasureOptional("Jump Attack.png");
            if (idle.Count == 0 || pull.Count == 0 || attack.Count < 5 || hurt == null || damage == null || faint == null || kickDown == null || shooting == null || jumpAttack == null)
            {
                Debug.LogError("[PlayerAnimationBuilder] Need Idle, PullWeapon, Attack (5+) and Hurt1.png, Damage.png, Faint.png, Kick Down.png, Shooting.png, Jump Attack.png under " + SpriteRoot);
                return;
            }
            var hurtFrame = hurt.Value;
            var damageFrame = damage.Value;
            var faintFrame = faint.Value;
            var kickDownFrame = kickDown.Value;
            var shootingFrame = shooting.Value;
            var jumpAttackFrame = jumpAttack.Value;

            float attackPPU = BasePPU * attack[0].CharHeight / idle[0].CharHeight;
            // Match the displayed height of Attack/5, including its per-frame scale adjustment.
            float attack5PPU = attackPPU / ScaleOf(attack[4]);
            float hurtPPU = PPUForCharacterHeight(hurtFrame, attack[4], attack5PPU);

            EditorUtility.DisplayProgressBar("Player Animations", "Applying import settings...", 0.4f);

            var idlePivot = PivotOf(idle[0]);
            foreach (var f in idle) ApplyImport(f.path, BasePPU, idlePivot);
            foreach (var f in pull) ApplyImport(f.path, BasePPU / ScaleOf(f), PivotOf(f));
            foreach (var f in attack) ApplyImport(f.path, attackPPU / ScaleOf(f), PivotOf(f));
            ApplyImport(hurtFrame.path, hurtPPU, PivotOf(hurtFrame));
            ApplyImport(damageFrame.path, PPUForCharacterHeight(damageFrame, attack[4], attack5PPU), PivotOf(damageFrame));
            ApplyImport(faintFrame.path, PPUForCharacterHeight(faintFrame, attack[4], attack5PPU), PivotOf(faintFrame));
            ApplyImport(kickDownFrame.path, PPUForMatchingBounds(kickDownFrame, hurtFrame, hurtPPU), PivotOf(kickDownFrame));
            ApplyImport(shootingFrame.path, PPUForCharacterHeight(shootingFrame, attack[4], attack5PPU), PivotOf(shootingFrame));
            ApplyImport(jumpAttackFrame.path, PPUForCharacterHeight(jumpAttackFrame, attack[4], attack5PPU), PivotOf(jumpAttackFrame));

            EditorUtility.DisplayProgressBar("Player Animations", "Building clips...", 0.7f);
            EnsureFolder(OutputFolder);

            var idleSprites = idle.Select(f => LoadSprite(f.path)).ToList();
            var pullSprites = pull.Select(f => LoadSprite(f.path)).ToList();
            var atk = attack.Select(f => LoadSprite(f.path)).ToList();
            var hurtSprite = LoadSprite(hurtFrame.path);
            var damageSprite = LoadSprite(damageFrame.path);
            var faintSprite = LoadSprite(faintFrame.path);
            var kickDownSprite = LoadSprite(kickDownFrame.path);
            var shootingSprite = LoadSprite(shootingFrame.path);
            var jumpAttackSprite = LoadSprite(jumpAttackFrame.path);

            var clipIdle = BuildClip("Player_Idle", Evenly(idleSprites, IdleFps), true);
            var clipDraw = BuildClip("Player_Draw", DrawFrames(pullSprites), false);
            var clipReady = BuildClip("Player_Ready", new List<(Sprite, float)> { (atk[3], 0.1f) }, true);
            var clipThrowA = BuildClip("Player_ThrowA", ThrowFrames(atk[1], atk[2]), false);
            var clipThrowB = BuildClip("Player_ThrowB", ThrowFrames(atk[4], atk[2]), false);
            var hurtFrames = new List<(Sprite, float)>();
            for (int i = 0; i < HitBlinkCount; i++)
            {
                hurtFrames.Add((hurtSprite, HurtFrameTime));
                hurtFrames.Add((damageSprite, DamageFrameTime));
            }
            var clipHit = BuildClip("Player_Hit", hurtFrames, false);
            var clipFaint = BuildClip("Player_Faint", new List<(Sprite, float)> { (faintSprite, HitPoseTime) }, false);
            var clipKickDown = BuildClip("Player_KickDown", new List<(Sprite, float)> { (kickDownSprite, DirectionalAttackPoseTime) }, false);
            var clipShooting = BuildClip("Player_Shooting", new List<(Sprite, float)> { (shootingSprite, DirectionalAttackPoseTime) }, false);
            var clipJumpAttack = BuildClip("Player_JumpAttack", new List<(Sprite, float)> { (jumpAttackSprite, DirectionalAttackPoseTime) }, false);
            var sheatheSprites = SheatheFrames.Where(i => i < pullSprites.Count).Select(i => pullSprites[i]).ToList();
            var clipSheathe = BuildClip("Player_Sheathe", Evenly(sheatheSprites, 1f / SheatheFrameTime), false);

            EditorUtility.DisplayProgressBar("Player Animations", "Building Animator Controller...", 0.9f);
            BuildController(clipIdle, clipDraw, clipReady, clipThrowA, clipThrowB, clipHit, clipFaint, clipKickDown, clipShooting, clipJumpAttack, clipSheathe);

            AssetDatabase.SaveAssets();

            Debug.Log($"[PlayerAnimationBuilder] Done -> {ControllerPath}\n" +
                      $"Attack PPU = {attackPPU:F1} (Idle/PullWeapon = {BasePPU})\n" +
                      $"Character height: Idle {WorldCharHeight(idle[0], idleSprites[0]):F2} / Attack(stand) {WorldCharHeight(attack[0], atk[0]):F2} units (should match)\n" +
                      "Next: assign PlayerMain.controller to the Player's Animator");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    static List<FrameInfo> MeasureFolder(string folder)
    {
        string dir = SpriteRoot + "/" + folder;
        if (!Directory.Exists(dir)) return new List<FrameInfo>();

        return Directory.GetFiles(dir, "*.png")
            .Select(p => p.Replace('\\', '/'))
            .OrderBy(p => int.TryParse(Path.GetFileNameWithoutExtension(p), out int n) ? n : int.MaxValue)
            .ThenBy(p => p)
            .Select(Measure)
            .ToList();
    }

    static FrameInfo? MeasureOptional(string fileName)
    {
        string path = SpriteRoot + "/" + fileName;
        return File.Exists(path) ? Measure(path) : (FrameInfo?)null;
    }

    static FrameInfo Measure(string path)
    {
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.LoadImage(File.ReadAllBytes(path));
        var px = tex.GetPixels32();
        int w = tex.width, h = tex.height;
        Object.DestroyImmediate(tex);

        int ground = -1, top = -1;
        int left = w, right = -1;
        for (int i = 0; i < px.Length; i++)
        {
            if (px[i].a <= AlphaThreshold) continue;
            int x = i % w;
            left = Mathf.Min(left, x);
            right = Mathf.Max(right, x);
        }
        for (int y = 0; y < h && ground < 0; y++)
            for (int x = 0; x < w; x++)
                if (px[y * w + x].a > AlphaThreshold) { ground = y; break; }
        for (int y = h - 1; y >= 0 && top < 0; y--)
            for (int x = 0; x < w; x++)
                if (px[y * w + x].a > AlphaThreshold) { top = y; break; }

        if (ground < 0)
            return new FrameInfo { path = path, width = w, height = h, groundY = 0, topY = h - 1, leftX = 0, rightX = w - 1, feetX = w * 0.5f };

        int band = Mathf.Max(1, Mathf.RoundToInt((top - ground) * FeetBandPercent));
        long sumX = 0, count = 0;
        for (int y = ground; y <= ground + band && y < h; y++)
            for (int x = 0; x < w; x++)
                if (px[y * w + x].a > AlphaThreshold) { sumX += x; count++; }

        return new FrameInfo
        {
            path = path, width = w, height = h,
            groundY = ground, topY = top,
            leftX = left, rightX = right,
            feetX = count > 0 ? (float)sumX / count : w * 0.5f
        };
    }

    static float ScaleOf(FrameInfo f)
    {
        string key = Path.GetFileName(Path.GetDirectoryName(f.path)) + "/" + Path.GetFileNameWithoutExtension(f.path);
        return FrameScale.TryGetValue(key, out float s) && s > 0f ? s : 1f;
    }

    static float PPUForCharacterHeight(FrameInfo frame, FrameInfo reference, float referencePPU, float targetScale = SpecialAnimationSpriteScale) =>
        referencePPU * frame.CharHeight / reference.CharHeight * PlayerTransformScale / targetScale;

    static float PPUForMatchingBounds(FrameInfo frame, FrameInfo reference, float referencePPU)
    {
        float heightRatio = (float)frame.CharHeight / reference.CharHeight;
        float widthRatio = (float)frame.CharWidth / reference.CharWidth;
        // Match visible sprite area so wide poses stay readable without looking oversized.
        return referencePPU * Mathf.Sqrt(heightRatio * widthRatio);
    }

    static Vector2 PivotOf(FrameInfo f) => new Vector2(f.feetX / f.width, (float)f.groundY / f.height);

    static float WorldCharHeight(FrameInfo f, Sprite s) =>
        s == null ? 0f : s.bounds.size.y * f.CharHeight / f.height;

    static void ApplyImport(string path, float ppu, Vector2 pivot)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer == null) return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMode = (int)SpriteImportMode.Single;
        settings.spritePixelsPerUnit = ppu;
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = pivot;
        settings.alphaIsTransparency = true;
        settings.mipmapEnabled = false;
        importer.SetTextureSettings(settings);

        importer.maxTextureSize = 2048;
        importer.SaveAndReimport();
    }

    static Sprite LoadSprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

    static List<(Sprite, float)> DrawFrames(List<Sprite> sprites)
    {
        var frames = new List<(Sprite, float)>();
        int last = sprites.Count - 1;
        for (int i = 0; i < sprites.Count; i++)
        {
            float t = i == last ? DrawFinalHoldTime
                    : i >= last - DrawSlowFrameCount ? DrawSlowFrameTime
                    : DrawFastFrameTime;
            frames.Add((sprites[i], t));
        }
        return frames;
    }

    static List<(Sprite, float)> ThrowFrames(Sprite throwPose, Sprite reloadPose)
    {
        var frames = new List<(Sprite, float)> { (throwPose, ThrowPoseTime) };
        if (UseReloadPose) frames.Add((reloadPose, ReloadPoseTime));
        return frames;
    }

    static List<(Sprite, float)> Evenly(List<Sprite> sprites, float fps) =>
        sprites.Select(s => (s, 1f / fps)).ToList();

    static AnimationClip BuildClip(string name, List<(Sprite sprite, float duration)> frames, bool loop)
    {
        string path = $"{OutputFolder}/{name}.anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, path);
        }
        ClearAllCurves(clip);
        clip.frameRate = 60f;

        var keys = new List<ObjectReferenceKeyframe>();
        float t = 0f;
        foreach (var (sprite, duration) in frames)
        {
            keys.Add(new ObjectReferenceKeyframe { time = t, value = sprite });
            t += duration;
        }
        keys.Add(new ObjectReferenceKeyframe { time = t, value = frames[frames.Count - 1].sprite });

        var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys.ToArray());

        var clipSettings = AnimationUtility.GetAnimationClipSettings(clip);
        clipSettings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, clipSettings);

        EditorUtility.SetDirty(clip);
        return clip;
    }

    static void ClearAllCurves(AnimationClip clip)
    {
        foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            AnimationUtility.SetEditorCurve(clip, binding, null);
        foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
            AnimationUtility.SetObjectReferenceCurve(clip, binding, null);
        AnimationUtility.SetAnimationEvents(clip, new AnimationEvent[0]);
    }

    static void BuildController(AnimationClip idle, AnimationClip draw, AnimationClip ready,
                                AnimationClip throwA, AnimationClip throwB, AnimationClip hit,
                                AnimationClip faint, AnimationClip kickDown, AnimationClip shooting,
                                AnimationClip jumpAttack, AnimationClip sheathe)
    {
        var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (ctrl == null) ctrl = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

        var sm = ctrl.layers[0].stateMachine;
        foreach (var child in sm.states.ToArray()) sm.RemoveState(child.state);

        var sIdle = AddState(sm, "Player_Idle", idle, new Vector3(300, 0));
        var sDraw = AddState(sm, "Player_Draw", draw, new Vector3(300, 100));
        var sReady = AddState(sm, "Player_Ready", ready, new Vector3(300, 200));
        var sThrowA = AddState(sm, "Player_ThrowA", throwA, new Vector3(550, 150));
        var sThrowB = AddState(sm, "Player_ThrowB", throwB, new Vector3(550, 250));
        var sHit = AddState(sm, "Player_Hit", hit, new Vector3(550, 350));
        AddState(sm, "Player_Faint", faint, new Vector3(550, 450));
        var sKickDown = AddState(sm, "Player_KickDown", kickDown, new Vector3(800, 150));
        var sShooting = AddState(sm, "Player_Shooting", shooting, new Vector3(800, 250));
        var sJumpAttack = AddState(sm, "Player_JumpAttack", jumpAttack, new Vector3(800, 350));
        var sSheathe = AddState(sm, "Player_Sheathe", sheathe, new Vector3(50, 100));
        sm.defaultState = sIdle;

        AutoNext(sDraw, sReady);
        AutoNext(sThrowA, sReady);
        AutoNext(sThrowB, sReady);
        AutoNext(sHit, sReady);
        AutoNext(sKickDown, sReady);
        AutoNext(sShooting, sReady);
        AutoNext(sJumpAttack, sReady);
        AutoNext(sSheathe, sIdle);

        EditorUtility.SetDirty(ctrl);
    }

    static AnimatorState AddState(AnimatorStateMachine sm, string name, Motion clip, Vector3 pos)
    {
        var state = sm.AddState(name, pos);
        state.motion = clip;
        state.writeDefaultValues = false;
        return state;
    }

    static void AutoNext(AnimatorState from, AnimatorState to)
    {
        var t = from.AddTransition(to);
        t.hasExitTime = true;
        t.exitTime = 1f;
        t.duration = 0f;
        t.hasFixedDuration = true;
    }

    static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;
        string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
    }
}
