using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using TrainingScenarios.Runtime.Configuration.Definitions;

namespace TrainingScenarios.Editor.Windows
{
    public sealed class ScenarioCollectionEditor
    {
        private const float RowSpacing = 4f;
        private const float StepDragHandleWidth = 22f;
        private const float HeaderButtonWidth = 118f;
        private const string StepDragKey = "TrainingScenarios.StepDrag";

        private readonly Action _configurationChanged;
        private readonly Action _repaint;
        private readonly Dictionary<string, ReorderableList> _nestedStepLists =
            new Dictionary<string, ReorderableList>();

        private ScenarioDefinition _scenario;
        private ReorderableList _scenarioStepsList;
        private ReorderableList _trainingStagesList;
        private ReorderableList _examStagesList;
        private ReorderableList _examGroupsList;
        private StepDragData _pendingStepDrag;
        private Vector2 _stepDragStart;

        public ScenarioCollectionEditor(Action configurationChanged, Action repaint)
        {
            _configurationChanged = configurationChanged;
            _repaint = repaint;
        }

        public void Rebuild(
            ScenarioDefinition scenario,
            SerializedObject scenarioObject,
            SerializedObject trainingProfileObject,
            SerializedObject examProfileObject)
        {
            _scenario = scenario;
            _nestedStepLists.Clear();
            _scenarioStepsList = scenarioObject == null
                ? null
                : CreateScenarioStepsList(scenarioObject);
            _trainingStagesList = trainingProfileObject == null
                ? null
                : CreateStagesList(trainingProfileObject, "Training Flow");
            _examStagesList = examProfileObject == null
                ? null
                : CreateStagesList(examProfileObject, "Exam Flow");
            _examGroupsList = examProfileObject == null
                ? null
                : CreateGroupsList(examProfileObject);
        }

        public void HandleCurrentEvent()
        {
            if (Event.current.type != EventType.DragExited)
            {
                return;
            }

            DragAndDrop.SetGenericData(StepDragKey, null);
            _pendingStepDrag = null;
        }

        public void DrawScenarioSteps()
        {
            EditorGUILayout.HelpBox(
                "Use + to create a step or add existing step assets. Drag the left grip to " +
                "reorder the catalog; drag the arrow on the right into a flow stage or exam group.",
                MessageType.Info);
            DrawList(_scenarioStepsList, StepCollectionKind.Scenario);
        }

        public void DrawTrainingStages()
        {
            DrawList(_trainingStagesList, StepCollectionKind.ActivationStage);
        }

        public void DrawExamStages()
        {
            DrawList(_examStagesList, StepCollectionKind.ActivationStage);
        }

        public void DrawExamGroups()
        {
            DrawList(_examGroupsList, StepCollectionKind.ExamGroup);
        }

        private void DrawList(ReorderableList list, StepCollectionKind collectionKind)
        {
            if (list == null)
            {
                return;
            }

            var rect = GUILayoutUtility.GetRect(
                0f,
                list.GetHeight(),
                GUILayout.ExpandWidth(true));
            list.DoList(rect);

            if (collectionKind == StepCollectionKind.Scenario)
            {
                HandleExternalStepDrop(rect, list.serializedProperty);
            }
        }

        private ReorderableList CreateScenarioStepsList(SerializedObject scenarioObject)
        {
            var steps = scenarioObject.FindProperty("_steps");
            var list = new ReorderableList(
                scenarioObject,
                steps,
                true,
                true,
                true,
                true);
            list.drawHeaderCallback = rect =>
                EditorGUI.LabelField(rect, "Scenario Step Catalog");
            list.drawElementCallback = (rect, index, _, _) =>
                DrawStepElement(rect, list, index, StepCollectionKind.Scenario);
            list.onAddDropdownCallback = (rect, _) => ShowScenarioStepMenu(
                rect,
                scenarioObject.targetObject,
                steps.propertyPath);
            list.onRemoveCallback = RemoveScenarioStep;
            list.onReorderCallback = _ => NotifyConfigurationChanged();
            return list;
        }

        private ReorderableList CreateStagesList(
            SerializedObject profileObject,
            string title)
        {
            var stages = profileObject
                .FindProperty("_activationPlan")
                .FindPropertyRelative("_stages");
            var list = new ReorderableList(
                profileObject,
                stages,
                true,
                true,
                true,
                true);
            list.drawHeaderCallback = rect => DrawStagesHeader(rect, title, stages);
            list.elementHeightCallback = index =>
            {
                var stage = stages.GetArrayElementAtIndex(index);
                var stepList = GetStepList(
                    stage.FindPropertyRelative("_steps"),
                    $"Stage {index + 1} Steps",
                    StepCollectionKind.ActivationStage);
                return EditorGUIUtility.singleLineHeight +
                       RowSpacing +
                       stepList.GetHeight() +
                       RowSpacing;
            };
            list.drawElementCallback = (rect, index, _, _) =>
            {
                var stage = stages.GetArrayElementAtIndex(index);
                var steps = stage.FindPropertyRelative("_steps");
                var titleRect = new Rect(
                    rect.x,
                    rect.y + 1f,
                    rect.width,
                    EditorGUIUtility.singleLineHeight);
                var stageKind = steps.arraySize > 1 ? "parallel" : "sequential";
                EditorGUI.LabelField(
                    titleRect,
                    $"Stage {index + 1}: {steps.arraySize} step(s), {stageKind}",
                    EditorStyles.boldLabel);

                var stepList = GetStepList(
                    steps,
                    $"Stage {index + 1} Steps",
                    StepCollectionKind.ActivationStage);
                var stepListRect = new Rect(
                    rect.x,
                    titleRect.yMax + RowSpacing,
                    rect.width,
                    stepList.GetHeight());
                stepList.DoList(stepListRect);
                HandleStepDrop(
                    stepListRect,
                    stepList,
                    steps,
                    StepCollectionKind.ActivationStage);
            };
            list.onAddCallback = _ => AddStage(stages);
            list.onRemoveCallback = currentList => RemoveStage(
                currentList.serializedProperty,
                currentList.index);
            list.onReorderCallback = _ =>
            {
                _nestedStepLists.Clear();
                NotifyConfigurationChanged();
            };
            return list;
        }

        private ReorderableList CreateGroupsList(SerializedObject profileObject)
        {
            var groups = profileObject.FindProperty("_groups");
            var list = new ReorderableList(
                profileObject,
                groups,
                true,
                true,
                true,
                true);
            list.drawHeaderCallback = rect =>
                EditorGUI.LabelField(rect, "Exam Groups");
            list.elementHeightCallback = index =>
            {
                var group = groups.GetArrayElementAtIndex(index);
                var stepList = GetStepList(
                    group.FindPropertyRelative("_steps"),
                    $"Group {index + 1} Steps",
                    StepCollectionKind.ExamGroup);
                return EditorGUIUtility.singleLineHeight * 2f +
                       RowSpacing * 2f +
                       stepList.GetHeight() +
                       RowSpacing;
            };
            list.drawElementCallback = (rect, index, _, _) =>
            {
                var group = groups.GetArrayElementAtIndex(index);
                var idRect = new Rect(
                    rect.x,
                    rect.y + 1f,
                    rect.width,
                    EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(
                    idRect,
                    group.FindPropertyRelative("_groupId"),
                    new GUIContent($"Group {index + 1} ID"));

                var nameRect = new Rect(
                    rect.x,
                    idRect.yMax + RowSpacing,
                    rect.width,
                    EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(
                    nameRect,
                    group.FindPropertyRelative("_displayName"),
                    new GUIContent("Display Name"));

                var steps = group.FindPropertyRelative("_steps");
                var stepList = GetStepList(
                    steps,
                    $"Group {index + 1} Steps",
                    StepCollectionKind.ExamGroup);
                var stepListRect = new Rect(
                    rect.x,
                    nameRect.yMax + RowSpacing,
                    rect.width,
                    stepList.GetHeight());
                stepList.DoList(stepListRect);
                HandleStepDrop(
                    stepListRect,
                    stepList,
                    steps,
                    StepCollectionKind.ExamGroup);
            };
            list.onAddCallback = _ => AddGroup(groups);
            list.onRemoveCallback = currentList => RemoveGroup(
                currentList.serializedProperty,
                currentList.index);
            list.onReorderCallback = _ =>
            {
                _nestedStepLists.Clear();
                NotifyConfigurationChanged();
            };
            return list;
        }

        private ReorderableList GetStepList(
            SerializedProperty steps,
            string title,
            StepCollectionKind collectionKind)
        {
            var owner = steps.serializedObject.targetObject;
            var key = $"{owner.GetInstanceID()}:{steps.propertyPath}:{collectionKind}";
            if (_nestedStepLists.TryGetValue(key, out var existingList))
            {
                return existingList;
            }

            var propertyPath = steps.propertyPath;
            var list = new ReorderableList(
                steps.serializedObject,
                steps,
                true,
                true,
                true,
                true);
            list.drawHeaderCallback = rect => EditorGUI.LabelField(rect, title);
            list.drawElementCallback = (rect, index, _, _) => DrawStepElement(
                rect,
                list,
                index,
                collectionKind);
            list.onAddDropdownCallback = (rect, _) => ShowStepMenu(
                rect,
                owner,
                propertyPath,
                collectionKind);
            list.onRemoveCallback = currentList => RemoveArrayElement(
                currentList.serializedProperty,
                currentList.index,
                "Remove Step from List");
            list.onReorderCallback = _ => NotifyConfigurationChanged();
            _nestedStepLists.Add(key, list);
            return list;
        }

        private void DrawStepElement(
            Rect rect,
            ReorderableList list,
            int index,
            StepCollectionKind collectionKind)
        {
            rect.y += 1f;
            rect.height = EditorGUIUtility.singleLineHeight;
            var handleRect = new Rect(
                rect.xMax - StepDragHandleWidth,
                rect.y,
                StepDragHandleWidth,
                rect.height);
            var fieldRect = new Rect(
                rect.x,
                rect.y,
                rect.width - StepDragHandleWidth - 2f,
                rect.height);
            var step = list.serializedProperty.GetArrayElementAtIndex(index);
            var stepDefinition = step.objectReferenceValue as StepDefinition;
            if (stepDefinition)
            {
                var content = new GUIContent(
                    GetStepLabel(stepDefinition),
                    $"{AssetDatabase.GetAssetPath(stepDefinition)}\nClick to select the asset.");
                if (GUI.Button(fieldRect, content, EditorStyles.label))
                {
                    Selection.activeObject = stepDefinition;
                    EditorGUIUtility.PingObject(stepDefinition);
                }

                EditorGUIUtility.AddCursorRect(fieldRect, MouseCursor.Link);
            }
            else
            {
                EditorGUI.PropertyField(fieldRect, step, GUIContent.none);
            }

            var handleContent = collectionKind == StepCollectionKind.Scenario
                ? new GUIContent("→", "Drag this step into a flow stage or exam group.")
                : new GUIContent("⇄", "Drag this step to another row or collection.");
            EditorGUI.LabelField(handleRect, handleContent, EditorStyles.centeredGreyMiniLabel);
            EditorGUIUtility.AddCursorRect(handleRect, MouseCursor.Pan);

            HandleStepDragStart(
                handleRect,
                list.serializedProperty,
                index,
                collectionKind);
        }

        private void HandleStepDragStart(
            Rect handleRect,
            SerializedProperty steps,
            int index,
            StepCollectionKind collectionKind)
        {
            var currentEvent = Event.current;
            var owner = steps.serializedObject.targetObject;

            if (currentEvent.type == EventType.MouseDown &&
                currentEvent.button == 0 &&
                handleRect.Contains(currentEvent.mousePosition))
            {
                _pendingStepDrag = new StepDragData(
                    owner,
                    steps.propertyPath,
                    index,
                    collectionKind);
                _stepDragStart = currentEvent.mousePosition;
                currentEvent.Use();
                return;
            }

            if (currentEvent.type == EventType.MouseDrag &&
                _pendingStepDrag != null &&
                _pendingStepDrag.Matches(owner, steps.propertyPath, index) &&
                Vector2.Distance(_stepDragStart, currentEvent.mousePosition) > 3f)
            {
                var step = steps.GetArrayElementAtIndex(index).objectReferenceValue;
                DragAndDrop.PrepareStartDrag();
                DragAndDrop.SetGenericData(StepDragKey, _pendingStepDrag);
                DragAndDrop.objectReferences = !step
                    ? Array.Empty<UnityEngine.Object>()
                    : new[] { step };
                DragAndDrop.StartDrag(!step ? "Move Step" : step.name);
                _pendingStepDrag = null;
                currentEvent.Use();
            }

            if (currentEvent.type == EventType.MouseUp)
            {
                _pendingStepDrag = null;
            }
        }

        private void HandleStepDrop(
            Rect listRect,
            ReorderableList destinationList,
            SerializedProperty destination,
            StepCollectionKind destinationKind)
        {
            var currentEvent = Event.current;
            if (!listRect.Contains(currentEvent.mousePosition) ||
                (currentEvent.type != EventType.DragUpdated &&
                 currentEvent.type != EventType.DragPerform))
            {
                return;
            }

            var dragData = DragAndDrop.GetGenericData(StepDragKey) as StepDragData;
            var draggedSteps = DragAndDrop.objectReferences
                .OfType<StepDefinition>()
                .ToArray();
            var destinationOwner = destination.serializedObject.targetObject;
            var canMoveInternal = dragData != null &&
                                  dragData.Kind == destinationKind &&
                                  dragData.Owner == destinationOwner;
            var copyableSteps = draggedSteps
                .Where(step =>
                    IsScenarioStep(step) &&
                    !IsAssignedToCollection(destinationOwner, destinationKind, step))
                .ToArray();
            var canCopy = !canMoveInternal && copyableSteps.Length > 0;

            if (!canMoveInternal && !canCopy)
            {
                return;
            }

            DragAndDrop.visualMode = canMoveInternal
                ? DragAndDropVisualMode.Move
                : DragAndDropVisualMode.Copy;

            if (currentEvent.type == EventType.DragUpdated)
            {
                currentEvent.Use();
                return;
            }

            DragAndDrop.AcceptDrag();
            var destinationIndex = GetDropIndex(
                listRect,
                destinationList,
                destination.arraySize,
                currentEvent.mousePosition.y);

            if (canMoveInternal)
            {
                MoveStep(dragData, destination, destinationIndex);
            }
            else
            {
                foreach (var step in copyableSteps)
                {
                    InsertObjectReference(destination, destinationIndex, step);
                    destinationIndex++;
                }
            }

            DragAndDrop.SetGenericData(StepDragKey, null);
            _nestedStepLists.Clear();
            NotifyConfigurationChanged();
            currentEvent.Use();
        }

        private void HandleExternalStepDrop(Rect rect, SerializedProperty destination)
        {
            var currentEvent = Event.current;
            if (!rect.Contains(currentEvent.mousePosition) ||
                (currentEvent.type != EventType.DragUpdated &&
                 currentEvent.type != EventType.DragPerform))
            {
                return;
            }

            if (DragAndDrop.GetGenericData(StepDragKey) != null)
            {
                return;
            }

            var steps = DragAndDrop.objectReferences.OfType<StepDefinition>().ToArray();
            if (steps.Length == 0)
            {
                return;
            }

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            if (currentEvent.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                AppendSteps(
                    destination.serializedObject.targetObject,
                    destination.propertyPath,
                    steps);
            }

            currentEvent.Use();
        }

        private void MoveStep(
            StepDragData sourceData,
            SerializedProperty destination,
            int destinationIndex)
        {
            var destinationOwner = destination.serializedObject.targetObject;

            if (sourceData.Owner == destinationOwner)
            {
                MoveStepWithinObject(sourceData, destination, destinationIndex);
                return;
            }

            var sourceObject = new SerializedObject(sourceData.Owner);
            sourceObject.Update();
            var source = sourceObject.FindProperty(sourceData.PropertyPath);
            if (!IsValidIndex(source, sourceData.Index))
            {
                return;
            }

            var step = source.GetArrayElementAtIndex(sourceData.Index).objectReferenceValue;
            Undo.RecordObjects(
                new[] { sourceData.Owner, destinationOwner },
                "Move Step");
            DeleteArrayElement(source, sourceData.Index);
            sourceObject.ApplyModifiedProperties();
            InsertObjectReference(destination, destinationIndex, step);
        }

        private void MoveStepWithinObject(
            StepDragData sourceData,
            SerializedProperty destination,
            int destinationIndex)
        {
            var serializedObject = destination.serializedObject;
            serializedObject.Update();
            var source = serializedObject.FindProperty(sourceData.PropertyPath);
            var currentDestination = serializedObject.FindProperty(destination.propertyPath);

            if (!IsValidIndex(source, sourceData.Index))
            {
                return;
            }

            Undo.RecordObject(sourceData.Owner, "Move Step");

            if (source.propertyPath == currentDestination.propertyPath)
            {
                var targetIndex = Mathf.Clamp(destinationIndex, 0, source.arraySize);
                if (targetIndex > sourceData.Index)
                {
                    targetIndex--;
                }

                targetIndex = Mathf.Clamp(
                    targetIndex,
                    0,
                    Math.Max(0, source.arraySize - 1));
                source.MoveArrayElement(sourceData.Index, targetIndex);
            }
            else
            {
                var step = source
                    .GetArrayElementAtIndex(sourceData.Index)
                    .objectReferenceValue;
                InsertObjectReferenceWithoutApply(
                    currentDestination,
                    destinationIndex,
                    step);
                source = serializedObject.FindProperty(sourceData.PropertyPath);
                DeleteArrayElement(source, sourceData.Index);
            }

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(sourceData.Owner);
        }

        private int GetDropIndex(
            Rect listRect,
            ReorderableList list,
            int count,
            float mouseY)
        {
            var elementsTop = listRect.y + list.headerHeight;
            var relativeY = mouseY - elementsTop;
            if (relativeY <= 0f)
            {
                return 0;
            }

            var rowHeight = list.elementHeight + 2f;
            return Mathf.Clamp(Mathf.FloorToInt(relativeY / rowHeight), 0, count);
        }

        private void ShowScenarioStepMenu(
            Rect rect,
            UnityEngine.Object owner,
            string propertyPath)
        {
            var menu = new GenericMenu();
            menu.AddItem(
                new GUIContent("Create New Step..."),
                false,
                () => CreateStepAndAppend(owner, propertyPath));

            var assignedSteps = new HashSet<StepDefinition>(
                _scenario.Steps.Where(step => step));
            var availableSteps = FindProjectSteps()
                .Where(step => !assignedSteps.Contains(step))
                .ToArray();

            menu.AddSeparator(string.Empty);
            if (availableSteps.Length == 0)
            {
                menu.AddDisabledItem(new GUIContent("All Project Steps Are Added"));
            }
            else
            {
                menu.AddItem(
                    new GUIContent($"Add All Unassigned ({availableSteps.Length})"),
                    false,
                    () => AppendSteps(owner, propertyPath, availableSteps));
                menu.AddSeparator(string.Empty);

                foreach (var step in availableSteps)
                {
                    var currentStep = step;
                    menu.AddItem(
                        new GUIContent($"Existing/{GetStepLabel(step)}"),
                        false,
                        () => AppendStep(owner, propertyPath, currentStep));
                }
            }

            menu.DropDown(rect);
        }

        private void ShowStepMenu(
            Rect rect,
            UnityEngine.Object owner,
            string propertyPath,
            StepCollectionKind collectionKind)
        {
            var menu = new GenericMenu();
            var assignedSteps = GetAssignedSteps(owner, collectionKind);
            var availableSteps = _scenario.Steps
                .Where(step => step && !assignedSteps.Contains(step))
                .ToArray();

            if (availableSteps.Length == 0)
            {
                menu.AddDisabledItem(new GUIContent(
                    _scenario.Steps.Count == 0
                        ? "The scenario has no steps"
                        : "All scenario steps are already assigned"));
            }
            else
            {
                foreach (var step in availableSteps)
                {
                    var currentStep = step;
                    menu.AddItem(
                        new GUIContent(GetStepLabel(step)),
                        false,
                        () => AppendStep(owner, propertyPath, currentStep));
                }
            }

            menu.DropDown(rect);
        }

        private void AppendStep(
            UnityEngine.Object owner,
            string propertyPath,
            StepDefinition step)
        {
            var serializedObject = new SerializedObject(owner);
            serializedObject.Update();
            var destination = serializedObject.FindProperty(propertyPath);
            AppendObjectReference(destination, step, "Add Step");
            _nestedStepLists.Clear();
            NotifyConfigurationChanged();
            _repaint();
        }

        private void AppendSteps(
            UnityEngine.Object owner,
            string propertyPath,
            IReadOnlyList<StepDefinition> steps)
        {
            var serializedObject = new SerializedObject(owner);
            serializedObject.Update();
            var destination = serializedObject.FindProperty(propertyPath);
            var existing = new HashSet<StepDefinition>();
            for (var index = 0; index < destination.arraySize; index++)
            {
                if (destination.GetArrayElementAtIndex(index).objectReferenceValue is
                    StepDefinition step)
                {
                    existing.Add(step);
                }
            }

            Undo.RecordObject(owner, "Add Scenario Steps");
            foreach (var step in steps)
            {
                if (step && existing.Add(step))
                {
                    InsertObjectReferenceWithoutApply(
                        destination,
                        destination.arraySize,
                        step);
                }
            }

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(owner);
            _nestedStepLists.Clear();
            NotifyConfigurationChanged();
            _repaint();
        }

        private void CreateStepAndAppend(UnityEngine.Object owner, string propertyPath)
        {
            var path = EditorUtility.SaveFilePanelInProject(
                "Create Scenario Step",
                "NewStep",
                "asset",
                "Choose where to save the new step definition.");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            var step = ScriptableObject.CreateInstance<StepDefinition>();
            var serializedStep = new SerializedObject(step);
            serializedStep.FindProperty("_stepId").stringValue = Guid.NewGuid().ToString("N");
            serializedStep.FindProperty("_displayName").stringValue =
                ObjectNames.NicifyVariableName(System.IO.Path.GetFileNameWithoutExtension(path));
            serializedStep.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(step, AssetDatabase.GenerateUniqueAssetPath(path));
            Undo.RegisterCreatedObjectUndo(step, "Create Scenario Step");
            AppendStep(owner, propertyPath, step);
            Selection.activeObject = step;
            EditorGUIUtility.PingObject(step);
        }

        private void DrawStagesHeader(Rect rect, string title, SerializedProperty stages)
        {
            var buttonRect = new Rect(
                rect.xMax - HeaderButtonWidth,
                rect.y,
                HeaderButtonWidth,
                rect.height);
            var labelRect = new Rect(
                rect.x,
                rect.y,
                rect.width - HeaderButtonWidth - RowSpacing,
                rect.height);

            EditorGUI.LabelField(labelRect, title);
            if (GUI.Button(
                    buttonRect,
                    new GUIContent(
                        "Add Unassigned",
                        "Create one sequential stage for every scenario step not yet in this flow."),
                    EditorStyles.miniButton))
            {
                AddUnassignedStages(stages);
            }
        }

        private void AddUnassignedStages(SerializedProperty stages)
        {
            var assigned = GetAssignedSteps(
                stages.serializedObject.targetObject,
                StepCollectionKind.ActivationStage);
            var unassigned = _scenario.Steps
                .Where(step => step && !assigned.Contains(step))
                .ToArray();
            if (unassigned.Length == 0)
            {
                return;
            }

            Undo.RecordObject(stages.serializedObject.targetObject, "Add Unassigned Stages");
            foreach (var step in unassigned)
            {
                var stageIndex = stages.arraySize;
                stages.InsertArrayElementAtIndex(stageIndex);
                var stageSteps = stages
                    .GetArrayElementAtIndex(stageIndex)
                    .FindPropertyRelative("_steps");
                stageSteps.arraySize = 1;
                stageSteps.GetArrayElementAtIndex(0).objectReferenceValue = step;
            }

            stages.serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(stages.serializedObject.targetObject);
            _nestedStepLists.Clear();
            NotifyConfigurationChanged();
            _repaint();
        }

        private HashSet<StepDefinition> GetAssignedSteps(
            UnityEngine.Object owner,
            StepCollectionKind collectionKind)
        {
            var result = new HashSet<StepDefinition>();
            var serializedObject = new SerializedObject(owner);
            serializedObject.Update();
            SerializedProperty collections;

            switch (collectionKind)
            {
                case StepCollectionKind.ActivationStage:
                    collections = serializedObject
                        .FindProperty("_activationPlan")
                        ?.FindPropertyRelative("_stages");
                    break;
                case StepCollectionKind.ExamGroup:
                    collections = serializedObject.FindProperty("_groups");
                    break;
                default:
                    return result;
            }

            if (collections == null)
            {
                return result;
            }

            for (var collectionIndex = 0;
                 collectionIndex < collections.arraySize;
                 collectionIndex++)
            {
                var steps = collections
                    .GetArrayElementAtIndex(collectionIndex)
                    .FindPropertyRelative("_steps");
                for (var stepIndex = 0; stepIndex < steps.arraySize; stepIndex++)
                {
                    if (steps.GetArrayElementAtIndex(stepIndex).objectReferenceValue is
                        StepDefinition step)
                    {
                        result.Add(step);
                    }
                }
            }

            return result;
        }

        private bool IsAssignedToCollection(
            UnityEngine.Object owner,
            StepCollectionKind collectionKind,
            StepDefinition step)
        {
            return GetAssignedSteps(owner, collectionKind).Contains(step);
        }

        private bool IsScenarioStep(StepDefinition step)
        {
            return step && _scenario.Steps.Contains(step);
        }

        private static StepDefinition[] FindProjectSteps()
        {
            return AssetDatabase.FindAssets("t:StepDefinition")
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .Select(AssetDatabase.LoadAssetAtPath<StepDefinition>)
                .Where(step => step)
                .ToArray();
        }

        private static string GetStepLabel(StepDefinition step)
        {
            return string.IsNullOrWhiteSpace(step.DisplayName)
                ? step.name
                : step.DisplayName;
        }

        private void AddStage(SerializedProperty stages)
        {
            Undo.RecordObject(stages.serializedObject.targetObject, "Add Stage");
            var index = stages.arraySize;
            stages.InsertArrayElementAtIndex(index);
            var stage = stages.GetArrayElementAtIndex(index);
            stage.FindPropertyRelative("_steps").arraySize = 0;
            stages.serializedObject.ApplyModifiedProperties();
            _nestedStepLists.Clear();
            NotifyConfigurationChanged();
        }

        private void RemoveStage(SerializedProperty stages, int index)
        {
            if (!IsValidIndex(stages, index) ||
                !EditorUtility.DisplayDialog(
                    "Delete Stage",
                    $"Delete Stage {index + 1}?",
                    "Delete",
                    "Cancel"))
            {
                return;
            }

            Undo.RecordObject(stages.serializedObject.targetObject, "Delete Stage");
            stages.DeleteArrayElementAtIndex(index);
            stages.serializedObject.ApplyModifiedProperties();
            _nestedStepLists.Clear();
            NotifyConfigurationChanged();
        }

        private void AddGroup(SerializedProperty groups)
        {
            Undo.RecordObject(groups.serializedObject.targetObject, "Add Group");
            var index = groups.arraySize;
            groups.InsertArrayElementAtIndex(index);
            var group = groups.GetArrayElementAtIndex(index);
            group.FindPropertyRelative("_groupId").stringValue = Guid.NewGuid().ToString("N");
            group.FindPropertyRelative("_displayName").stringValue = string.Empty;
            group.FindPropertyRelative("_steps").arraySize = 0;
            groups.serializedObject.ApplyModifiedProperties();
            _nestedStepLists.Clear();
            NotifyConfigurationChanged();
        }

        private void RemoveGroup(SerializedProperty groups, int index)
        {
            if (!IsValidIndex(groups, index) ||
                !EditorUtility.DisplayDialog(
                    "Delete Group",
                    $"Delete Group {index + 1}?",
                    "Delete",
                    "Cancel"))
            {
                return;
            }

            Undo.RecordObject(groups.serializedObject.targetObject, "Delete Group");
            groups.DeleteArrayElementAtIndex(index);
            groups.serializedObject.ApplyModifiedProperties();
            _nestedStepLists.Clear();
            NotifyConfigurationChanged();
        }

        private void AppendObjectReference(
            SerializedProperty destination,
            UnityEngine.Object value,
            string undoName)
        {
            InsertObjectReference(destination, destination.arraySize, value, undoName);
        }

        private void InsertObjectReference(
            SerializedProperty destination,
            int index,
            UnityEngine.Object value,
            string undoName = "Add Step")
        {
            Undo.RecordObject(destination.serializedObject.targetObject, undoName);
            InsertObjectReferenceWithoutApply(destination, index, value);
            destination.serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(destination.serializedObject.targetObject);
        }

        private void InsertObjectReferenceWithoutApply(
            SerializedProperty destination,
            int index,
            UnityEngine.Object value)
        {
            var insertIndex = Mathf.Clamp(index, 0, destination.arraySize);
            destination.InsertArrayElementAtIndex(insertIndex);
            destination.GetArrayElementAtIndex(insertIndex).objectReferenceValue = value;
        }

        private void RemoveArrayElement(
            SerializedProperty array,
            int index,
            string undoName)
        {
            if (!IsValidIndex(array, index))
            {
                return;
            }

            Undo.RecordObject(array.serializedObject.targetObject, undoName);
            DeleteArrayElement(array, index);
            array.serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(array.serializedObject.targetObject);
            _nestedStepLists.Clear();
            NotifyConfigurationChanged();
        }

        private void RemoveScenarioStep(ReorderableList list)
        {
            var steps = list.serializedProperty;
            if (!IsValidIndex(steps, list.index))
            {
                return;
            }

            var step = steps.GetArrayElementAtIndex(list.index).objectReferenceValue;
            var stepName = step ? step.name : "missing step reference";
            var references = FindStepReferences(step);
            if (references.Count > 0)
            {
                EditorUtility.DisplayDialog(
                    "Step Is Still Assigned",
                    $"Remove '{stepName}' from {string.Join(", ", references)} before deleting " +
                    "it from the scenario catalog.",
                    "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    "Remove Step",
                    $"Remove '{stepName}' from the scenario catalog?",
                    "Remove",
                    "Cancel"))
            {
                return;
            }

            RemoveArrayElement(steps, list.index, "Remove Step");
        }

        private List<string> FindStepReferences(UnityEngine.Object step)
        {
            var references = new List<string>();
            AddStepReferences(_trainingStagesList, step, "Training flow", references);
            AddStepReferences(_examStagesList, step, "Exam flow", references);
            AddStepReferences(_examGroupsList, step, "Exam groups", references);
            return references;
        }

        private static void AddStepReferences(
            ReorderableList list,
            UnityEngine.Object step,
            string label,
            ICollection<string> references)
        {
            if (list == null || step == null)
            {
                return;
            }

            var containers = list.serializedProperty;
            for (var containerIndex = 0; containerIndex < containers.arraySize; containerIndex++)
            {
                var assignedSteps = containers
                    .GetArrayElementAtIndex(containerIndex)
                    .FindPropertyRelative("_steps");

                for (var stepIndex = 0; stepIndex < assignedSteps.arraySize; stepIndex++)
                {
                    if (assignedSteps.GetArrayElementAtIndex(stepIndex).objectReferenceValue == step)
                    {
                        references.Add(label);
                        return;
                    }
                }
            }
        }

        private void DeleteArrayElement(SerializedProperty array, int index)
        {
            var element = array.GetArrayElementAtIndex(index);
            if (element.propertyType == SerializedPropertyType.ObjectReference)
            {
                element.objectReferenceValue = null;
            }

            array.DeleteArrayElementAtIndex(index);
        }

        private bool IsValidIndex(SerializedProperty array, int index)
        {
            return array != null && index >= 0 && index < array.arraySize;
        }

        private void NotifyConfigurationChanged()
        {
            _configurationChanged();
        }

        private enum StepCollectionKind
        {
            Scenario,
            ActivationStage,
            ExamGroup
        }

        private sealed class StepDragData
        {
            public StepDragData(
                UnityEngine.Object owner,
                string propertyPath,
                int index,
                StepCollectionKind kind)
            {
                Owner = owner;
                PropertyPath = propertyPath;
                Index = index;
                Kind = kind;
            }

            public UnityEngine.Object Owner { get; }

            public string PropertyPath { get; }

            public int Index { get; }

            public StepCollectionKind Kind { get; }

            public bool Matches(
                UnityEngine.Object owner,
                string propertyPath,
                int index)
            {
                return Owner == owner &&
                       PropertyPath == propertyPath &&
                       Index == index;
            }
        }
    }
}
