using UnityEditor;
using UnityEngine;
using UnityEditor.Animations;

public class AnimatorAutoBuilder
{
    [MenuItem("Tools/Build Animator/BattleCharacterController (Full Fixed)")]
    public static void BuildAnimator()
    {
        string path = "Assets/BattleCharacterController.controller";

        if ( AssetDatabase.LoadAssetAtPath<AnimatorController>(path) != null )
            AssetDatabase.DeleteAsset(path);

        var controller = AnimatorController.CreateAnimatorControllerAtPath(path);

        // ================================
        // Step 2: パラメータ設定
        // ================================
        string[] bools = {
            "isGrounded","isMoving","isAttacking","isGuarding","isBlocking",
            "airAttack","isBoosting","isDodging","isJumping","inAir",
            "isStunned","isInvincible","energyDepleted","isAiming","isReloading",
            "criticalHit","blockSuccess","guardBroken"
        };
        foreach ( var b in bools )
            controller.AddParameter(b, AnimatorControllerParameterType.Bool);

        string[] floats = { "moveSpeed", "moveX", "moveY", "verticalVelocity", "stunGauge", "aimAccuracy", "weaponPower", "damageIntensity" };
        foreach ( var f in floats )
            controller.AddParameter(f, AnimatorControllerParameterType.Float);

        string[] ints = { "combatMode", "attackType", "attackDirection", "comboCount", "guardDirection", "dodgeDirection", "maneuverType", "hitDirection" };
        foreach ( var i in ints )
            controller.AddParameter(i, AnimatorControllerParameterType.Int);

        string[] triggers = { "attackTrigger", "blockTrigger", "dodgeTrigger", "quickTurnTrigger", "maneuverTrigger", "hitTrigger" };
        foreach ( var t in triggers )
            controller.AddParameter(t, AnimatorControllerParameterType.Trigger);

        // ================================
        // Step 3: Base Layer
        // ================================
        var baseSM = controller.layers[0].stateMachine;
        var idle = baseSM.AddState("Idle");
        baseSM.defaultState = idle;

        var walking = baseSM.AddState("Walking");
        var running = baseSM.AddState("Running");
        var jumpStart = baseSM.AddState("Jump_Start");
        var jumpLoop = baseSM.AddState("Jump_Loop");
        var jumpLand = baseSM.AddState("Jump_Land");
        var boostStart = baseSM.AddState("Boost_Start");
        var boostLoop = baseSM.AddState("Boost_Loop");
        var boostEnd = baseSM.AddState("Boost_End");

        // トランジション
        AddTransition(idle, walking, ("isMoving", true), ("isBoosting", false));
        AddTransition(walking, idle, ("isMoving", false));
        AddTransition(walking, running, ("isMoving", true), ("moveSpeed", 0.8f, true));
        AddAnyTransition(baseSM, jumpStart, ("isGrounded", false), ("isJumping", true));
        jumpStart.AddTransition(jumpLoop).hasExitTime = true;
        jumpLoop.AddTransition(jumpLand).AddCondition(AnimatorConditionMode.If, 0, "isGrounded");
        AddAnyTransition(baseSM, boostStart, ("isBoosting", true));
        boostStart.AddTransition(boostLoop).hasExitTime = true;
        boostLoop.AddTransition(boostEnd).AddCondition(AnimatorConditionMode.IfNot, 0, "isBoosting");
        boostEnd.AddTransition(idle).hasExitTime = true;

        // Walkingにブレンドツリー
        var blendTree = new BlendTree { name = "WalkBlend", blendType = BlendTreeType.FreeformCartesian2D, blendParameter = "moveX", blendParameterY = "moveY" };
        walking.motion = blendTree;
        AssetDatabase.AddObjectToAsset(blendTree, controller);
        for ( int i = 0; i < 9; i++ )
            blendTree.AddChild(new AnimationClip(), new Vector2(i % 3 - 1, i / 3 - 1)); // ダミー配置

        // ================================
        // Step 5: Combat Layer
        // ================================
        var combatLayer = MakeLayer(controller, "Combat Layer", AnimatorLayerBlendingMode.Additive);
        var meleeSM = combatLayer.stateMachine.AddStateMachine("Melee Combat");
        meleeSM.defaultState = meleeSM.AddState("Melee_Idle");
        meleeSM.AddState("Melee_Guard");
        meleeSM.AddState("Attack_Weak_Up");
        meleeSM.AddState("Attack_Weak_Left");
        meleeSM.AddState("Attack_Weak_Right");

        var rangedSM = combatLayer.stateMachine.AddStateMachine("Ranged Combat");
        rangedSM.defaultState = rangedSM.AddState("Ranged_Idle");

        var barrierSM = combatLayer.stateMachine.AddStateMachine("Energy Barrier");
        barrierSM.defaultState = barrierSM.AddState("Barrier_Idle");

        AddAnyTransition(combatLayer.stateMachine, meleeSM.defaultState, ("combatMode", 0));
        AddAnyTransition(combatLayer.stateMachine, rangedSM.defaultState, ("combatMode", 1));
        AddAnyTransition(combatLayer.stateMachine, barrierSM.defaultState, ("combatMode", 2));

        // ================================
        // Step 6: Special Layer
        // ================================
        var specialLayer = MakeLayer(controller, "Special Layer", AnimatorLayerBlendingMode.Additive);
        var sNone = specialLayer.stateMachine.AddState("Special_None");
        specialLayer.stateMachine.defaultState = sNone;
        var dBack = specialLayer.stateMachine.AddState("Dodge_Back");
        var dLeft = specialLayer.stateMachine.AddState("Dodge_Left");

        AddTransition(sNone, dBack, ("dodgeTrigger", true), ("dodgeDirection", 0));
        AddTransition(sNone, dLeft, ("dodgeTrigger", true), ("dodgeDirection", 1));

        // ================================
        // Step 7: Additive Layer
        // ================================
        var additiveLayer = MakeLayer(controller, "Additive Layer", AnimatorLayerBlendingMode.Additive);
        var noReact = additiveLayer.stateMachine.AddState("No_Reaction");
        additiveLayer.stateMachine.defaultState = noReact;
        var hitL = additiveLayer.stateMachine.AddState("Hit_Light");

        AddTransition(noReact, hitL, ("hitTrigger", true), ("damageIntensity", 0.3f, false));

        AssetDatabase.SaveAssets();
        Debug.Log("✅ BattleCharacterController.controller を生成しました: " + path);
    }

    // ================================
    // ヘルパー
    // ================================
    private static AnimatorControllerLayer MakeLayer(AnimatorController ctrl, string name, AnimatorLayerBlendingMode mode)
    {
        var l = new AnimatorControllerLayer
        {
            name = name,
            defaultWeight = 1f,
            blendingMode = mode,
            stateMachine = new AnimatorStateMachine()
        };
        ctrl.AddLayer(l);
        return l;
    }

    private static void AddTransition(AnimatorState from, AnimatorState to, (string, bool) value, params (string, object, bool)[] conditions)
    {
        var t = from.AddTransition(to);
        t.hasExitTime = false;
        foreach ( var c in conditions )
            AddCondition(t, c);
    }

    private static void AddAnyTransition(AnimatorStateMachine sm, AnimatorState target, params (string, object, bool)[] conditions)
    {
        var t = sm.AddAnyStateTransition(target);
        t.hasExitTime = false;
        foreach ( var c in conditions )
            AddCondition(t, c);
    }

    private static void AddCondition(AnimatorStateTransition t, (string, object, bool) cond)
    {
        string param = cond.Item1;
        object value = cond.Item2;
        bool greater = cond.Item3;

        if ( value is bool b )
            t.AddCondition(b ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, param);
        else if ( value is float f )
            t.AddCondition(greater ? AnimatorConditionMode.Greater : AnimatorConditionMode.Less, f, param);
        else if ( value is int i )
            t.AddCondition(AnimatorConditionMode.Equals, i, param);
    }

    // オーバーロード（greater不要の省略版）
    private static void AddTransition(AnimatorState from, AnimatorState to, params (string, object)[] conditions)
    {
        var t = from.AddTransition(to);
        t.hasExitTime = false;
        foreach ( var c in conditions )
            AddCondition(t, (c.Item1, c.Item2, false));
    }

    private static void AddAnyTransition(AnimatorStateMachine sm, AnimatorState target, params (string, object)[] conditions)
    {
        var t = sm.AddAnyStateTransition(target);
        t.hasExitTime = false;
        foreach ( var c in conditions )
            AddCondition(t, (c.Item1, c.Item2, false));
    }
}
