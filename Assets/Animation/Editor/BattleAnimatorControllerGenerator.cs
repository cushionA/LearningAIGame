//using UnityEngine;
//using UnityEditor;
//using UnityEditor.Animations;
//using System.Collections.Generic;
//using System.IO;
//using BattleSystem.UI;
//using LearningAIGame.CombatSystem;

///// <summary>
///// バトルシステム用アニメーターコントローラーを自動生成するエディタスクリプト
///// キャラクターコントローラー仕様書に基づいた完全なアニメーター構造を作成
///// </summary>
//public class BattleAnimatorControllerGenerator : EditorWindow
//{
//    [Header("生成設定")]
//    [SerializeField] private string controllerName = "BattleCharacterController";
//    [SerializeField] private string savePath = "Assets/Animators/";
//    [SerializeField] private bool createSubStateMachines = true;
//    [SerializeField] private bool addTransitionConditions = true;
//    [SerializeField] private bool createBlendTrees = true;

//    [Header("アニメーション設定")]
//    [SerializeField] private bool useRootMotion = false;
//    [SerializeField] private float defaultTransitionDuration = 0.1f;
//    [SerializeField] private float combatTransitionDuration = 0.05f;

//    private AnimatorController controller;
//    private AnimatorStateMachine rootStateMachine;

//    [MenuItem("Tools/Battle System/Generate Animator Controller")]
//    public static void ShowWindow()
//    {
//        GetWindow<BattleAnimatorControllerGenerator>("Battle Animator Generator");
//    }

//    private void OnGUI()
//    {
//        GUILayout.Label("Battle Animator Controller Generator", EditorStyles.boldLabel);
//        EditorGUILayout.Space();

//        controllerName = EditorGUILayout.TextField("Controller Name", controllerName);
//        savePath = EditorGUILayout.TextField("Save Path", savePath);

//        EditorGUILayout.Space();
//        GUILayout.Label("Generation Options", EditorStyles.boldLabel);
//        createSubStateMachines = EditorGUILayout.Toggle("Create Sub State Machines", createSubStateMachines);
//        addTransitionConditions = EditorGUILayout.Toggle("Add Transition Conditions", addTransitionConditions);
//        createBlendTrees = EditorGUILayout.Toggle("Create Blend Trees", createBlendTrees);

//        EditorGUILayout.Space();
//        GUILayout.Label("Animation Settings", EditorStyles.boldLabel);
//        useRootMotion = EditorGUILayout.Toggle("Use Root Motion", useRootMotion);
//        defaultTransitionDuration = EditorGUILayout.FloatField("Default Transition Duration", defaultTransitionDuration);
//        combatTransitionDuration = EditorGUILayout.FloatField("Combat Transition Duration", combatTransitionDuration);

//        EditorGUILayout.Space();

//        if ( GUILayout.Button("Generate Animator Controller", GUILayout.Height(30)) )
//        {
//            GenerateAnimatorController();
//        }

//        EditorGUILayout.Space();

//        if ( GUILayout.Button("Generate Test Animations", GUILayout.Height(25)) )
//        {
//            GenerateTestAnimations();
//        }
//    }

//    private void GenerateAnimatorController()
//    {
//        // ディレクトリ作成
//        if ( !Directory.Exists(savePath) )
//        {
//            Directory.CreateDirectory(savePath);
//        }

//        // アニメーターコントローラー作成
//        string fullPath = Path.Combine(savePath, controllerName + ".controller");
//        controller = AnimatorController.CreateAnimatorControllerAtPath(fullPath);
//        rootStateMachine = controller.layers[0].stateMachine;

//        // パラメーター追加
//        CreateParameters();

//        // ステート作成
//        if ( createSubStateMachines )
//        {
//            CreateSubStateMachines();
//        }
//        else
//        {
//            CreateFlatStates();
//        }

//        // トランジション作成
//        if ( addTransitionConditions )
//        {
//            CreateTransitions();
//        }

//        // アセットの保存
//        AssetDatabase.SaveAssets();
//        AssetDatabase.Refresh();

//        Debug.Log($"Animator Controller generated at: {fullPath}");
//        EditorUtility.FocusProjectWindow();
//        Selection.activeObject = controller;
//    }

//    private void CreateParameters()
//    {
//        // === Core State Parameters ===
//        controller.AddParameter("ActionMode", AnimatorControllerParameterType.Int);
//        controller.AddParameter("ActionState", AnimatorControllerParameterType.Int);
//        controller.AddParameter("AttackDirection", AnimatorControllerParameterType.Int);

//        // === Movement Parameters ===
//        controller.AddParameter("MoveX", AnimatorControllerParameterType.Float);
//        controller.AddParameter("MoveZ", AnimatorControllerParameterType.Float);
//        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
//        controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
//        controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
//        controller.AddParameter("IsBoosting", AnimatorControllerParameterType.Bool);

//        // === Combat Parameters ===
//        controller.AddParameter("IsAttacking", AnimatorControllerParameterType.Bool);
//        controller.AddParameter("AttackType", AnimatorControllerParameterType.Int);
//        controller.AddParameter("ComboCount", AnimatorControllerParameterType.Int);
//        controller.AddParameter("IsGuarding", AnimatorControllerParameterType.Bool);
//        controller.AddParameter("IsBlocking", AnimatorControllerParameterType.Bool);

//        // === Aerial Parameters ===
//        controller.AddParameter("IsInAir", AnimatorControllerParameterType.Bool);
//        controller.AddParameter("VerticalVelocity", AnimatorControllerParameterType.Float);
//        controller.AddParameter("IsJumping", AnimatorControllerParameterType.Bool);
//        controller.AddParameter("IsFalling", AnimatorControllerParameterType.Bool);
//        controller.AddParameter("AirAttackCount", AnimatorControllerParameterType.Int);

//        // === System Parameters ===
//        controller.AddParameter("EnergyPercentage", AnimatorControllerParameterType.Float);
//        controller.AddParameter("HealthPercentage", AnimatorControllerParameterType.Float);
//        controller.AddParameter("IsStunned", AnimatorControllerParameterType.Bool);
//        controller.AddParameter("IsInvincible", AnimatorControllerParameterType.Bool);
//        controller.AddParameter("IsEnergyDepleted", AnimatorControllerParameterType.Bool);

//        // === Action Triggers ===
//        controller.AddParameter("WeakAttack", AnimatorControllerParameterType.Trigger);
//        controller.AddParameter("StrongAttack", AnimatorControllerParameterType.Trigger);
//        controller.AddParameter("SkillAttack", AnimatorControllerParameterType.Trigger);
//        controller.AddParameter("Dodge", AnimatorControllerParameterType.Trigger);
//        controller.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
//        controller.AddParameter("Block", AnimatorControllerParameterType.Trigger);
//        controller.AddParameter("Maneuver", AnimatorControllerParameterType.Trigger);

//        // === Mode Parameters ===
//        controller.AddParameter("MeleeMode", AnimatorControllerParameterType.Bool);
//        controller.AddParameter("RangedMode", AnimatorControllerParameterType.Bool);
//        controller.AddParameter("EnergyBarrierMode", AnimatorControllerParameterType.Bool);

//        // === Special Parameters ===
//        controller.AddParameter("DamageDirection", AnimatorControllerParameterType.Int);
//        controller.AddParameter("HitReaction", AnimatorControllerParameterType.Trigger);
//        controller.AddParameter("DeathTrigger", AnimatorControllerParameterType.Trigger);
//        controller.AddParameter("ReviveTrigger", AnimatorControllerParameterType.Trigger);

//        Debug.Log("Parameters created successfully");
//    }

//    private void CreateSubStateMachines()
//    {
//        // === Main State Machines ===
//        var locomotionSM = rootStateMachine.AddStateMachine("Locomotion", new Vector3(200, 0, 0));
//        var combatSM = rootStateMachine.AddStateMachine("Combat", new Vector3(400, 0, 0));
//        var aerialSM = rootStateMachine.AddStateMachine("Aerial", new Vector3(600, 0, 0));
//        var specialSM = rootStateMachine.AddStateMachine("Special", new Vector3(800, 0, 0));

//        // === Locomotion Sub-States ===
//        CreateLocomotionStates(locomotionSM);

//        // === Combat Sub-States ===
//        CreateCombatStates(combatSM);

//        // === Aerial Sub-States ===
//        CreateAerialStates(aerialSM);

//        // === Special Sub-States ===
//        CreateSpecialStates(specialSM);

//        Debug.Log("Sub-state machines created successfully");
//    }

//    private void CreateLocomotionStates(AnimatorStateMachine locomotionSM)
//    {
//        // Base locomotion states
//        var idleState = locomotionSM.AddState("Idle", new Vector3(0, 0, 0));

//        if ( createBlendTrees )
//        {
//            // Movement Blend Tree
//            var moveBlendTree = new BlendTree();
//            moveBlendTree.name = "Movement";
//            moveBlendTree.blendType = BlendTreeType.FreeformDirectional2D;
//            moveBlendTree.blendParameter = "MoveX";
//            moveBlendTree.blendParameterY = "MoveZ";

//            var moveState = locomotionSM.AddState("Movement", new Vector3(200, 0, 0));
//            moveState.motion = moveBlendTree;

//            // Boost Blend Tree
//            var boostBlendTree = new BlendTree();
//            boostBlendTree.name = "Boost";
//            boostBlendTree.blendType = BlendTreeType.FreeformDirectional2D;
//            boostBlendTree.blendParameter = "MoveX";
//            boostBlendTree.blendParameterY = "MoveZ";

//            var boostState = locomotionSM.AddState("Boost", new Vector3(400, 0, 0));
//            boostState.motion = boostBlendTree;
//        }
//        else
//        {
//            // Simple movement states
//            locomotionSM.AddState("Walk_Forward", new Vector3(200, 0, 0));
//            locomotionSM.AddState("Walk_Back", new Vector3(200, 100, 0));
//            locomotionSM.AddState("Walk_Left", new Vector3(100, 50, 0));
//            locomotionSM.AddState("Walk_Right", new Vector3(300, 50, 0));

//            locomotionSM.AddState("Boost_Forward", new Vector3(400, 0, 0));
//            locomotionSM.AddState("Boost_Back", new Vector3(400, 100, 0));
//            locomotionSM.AddState("Boost_Left", new Vector3(300, 150, 0));
//            locomotionSM.AddState("Boost_Right", new Vector3(500, 150, 0));
//        }

//        // Guard states
//        var guardState = locomotionSM.AddState("Guard", new Vector3(0, 200, 0));
//        var guardWalkState = locomotionSM.AddState("Guard_Walk", new Vector3(200, 200, 0));

//        // Dodge states
//        locomotionSM.AddState("Dodge_Forward", new Vector3(600, 0, 0));
//        locomotionSM.AddState("Dodge_Back", new Vector3(600, 100, 0));
//        locomotionSM.AddState("Dodge_Left", new Vector3(500, 50, 0));
//        locomotionSM.AddState("Dodge_Right", new Vector3(700, 50, 0));
//        locomotionSM.AddState("Dodge_Backstep", new Vector3(600, 200, 0));

//        // Set default state
//        locomotionSM.defaultState = idleState;
//    }

//    private void CreateCombatStates(AnimatorStateMachine combatSM)
//    {
//        // === Melee Attack States ===
//        var meleeAttackSM = combatSM.AddStateMachine("Melee_Attacks", new Vector3(0, 0, 0));

//        // Weak attacks by direction
//        meleeAttackSM.AddState("Weak_Attack_Up", new Vector3(0, 0, 0));
//        meleeAttackSM.AddState("Weak_Attack_Left", new Vector3(150, 0, 0));
//        meleeAttackSM.AddState("Weak_Attack_Right", new Vector3(300, 0, 0));

//        // Strong attacks by direction
//        meleeAttackSM.AddState("Strong_Attack_Up", new Vector3(0, 150, 0));
//        meleeAttackSM.AddState("Strong_Attack_Left", new Vector3(150, 150, 0));
//        meleeAttackSM.AddState("Strong_Attack_Right", new Vector3(300, 150, 0));

//        // Combo states
//        meleeAttackSM.AddState("Combo_2nd", new Vector3(450, 0, 0));
//        meleeAttackSM.AddState("Combo_3rd", new Vector3(600, 0, 0));
//        meleeAttackSM.AddState("Combo_4th", new Vector3(750, 0, 0));
//        meleeAttackSM.AddState("Combo_Finisher", new Vector3(900, 0, 0));

//        // === Ranged Attack States ===
//        var rangedAttackSM = combatSM.AddStateMachine("Ranged_Attacks", new Vector3(300, 0, 0));

//        // Weak ranged by direction
//        rangedAttackSM.AddState("Weak_Shot_Up", new Vector3(0, 0, 0));
//        rangedAttackSM.AddState("Weak_Shot_Left", new Vector3(150, 0, 0));
//        rangedAttackSM.AddState("Weak_Shot_Right", new Vector3(300, 0, 0));

//        // Strong ranged by direction
//        rangedAttackSM.AddState("Strong_Shot_Up", new Vector3(0, 150, 0));
//        rangedAttackSM.AddState("Strong_Shot_Left", new Vector3(150, 150, 0));
//        rangedAttackSM.AddState("Strong_Shot_Right", new Vector3(300, 150, 0));

//        // Skill states
//        rangedAttackSM.AddState("Missile_Launch", new Vector3(450, 0, 0));
//        rangedAttackSM.AddState("Reload", new Vector3(450, 150, 0));

//        // === Defense States ===
//        var defenseSM = combatSM.AddStateMachine("Defense", new Vector3(600, 0, 0));

//        // Blocking by direction
//        defenseSM.AddState("Block_Up", new Vector3(0, 0, 0));
//        defenseSM.AddState("Block_Left", new Vector3(150, 0, 0));
//        defenseSM.AddState("Block_Right", new Vector3(300, 0, 0));

//        // Block success/failure
//        defenseSM.AddState("Block_Success", new Vector3(0, 150, 0));
//        defenseSM.AddState("Block_Failure", new Vector3(150, 150, 0));

//        // Guard break states
//        defenseSM.AddState("Guard_Break", new Vector3(300, 150, 0));
//        defenseSM.AddState("Guard_Stagger", new Vector3(450, 150, 0));
//    }

//    private void CreateAerialStates(AnimatorStateMachine aerialSM)
//    {
//        // Basic aerial states
//        aerialSM.AddState("Jump_Start", new Vector3(0, 0, 0));
//        aerialSM.AddState("Jump_Peak", new Vector3(200, 0, 0));
//        aerialSM.AddState("Falling", new Vector3(400, 0, 0));
//        aerialSM.AddState("Land", new Vector3(600, 0, 0));
//        aerialSM.AddState("Hard_Land", new Vector3(600, 100, 0));

//        // Double jump
//        aerialSM.AddState("Double_Jump", new Vector3(200, 150, 0));
//        aerialSM.AddState("Air_Charge", new Vector3(400, 150, 0));

//        // Air attacks by direction
//        aerialSM.AddState("Air_Attack_Up", new Vector3(0, 300, 0));
//        aerialSM.AddState("Air_Attack_Left", new Vector3(150, 300, 0));
//        aerialSM.AddState("Air_Attack_Right", new Vector3(300, 300, 0));
//        aerialSM.AddState("Air_Attack_Down", new Vector3(150, 450, 0));

//        // Air combo states
//        aerialSM.AddState("Air_Combo_2nd", new Vector3(450, 300, 0));
//        aerialSM.AddState("Air_Combo_3rd", new Vector3(600, 300, 0));
//        aerialSM.AddState("Air_Combo_Finisher", new Vector3(750, 300, 0));

//        // Air dodge
//        aerialSM.AddState("Air_Dodge", new Vector3(800, 0, 0));
//        aerialSM.AddState("Air_Boost", new Vector3(800, 150, 0));

//        // Landing attacks
//        aerialSM.AddState("Landing_Attack", new Vector3(600, 450, 0));

//        // Set default state
//        var jumpStart = aerialSM.states[0].state;
//        aerialSM.defaultState = jumpStart;
//    }

//    private void CreateSpecialStates(AnimatorStateMachine specialSM)
//    {
//        // Hit reactions
//        specialSM.AddState("Hit_Light", new Vector3(0, 0, 0));
//        specialSM.AddState("Hit_Heavy", new Vector3(200, 0, 0));
//        specialSM.AddState("Hit_Knockdown", new Vector3(400, 0, 0));

//        // Stun states
//        specialSM.AddState("Stunned", new Vector3(0, 150, 0));
//        specialSM.AddState("Stun_Recovery", new Vector3(200, 150, 0));

//        // Energy states
//        specialSM.AddState("Energy_Depleted", new Vector3(400, 150, 0));
//        specialSM.AddState("Energy_Barrier", new Vector3(600, 150, 0));
//        specialSM.AddState("Energy_Recovery", new Vector3(800, 150, 0));

//        // Maneuver states
//        specialSM.AddState("Maneuver_Execute", new Vector3(0, 300, 0));
//        specialSM.AddState("Quick_Turn", new Vector3(200, 300, 0));

//        // Mode transitions
//        specialSM.AddState("Mode_Switch_Melee", new Vector3(400, 300, 0));
//        specialSM.AddState("Mode_Switch_Ranged", new Vector3(600, 300, 0));

//        // Death and revival
//        specialSM.AddState("Death", new Vector3(800, 300, 0));
//        specialSM.AddState("Revival", new Vector3(1000, 300, 0));

//        // Invincibility
//        specialSM.AddState("Invincible", new Vector3(1000, 150, 0));

//        // Skill cooldown
//        specialSM.AddState("Skill_Cooldown", new Vector3(1000, 0, 0));
//    }

//    private void CreateFlatStates()
//    {
//        // Basic states in flat structure
//        var idleState = rootStateMachine.AddState("Idle", new Vector3(300, 100, 0));
//        rootStateMachine.AddState("Walk", new Vector3(500, 100, 0));
//        rootStateMachine.AddState("Run", new Vector3(700, 100, 0));
//        rootStateMachine.AddState("Jump", new Vector3(300, 300, 0));
//        rootStateMachine.AddState("Fall", new Vector3(500, 300, 0));
//        rootStateMachine.AddState("Attack", new Vector3(700, 300, 0));
//        rootStateMachine.AddState("Guard", new Vector3(300, 500, 0));
//        rootStateMachine.AddState("Dodge", new Vector3(500, 500, 0));
//        rootStateMachine.AddState("Hit", new Vector3(700, 500, 0));

//        rootStateMachine.defaultState = idleState;
//    }

//    private void CreateTransitions()
//    {
//        if ( !createSubStateMachines )
//            return;

//        // Get state machines
//        var locomotionSM = GetStateMachine("Locomotion");
//        var combatSM = GetStateMachine("Combat");
//        var aerialSM = GetStateMachine("Aerial");
//        var specialSM = GetStateMachine("Special");

//        if ( locomotionSM == null || combatSM == null || aerialSM == null || specialSM == null )
//        {
//            Debug.LogWarning("Could not find all state machines for transition creation");
//            return;
//        }

//        // === Main State Machine Transitions ===
//        CreateMainTransitions(locomotionSM, combatSM, aerialSM, specialSM);

//        // === Internal State Machine Transitions ===
//        CreateLocomotionTransitions(locomotionSM);
//        CreateCombatTransitions(combatSM);
//        CreateAerialTransitions(aerialSM);
//        CreateSpecialTransitions(specialSM);

//        Debug.Log("Transitions created successfully");
//    }

//    private void CreateMainTransitions(AnimatorStateMachine locomotionSM, AnimatorStateMachine combatSM,
//                                     AnimatorStateMachine aerialSM, AnimatorStateMachine specialSM)
//    {
//        // Locomotion to Combat
//        var locToCombat = rootStateMachine.AddAnyStateTransition(combatSM);
//        locToCombat.AddCondition(AnimatorConditionMode.If, 0, "IsAttacking");
//        locToCombat.duration = combatTransitionDuration;

//        // Combat to Locomotion
//        var combatToLoc = rootStateMachine.AddAnyStateTransition(locomotionSM);
//        combatToLoc.AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");
//        combatToLoc.duration = combatTransitionDuration;

//        // To Aerial
//        var toAerial = rootStateMachine.AddAnyStateTransition(aerialSM);
//        toAerial.AddCondition(AnimatorConditionMode.If, 0, "IsInAir");
//        toAerial.duration = defaultTransitionDuration;

//        // From Aerial
//        var fromAerial = rootStateMachine.AddAnyStateTransition(locomotionSM);
//        fromAerial.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
//        fromAerial.AddCondition(AnimatorConditionMode.IfNot, 0, "IsInAir");
//        fromAerial.duration = defaultTransitionDuration;

//        // To Special (high priority)
//        var toSpecial = rootStateMachine.AddAnyStateTransition(specialSM);
//        toSpecial.AddCondition(AnimatorConditionMode.If, 0, "IsStunned");
//        toSpecial.duration = 0f;
//        toSpecial.hasExitTime = false;

//        // Energy depletion
//        var toEnergySpecial = rootStateMachine.AddAnyStateTransition(specialSM);
//        toEnergySpecial.AddCondition(AnimatorConditionMode.If, 0, "IsEnergyDepleted");
//        toEnergySpecial.duration = defaultTransitionDuration;
//    }

//    private void CreateLocomotionTransitions(AnimatorStateMachine locomotionSM)
//    {
//        var states = locomotionSM.states;
//        if ( states.Length == 0 )
//            return;

//        var idleState = states[0].state; // Assuming first state is idle

//        // Create any state transitions for common actions
//        var dodgeTransition = locomotionSM.AddAnyStateTransition(GetStateByName(locomotionSM, "Dodge_"));
//        if ( dodgeTransition != null )
//        {
//            dodgeTransition.AddCondition(AnimatorConditionMode.If, 0, "Dodge");
//            dodgeTransition.duration = 0f;
//            dodgeTransition.hasExitTime = false;
//        }

//        // Guard transitions
//        var guardTransition = locomotionSM.AddAnyStateTransition(GetStateByName(locomotionSM, "Guard"));
//        if ( guardTransition != null )
//        {
//            guardTransition.AddCondition(AnimatorConditionMode.If, 0, "IsGuarding");
//            guardTransition.duration = 0.1f;
//        }
//    }

//    private void CreateCombatTransitions(AnimatorStateMachine combatSM)
//    {
//        // Attack direction transitions
//        var meleeAttackSM = GetSubStateMachine(combatSM, "Melee_Attacks");
//        var rangedAttackSM = GetSubStateMachine(combatSM, "Ranged_Attacks");

//        if ( meleeAttackSM != null )
//        {
//            // Weak attack transitions
//            var weakAttackTransition = meleeAttackSM.AddAnyStateTransition(GetStateByName(meleeAttackSM, "Weak_Attack_"));
//            if ( weakAttackTransition != null )
//            {
//                weakAttackTransition.AddCondition(AnimatorConditionMode.If, 0, "WeakAttack");
//                weakAttackTransition.duration = 0f;
//                weakAttackTransition.hasExitTime = false;
//            }

//            // Strong attack transitions
//            var strongAttackTransition = meleeAttackSM.AddAnyStateTransition(GetStateByName(meleeAttackSM, "Strong_Attack_"));
//            if ( strongAttackTransition != null )
//            {
//                strongAttackTransition.AddCondition(AnimatorConditionMode.If, 0, "StrongAttack");
//                strongAttackTransition.duration = 0f;
//                strongAttackTransition.hasExitTime = false;
//            }
//        }

//        // Mode transitions
//        if ( rangedAttackSM != null )
//        {
//            var toRanged = combatSM.AddAnyStateTransition(rangedAttackSM);
//            toRanged.AddCondition(AnimatorConditionMode.If, 0, "RangedMode");
//            toRanged.duration = defaultTransitionDuration;
//        }
//    }

//    private void CreateAerialTransitions(AnimatorStateMachine aerialSM)
//    {
//        // Jump to air attack
//        var toAirAttack = aerialSM.AddAnyStateTransition(GetStateByName(aerialSM, "Air_Attack_"));
//        if ( toAirAttack != null )
//        {
//            toAirAttack.AddCondition(AnimatorConditionMode.If, 0, "IsAttacking");
//            toAirAttack.AddCondition(AnimatorConditionMode.If, 0, "IsInAir");
//            toAirAttack.duration = 0f;
//            toAirAttack.hasExitTime = false;
//        }

//        // Double jump
//        var toDoubleJump = aerialSM.AddAnyStateTransition(GetStateByName(aerialSM, "Double_Jump"));
//        if ( toDoubleJump != null )
//        {
//            toDoubleJump.AddCondition(AnimatorConditionMode.If, 0, "Jump");
//            toDoubleJump.AddCondition(AnimatorConditionMode.If, 0, "IsInAir");
//            toDoubleJump.duration = 0f;
//        }
//    }

//    private void CreateSpecialTransitions(AnimatorStateMachine specialSM)
//    {
//        // Hit reactions
//        var toHitReaction = specialSM.AddAnyStateTransition(GetStateByName(specialSM, "Hit_"));
//        if ( toHitReaction != null )
//        {
//            toHitReaction.AddCondition(AnimatorConditionMode.If, 0, "HitReaction");
//            toHitReaction.duration = 0f;
//            toHitReaction.hasExitTime = false;
//        }

//        // Death
//        var toDeath = specialSM.AddAnyStateTransition(GetStateByName(specialSM, "Death"));
//        if ( toDeath != null )
//        {
//            toDeath.AddCondition(AnimatorConditionMode.If, 0, "DeathTrigger");
//            toDeath.duration = 0f;
//            toDeath.hasExitTime = false;
//        }
//    }

//    private AnimatorStateMachine GetStateMachine(string name)
//    {
//        foreach ( var stateMachine in rootStateMachine.stateMachines )
//        {
//            if ( stateMachine.stateMachine.name == name )
//                return stateMachine.stateMachine;
//        }
//        return null;
//    }

//    private AnimatorStateMachine GetSubStateMachine(AnimatorStateMachine parent, string name)
//    {
//        foreach ( var stateMachine in parent.stateMachines )
//        {
//            if ( stateMachine.stateMachine.name == name )
//                return stateMachine.stateMachine;
//        }
//        return null;
//    }

//    private AnimatorState GetStateByName(AnimatorStateMachine stateMachine, string namePrefix)
//    {
//        foreach ( var state in stateMachine.states )
//        {
//            if ( state.state.name.StartsWith(namePrefix) )
//                return state.state;
//        }
//        return null;
//    }

//    private void GenerateTestAnimations()
//    {
//        string animPath = Path.Combine(savePath, "TestAnimations/");
//        if ( !Directory.Exists(animPath) )
//        {
//            Directory.CreateDirectory(animPath);
//        }

//        // Create basic test animation clips
//        CreateTestAnimationClip(animPath, "Idle");
//        CreateTestAnimationClip(animPath, "Walk");
//        CreateTestAnimationClip(animPath, "Attack");
//        CreateTestAnimationClip(animPath, "Guard");
//        CreateTestAnimationClip(animPath, "Jump");
//        CreateTestAnimationClip(animPath, "Land");

//        AssetDatabase.Refresh();
//        Debug.Log($"Test animations generated at: {animPath}");
//    }

//    private void CreateTestAnimationClip(string path, string clipName)
//    {
//        AnimationClip clip = new AnimationClip();
//        clip.name = clipName;

//        // Create a simple animation curve for testing
//        AnimationCurve curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
//        clip.SetCurve("", typeof(Transform), "localPosition.y", curve);

//        string fullPath = Path.Combine(path, clipName + ".anim");
//        AssetDatabase.CreateAsset(clip, fullPath);
//    }
//}

///// <summary>
///// バトルシステム用アニメーターパラメーター定数クラス
///// アニメーターコントローラーとスクリプト間での型安全なパラメーター参照を提供
///// </summary>
//public static class BattleAnimatorParameters
//{
//    // === Core State Parameters ===
//    public const string ACTION_MODE = "ActionMode";
//    public const string ACTION_STATE = "ActionState";
//    public const string ATTACK_DIRECTION = "AttackDirection";

//    // === Movement Parameters ===
//    public const string MOVE_X = "MoveX";
//    public const string MOVE_Z = "MoveZ";
//    public const string SPEED = "Speed";
//    public const string IS_GROUNDED = "IsGrounded";
//    public const string IS_MOVING = "IsMoving";
//    public const string IS_BOOSTING = "IsBoosting";

//    // === Combat Parameters ===
//    public const string IS_ATTACKING = "IsAttacking";
//    public const string ATTACK_TYPE = "AttackType";
//    public const string COMBO_COUNT = "ComboCount";
//    public const string IS_GUARDING = "IsGuarding";
//    public const string IS_BLOCKING = "IsBlocking";

//    // === Aerial Parameters ===
//    public const string IS_IN_AIR = "IsInAir";
//    public const string VERTICAL_VELOCITY = "VerticalVelocity";
//    public const string IS_JUMPING = "IsJumping";
//    public const string IS_FALLING = "IsFalling";
//    public const string AIR_ATTACK_COUNT = "AirAttackCount";

//    // === System Parameters ===
//    public const string ENERGY_PERCENTAGE = "EnergyPercentage";
//    public const string HEALTH_PERCENTAGE = "HealthPercentage";
//    public const string IS_STUNNED = "IsStunned";
//    public const string IS_INVINCIBLE = "IsInvincible";
//    public const string IS_ENERGY_DEPLETED = "IsEnergyDepleted";

//    // === Action Triggers ===
//    public const string WEAK_ATTACK = "WeakAttack";
//    public const string STRONG_ATTACK = "StrongAttack";
//    public const string SKILL_ATTACK = "SkillAttack";
//    public const string DODGE = "Dodge";
//    public const string JUMP = "Jump";
//    public const string BLOCK = "Block";
//    public const string MANEUVER = "Maneuver";

//    // === Mode Parameters ===
//    public const string MELEE_MODE = "MeleeMode";
//    public const string RANGED_MODE = "RangedMode";
//    public const string ENERGY_BARRIER_MODE = "EnergyBarrierMode";

//    // === Special Parameters ===
//    public const string DAMAGE_DIRECTION = "DamageDirection";
//    public const string HIT_REACTION = "HitReaction";
//    public const string DEATH_TRIGGER = "DeathTrigger";
//    public const string REVIVE_TRIGGER = "ReviveTrigger";

//    // === Enum Values for Parameters ===
//    public static class ActionModes
//    {
//        public const int MELEE = 0;
//        public const int RANGED = 1;
//        public const int ENERGY_BARRIER = 2;
//    }

//    public static class ActionStates
//    {
//        public const int IDLE = 0;
//        public const int WALKING = 1;
//        public const int JUMPING = 2;
//        public const int FALLING = 3;
//        public const int BOOSTING = 4;
//        public const int DODGING = 5;
//        public const int ATTACKING = 6;
//        public const int GUARDING = 7;
//        public const int USING_MANEUVER = 8;
//    }

//    public static class AttackDirections
//    {
//        public const int UP = 0;
//        public const int LEFT = 1;
//        public const int RIGHT = 2;
//    }

//    public static class AttackTypes
//    {
//        public const int WEAK = 0;
//        public const int STRONG = 1;
//        public const int SKILL = 2;
//    }
//}

///// <summary>
///// アニメーターコントローラーとStateSystemの統合クラス
///// StateSystemからアニメーターへのパラメーター更新を自動化
///// </summary>
//[System.Serializable]
//public class BattleAnimatorUpdater
//{
//    [Header("References")]
//    public Animator animator;

//    [Header("Update Settings")]
//    public bool autoUpdate = true;
//    public float updateInterval = 0.02f; // 50fps equivalent

//    private float lastUpdateTime;
//    private int cachedActionMode = -1;
//    private int cachedActionState = -1;
//    private bool cachedIsAttacking = false;
//    private bool cachedIsGuarding = false;
//    private bool cachedIsInAir = false;

//    public void Initialize(Animator targetAnimator)
//    {
//        animator = targetAnimator;
//        if ( animator == null )
//        {
//            Debug.LogError("BattleAnimatorUpdater: Animator reference is null!");
//            return;
//        }

//        // Verify animator controller has required parameters
//        ValidateAnimatorParameters();
//    }

//    public void UpdateFromStateSystem(StateSystem stateSystem)
//    {
//        if ( animator == null || stateSystem == null )
//            return;

//        if ( !autoUpdate || Time.time - lastUpdateTime < updateInterval )
//            return;

//        // === Core State Updates ===
//        UpdateActionMode(stateSystem.CurrentActionMode);
//        UpdateActionState(stateSystem.CurrentActionState);
//        UpdateAttackDirection(stateSystem.CurrentAttackDirection);

//        // === Movement Updates ===
//        UpdateMovementParameters(stateSystem.MovementData);

//        // === Combat Updates ===
//        UpdateCombatParameters(stateSystem.CombatData);

//        // === Aerial Updates ===
//        UpdateAerialParameters(stateSystem.AerialData);

//        // === System Updates ===
//        UpdateSystemParameters(stateSystem.HealthData, stateSystem.EnergyData);

//        lastUpdateTime = Time.time;
//    }

//    private void UpdateActionMode(ActionMode actionMode)
//    {
//        int modeValue = (int)actionMode;
//        if ( cachedActionMode != modeValue )
//        {
//            animator.SetInteger(BattleAnimatorParameters.ACTION_MODE, modeValue);

//            // Update mode booleans
//            animator.SetBool(BattleAnimatorParameters.MELEE_MODE, actionMode == ActionMode.Melee);
//            animator.SetBool(BattleAnimatorParameters.RANGED_MODE, actionMode == ActionMode.Ranged);
//            animator.SetBool(BattleAnimatorParameters.ENERGY_BARRIER_MODE, actionMode == ActionMode.EnergyBarrier);

//            cachedActionMode = modeValue;
//        }
//    }

//    private void UpdateActionState(ActionState actionState)
//    {
//        int stateValue = (int)actionState;
//        if ( cachedActionState != stateValue )
//        {
//            animator.SetInteger(BattleAnimatorParameters.ACTION_STATE, stateValue);

//            // Update state booleans
//            bool isAttacking = actionState == ActionState.Attacking;
//            bool isGuarding = actionState == ActionState.Guarding;
//            bool isInAir = actionState == ActionState.Jumping || actionState == ActionState.Falling;

//            if ( cachedIsAttacking != isAttacking )
//            {
//                animator.SetBool(BattleAnimatorParameters.IS_ATTACKING, isAttacking);
//                cachedIsAttacking = isAttacking;
//            }

//            if ( cachedIsGuarding != isGuarding )
//            {
//                animator.SetBool(BattleAnimatorParameters.IS_GUARDING, isGuarding);
//                cachedIsGuarding = isGuarding;
//            }

//            if ( cachedIsInAir != isInAir )
//            {
//                animator.SetBool(BattleAnimatorParameters.IS_IN_AIR, isInAir);
//                cachedIsInAir = isInAir;
//            }

//            cachedActionState = stateValue;
//        }
//    }

//    private void UpdateAttackDirection(AttackDirection direction)
//    {
//        animator.SetInteger(BattleAnimatorParameters.ATTACK_DIRECTION, (int)direction);
//    }

//    private void UpdateMovementParameters(MovementData movementData)
//    {
//        animator.SetFloat(BattleAnimatorParameters.MOVE_X, movementData.moveInput.x);
//        animator.SetFloat(BattleAnimatorParameters.MOVE_Z, movementData.moveInput.z);
//        animator.SetFloat(BattleAnimatorParameters.SPEED, movementData.currentSpeed);
//        animator.SetBool(BattleAnimatorParameters.IS_GROUNDED, movementData.isGrounded);
//        animator.SetBool(BattleAnimatorParameters.IS_MOVING, movementData.isMoving);
//        animator.SetBool(BattleAnimatorParameters.IS_BOOSTING, movementData.isBoosting);
//    }

//    private void UpdateCombatParameters(CombatData combatData)
//    {
//        animator.SetInteger(BattleAnimatorParameters.ATTACK_TYPE, (int)combatData.currentAttackType);
//        animator.SetInteger(BattleAnimatorParameters.COMBO_COUNT, combatData.comboCount);
//        animator.SetBool(BattleAnimatorParameters.IS_BLOCKING, combatData.isBlocking);
//    }

//    private void UpdateAerialParameters(AerialData aerialData)
//    {
//        animator.SetFloat(BattleAnimatorParameters.VERTICAL_VELOCITY, aerialData.verticalVelocity);
//        animator.SetBool(BattleAnimatorParameters.IS_JUMPING, aerialData.isJumping);
//        animator.SetBool(BattleAnimatorParameters.IS_FALLING, aerialData.isFalling);
//        animator.SetInteger(BattleAnimatorParameters.AIR_ATTACK_COUNT, aerialData.airAttackCount);
//    }

//    private void UpdateSystemParameters(HealthData healthData, EnergyData energyData)
//    {
//        animator.SetFloat(BattleAnimatorParameters.ENERGY_PERCENTAGE, energyData.energyPercentage);
//        animator.SetFloat(BattleAnimatorParameters.HEALTH_PERCENTAGE, healthData.healthPercentage);
//        animator.SetBool(BattleAnimatorParameters.IS_STUNNED, healthData.isStunned);
//        animator.SetBool(BattleAnimatorParameters.IS_INVINCIBLE, healthData.isInvincible);
//        animator.SetBool(BattleAnimatorParameters.IS_ENERGY_DEPLETED, energyData.isEnergyDepleted);
//    }

//    // === Trigger Methods for Actions ===
//    public void TriggerWeakAttack() => animator.SetTrigger(BattleAnimatorParameters.WEAK_ATTACK);
//    public void TriggerStrongAttack() => animator.SetTrigger(BattleAnimatorParameters.STRONG_ATTACK);
//    public void TriggerSkillAttack() => animator.SetTrigger(BattleAnimatorParameters.SKILL_ATTACK);
//    public void TriggerDodge() => animator.SetTrigger(BattleAnimatorParameters.DODGE);
//    public void TriggerJump() => animator.SetTrigger(BattleAnimatorParameters.JUMP);
//    public void TriggerBlock() => animator.SetTrigger(BattleAnimatorParameters.BLOCK);
//    public void TriggerManeuver() => animator.SetTrigger(BattleAnimatorParameters.MANEUVER);
//    public void TriggerHitReaction() => animator.SetTrigger(BattleAnimatorParameters.HIT_REACTION);
//    public void TriggerDeath() => animator.SetTrigger(BattleAnimatorParameters.DEATH_TRIGGER);
//    public void TriggerRevive() => animator.SetTrigger(BattleAnimatorParameters.REVIVE_TRIGGER);

//    private void ValidateAnimatorParameters()
//    {
//        if ( animator.runtimeAnimatorController == null )
//        {
//            Debug.LogWarning("BattleAnimatorUpdater: No AnimatorController assigned!");
//            return;
//        }

//        // Check for required parameters (implement if needed)
//        var parameters = animator.parameters;
//        var requiredParams = new string[]
//        {
//            BattleAnimatorParameters.ACTION_MODE,
//            BattleAnimatorParameters.ACTION_STATE,
//            BattleAnimatorParameters.IS_ATTACKING,
//            BattleAnimatorParameters.IS_GUARDING,
//            BattleAnimatorParameters.IS_IN_AIR
//        };

//        foreach ( var param in requiredParams )
//        {
//            bool found = false;
//            foreach ( var animParam in parameters )
//            {
//                if ( animParam.name == param )
//                {
//                    found = true;
//                    break;
//                }
//            }

//            if ( !found )
//            {
//                Debug.LogWarning($"BattleAnimatorUpdater: Required parameter '{param}' not found in AnimatorController!");
//            }
//        }
//    }
//}

