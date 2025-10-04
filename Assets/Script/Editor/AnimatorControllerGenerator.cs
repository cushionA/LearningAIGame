using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;

namespace LearningAIGame.CombatSystem.Editor
{
    public class AnimatorControllerGenerator : EditorWindow
    {
        private string _controllerName = "BattleCharacter";
        private string _savePath = "Assets/Animations/Controllers";

        [MenuItem("Tools/Combat System/Generate Animator Controller")]
        private static void ShowWindow()
        {
            var window = GetWindow<AnimatorControllerGenerator>();
            window.titleContent = new GUIContent("Animator Generator");
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("Animator Controller Generator", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            _controllerName = EditorGUILayout.TextField("Controller Name", _controllerName);
            _savePath = EditorGUILayout.TextField("Save Path", _savePath);

            EditorGUILayout.Space();

            if (GUILayout.Button("Generate Animator Controller", GUILayout.Height(30)))
            {
                GenerateAnimatorController();
            }
        }

        private void GenerateAnimatorController()
        {
            if (!AssetDatabase.IsValidFolder(_savePath))
            {
                System.IO.Directory.CreateDirectory(_savePath);
            }

            string path = $"{_savePath}/{_controllerName}.controller";
            var controller = AnimatorController.CreateAnimatorControllerAtPath(path);

            AddParameters(controller);

            var rootStateMachine = controller.layers[0].stateMachine;

            CreateStates(controller, rootStateMachine);
            SetupTransitions(controller, rootStateMachine);

            AssetDatabase.SaveAssets();
            Debug.Log($"AnimatorController generated at: {path}");
        }

        private void AddParameters(AnimatorController controller)
        {
            controller.AddParameter("ActionState", AnimatorControllerParameterType.Int);
            controller.AddParameter("MoveX", AnimatorControllerParameterType.Float);
            controller.AddParameter("MoveZ", AnimatorControllerParameterType.Float);
            controller.AddParameter("Stance", AnimatorControllerParameterType.Int);
        }

        private void CreateStates(AnimatorController controller, AnimatorStateMachine stateMachine)
        {
            // Stance差分ありアクション（Sub-State Machine）
            var stanceVariantActions = new List<(int bitPos, string name, bool hasWalk)>
            {
                (0, "Guard", true),
                (1, "Blocking", false),
                (2, "FrontAvoid", false),
                (3, "SideAvoid", false),
                (4, "BackAvoid", false),
                (7, "WeakAttack", false),
                (8, "HeavyAttack", false),
                (9, "BlockingSuccess", false),
                (10, "GuardSuccess", false),
                (11, "HeavyAttackCancel", false)
            };

            // Stance差分なしアクション（通常ステート）
            var noStanceActions = new List<(int bitPos, string name)>
            {
                (12, "SmallStagger"),
                (13, "LargeStagger"),
                (14, "BlockedWeakAttack"),
                (15, "BlockedHeavyAttack"),
                (16, "GuardedWeakAttack"),
                (17, "Death")
            };

            // Sub-State Machine作成
            foreach (var (bitPos, name, hasWalk) in stanceVariantActions)
            {
                CreateStanceSubStateMachine(controller, stateMachine, bitPos, name, hasWalk);
            }

            // 通常ステート作成
            foreach (var (bitPos, name) in noStanceActions)
            {
                stateMachine.AddState(name, GetStatePosition(bitPos));
            }
        }

        private void CreateStanceSubStateMachine(AnimatorController controller, AnimatorStateMachine parent,
            int bitPos, string name, bool hasWalk)
        {
            var subSM = parent.AddStateMachine(name, GetStatePosition(bitPos));

            // 3つのStanceステート
            var upState = subSM.AddState($"{name}_Up", new Vector3(0, 0, 0));
            var leftState = subSM.AddState($"{name}_Left", new Vector3(0, 80, 0));
            var rightState = subSM.AddState($"{name}_Right", new Vector3(0, 160, 0));

            // 歩行BlendTreeを設定（Guardのみ）
            if (hasWalk)
            {
                upState.motion = CreateWalkBlendTree(controller, $"{name}_Up");
                leftState.motion = CreateWalkBlendTree(controller, $"{name}_Left");
                rightState.motion = CreateWalkBlendTree(controller, $"{name}_Right");
            }

            subSM.defaultState = upState;

            // Stance間の遷移（即座に切り替え）
            CreateStanceTransitions(upState, leftState, rightState);
        }

        private void CreateStanceTransitions(AnimatorState upState, AnimatorState leftState, AnimatorState rightState)
        {
            // Up ⇄ Left
            var upToLeft = upState.AddTransition(leftState);
            upToLeft.hasExitTime = false;
            upToLeft.duration = 0f;
            upToLeft.AddCondition(AnimatorConditionMode.Equals, 1, "Stance");

            var leftToUp = leftState.AddTransition(upState);
            leftToUp.hasExitTime = false;
            leftToUp.duration = 0f;
            leftToUp.AddCondition(AnimatorConditionMode.Equals, 0, "Stance");

            // Up ⇄ Right
            var upToRight = upState.AddTransition(rightState);
            upToRight.hasExitTime = false;
            upToRight.duration = 0f;
            upToRight.AddCondition(AnimatorConditionMode.Equals, 2, "Stance");

            var rightToUp = rightState.AddTransition(upState);
            rightToUp.hasExitTime = false;
            rightToUp.duration = 0f;
            rightToUp.AddCondition(AnimatorConditionMode.Equals, 0, "Stance");

            // Left ⇄ Right
            var leftToRight = leftState.AddTransition(rightState);
            leftToRight.hasExitTime = false;
            leftToRight.duration = 0f;
            leftToRight.AddCondition(AnimatorConditionMode.Equals, 2, "Stance");

            var rightToLeft = rightState.AddTransition(leftState);
            rightToLeft.hasExitTime = false;
            rightToLeft.duration = 0f;
            rightToLeft.AddCondition(AnimatorConditionMode.Equals, 1, "Stance");
        }

        private BlendTree CreateWalkBlendTree(AnimatorController controller, string name)
        {
            var blendTree = new BlendTree
            {
                name = $"{name}_Walk",
                blendParameter = "MoveX",
                blendParameterY = "MoveZ",
                blendType = BlendTreeType.FreeformDirectional2D
            };

            blendTree.AddChild(null, new Vector2(0f, 0f));
            blendTree.AddChild(null, new Vector2(0f, 1f));
            blendTree.AddChild(null, new Vector2(0f, -1f));
            blendTree.AddChild(null, new Vector2(-1f, 0f));
            blendTree.AddChild(null, new Vector2(1f, 0f));
            blendTree.AddChild(null, new Vector2(-0.707f, 0.707f));
            blendTree.AddChild(null, new Vector2(0.707f, 0.707f));
            blendTree.AddChild(null, new Vector2(-0.707f, -0.707f));
            blendTree.AddChild(null, new Vector2(0.707f, -0.707f));

            AssetDatabase.AddObjectToAsset(blendTree, controller);
            return blendTree;
        }

        private void SetupTransitions(AnimatorController controller, AnimatorStateMachine stateMachine)
        {
            // ActionStateとアクション名のマッピング
            var actionMapping = new Dictionary<int, string>
            {
                {0, "Guard"},
                {1, "Blocking"},
                {2, "FrontAvoid"},
                {3, "SideAvoid"},
                {4, "BackAvoid"},
                {7, "WeakAttack"},
                {8, "HeavyAttack"},
                {9, "BlockingSuccess"},
                {10, "GuardSuccess"},
                {11, "HeavyAttackCancel"},
                {12, "SmallStagger"},
                {13, "LargeStagger"},
                {14, "BlockedWeakAttack"},
                {15, "BlockedHeavyAttack"},
                {16, "GuardedWeakAttack"},
                {17, "Death"}
            };

            // Any Stateから全アクションへの遷移
            foreach (var kvp in actionMapping)
            {
                int actionState = kvp.Key;
                string targetName = kvp.Value;

                // Sub-State Machineか通常ステートか判定
                var targetSM = FindStateMachineByName(stateMachine, targetName);
                if (targetSM != null)
                {
                    // Sub-State Machineへの遷移
                    var transition = stateMachine.AddAnyStateTransition(targetSM);
                    transition.AddCondition(AnimatorConditionMode.Equals, actionState, "ActionState");
                    transition.duration = 0.1f;
                }
                else
                {
                    // 通常ステートへの遷移
                    var targetState = FindStateByName(stateMachine, targetName);
                    if (targetState != null)
                    {
                        var transition = stateMachine.AddAnyStateTransition(targetState);
                        transition.AddCondition(AnimatorConditionMode.Equals, actionState, "ActionState");
                        transition.duration = 0.1f;
                    }
                }
            }
        }

        private Vector3 GetStatePosition(int bitPos)
        {
            int row = bitPos / 4;
            int col = bitPos % 4;
            return new Vector3(col * 300f, row * 100f, 0f);
        }

        private AnimatorStateMachine FindStateMachineByName(AnimatorStateMachine parent, string name)
        {
            foreach (var child in parent.stateMachines)
            {
                if (child.stateMachine.name == name)
                    return child.stateMachine;
            }
            return null;
        }

        private AnimatorState FindStateByName(AnimatorStateMachine parent, string name)
        {
            foreach (var child in parent.states)
            {
                if (child.state.name == name)
                    return child.state;
            }
            return null;
        }
    }
}