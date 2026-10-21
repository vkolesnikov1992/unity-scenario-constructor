using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TrainingScenarios.Demo.Interactions;
using TrainingScenarios.Demo.Presentation;
using TrainingScenarios.Runtime.Actions.Bindings;
using TrainingScenarios.Runtime.Actions.Models;
using TrainingScenarios.Runtime.Actions.Sources;
using TrainingScenarios.Runtime.Composition;
using TrainingScenarios.Runtime.Configuration.Definitions;
using TrainingScenarios.Runtime.Configuration.Providers;

public static class BuildScenarioGameplay
{
    private const string ScenePath = "Assets/TrainingScenarios/Demo/Scenes/ScenarioDemo.unity";
    private const string ScenarioPath = "Assets/TrainingScenarios/Demo/Configurations/DemoScenario.asset";
    private const string TrainingAnalyzerPath = "Assets/TrainingScenarios/Demo/Configurations/DemoScenario_TrainingAnalyzer.asset";
    private const string ExamAnalyzerPath = "Assets/TrainingScenarios/Demo/Configurations/DemoScenario_ExamAnalyzer.asset";
    private const string MaterialFolder = "Assets/TrainingScenarios/Demo/Materials/Gameplay";

    private static Font _font;
    private static Camera _camera;
    private static Material _floorMaterial;
    private static Material _wallMaterial;
    private static Material _baseMaterial;
    private static Material _indicatorMaterial;
    private static Material _redMaterial;
    private static Material _blueZoneMaterial;
    private static Material _greenMaterial;

    private sealed class Station
    {
        public GameObject Root;
        public WorldInteractable Interactable;
        public WorldActionSource Source;
        public Renderer Indicator;
        public Transform VisualPivot;
    }

    public static string All()
    {
        var scenario = AssetDatabase.LoadAssetAtPath<ScenarioDefinition>(ScenarioPath);
        if (!scenario || scenario.Steps.Count != 12)
        {
            throw new InvalidOperationException("DemoScenario with exactly 12 configured steps is required.");
        }

        EnsureMaterials();
        UpdateScenarioLabels(scenario);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "ScenarioDemo";

        CreateEnvironment();
        var camera = CreateCameraAndLighting();
        var ui = CreateHud();
        var player = CreatePlayer(ui.PromptText);
        var playerStart = CreatePlayerStart(player.transform.position, player.transform.rotation);
        CreateEventSystem();

        var runtimeObject = new GameObject("Scenario Runtime");
        var registry = runtimeObject.AddComponent<SceneBindingRegistry>();
        var context = runtimeObject.AddComponent<ScenarioSceneContext>();
        var physicsDriver = runtimeObject.AddComponent<DemoPhysicsDriver>();
        var view = runtimeObject.AddComponent<DemoScenarioView>();
        var journal = runtimeObject.AddComponent<DemoJournalController>();
        var worldState = runtimeObject.AddComponent<DemoWorldStateController>();
        var controller = runtimeObject.AddComponent<DemoScenarioController>();

        var bindings = new List<SceneBindingBehaviour>();
        BuildGameplayObjects(scenario, physicsDriver, ui.Canvas.transform, bindings);

        ConfigureView(view, ui);
        ConfigureJournal(journal, scenario, ui);
        ConfigureWorldState(worldState, player, playerStart);
        ConfigureRuntime(context, registry, controller, scenario, view, journal, physicsDriver, worldState);
        ConfigureControlButtons(controller, journal, ui);
        SetObjectList(registry, "_bindings", bindings);

        Selection.activeGameObject = player.gameObject;
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, ScenePath))
        {
            throw new InvalidOperationException("Unity could not save the gameplay demo scene.");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return $"Built {ScenePath} with {bindings.Count} scenario bindings and 12 world steps.";
    }

    private static void BuildGameplayObjects(
        ScenarioDefinition scenario,
        DemoPhysicsDriver physicsDriver,
        Transform canvas,
        List<SceneBindingBehaviour> bindings)
    {
        var roots = new GameObject("Gameplay Stations").transform;

        var step1Button = CreateActionProxy(canvas, "Step 01 Button Proxy", "step-01.button", "step-01", bindings);
        var step12Button = CreateActionProxy(canvas, "Step 12 Button Proxy", "step-12.button", "step-12", bindings);

        var step1 = CreateStation(
            roots,
            "01 Briefing Terminal",
            new Vector3(-10f, 0f, -6.5f),
            "01\nBRIEFING",
            "Confirm briefing",
            scenario.Steps[0].StepId,
            null,
            null,
            null,
            bindings);
        SetObject(step1.Interactable, "_buttonAction", step1Button);
        AddScreen(step1.Root.transform, new Color(0.15f, 0.65f, 1f));

        CreateTriggerZone(
            roots,
            "02 Work Zone",
            new Vector3(-7f, 0.8f, -2.5f),
            new Vector3(4.2f, 1.6f, 3.2f),
            "02\nWORK ZONE",
            scenario.Steps[1].StepId,
            "step-02.trigger",
            "step-02",
            bindings);

        CreateStation(
            roots,
            "03 Sequence A",
            new Vector3(-6.5f, 0f, 3.5f),
            "03-A\nCHECK A",
            "Run check A first",
            scenario.Steps[2].StepId,
            "step-03.sequence-a",
            "step-03",
            ScenarioActionTypes.UnityEventInvoked,
            bindings,
            true);
        CreateStation(
            roots,
            "03 Sequence B",
            new Vector3(-6f, 0f, 6.5f),
            "03-B\nCHECK B",
            "Run check B after A",
            scenario.Steps[2].StepId,
            "step-03.sequence-b",
            "step-03",
            ScenarioActionTypes.UnityEventInvoked,
            bindings,
            true);

        var safety = CreateStation(
            roots,
            "04 Safety Switch",
            new Vector3(-1.5f, 0f, 6.5f),
            "04-A\nSAFETY",
            "Enable safety first",
            scenario.Steps[3].StepId,
            null,
            null,
            null,
            bindings,
            true);
        var safetyState = safety.Root.AddComponent<SceneStateBinding>();
        SetString(safetyState, "_bindingId", "step-04.safety-state");
        SetString(safetyState, "_initialValue", bool.FalseString);
        bindings.Add(safetyState);
        AddPersistentListener(safety.Interactable, safetyState.SetTrue);

        var launch = CreateStation(
            roots,
            "04 Launch Lever",
            new Vector3(2f, 0f, 6.5f),
            "04-B\nLAUNCH",
            "Launch after safety",
            scenario.Steps[3].StepId,
            null,
            null,
            null,
            bindings,
            true);
        AddPersistentListener(launch.Interactable, physicsDriver.LaunchCollisionProbe);

        var collisionStart = CreateAnchor("Collision Start", new Vector3(3.5f, 0.65f, 6.5f), roots);
        var collisionTarget = CreateAnchor("Collision Target", new Vector3(8f, 0.65f, 6.5f), roots);
        var probe = CreatePrimitive(
            PrimitiveType.Sphere,
            "Collision Probe",
            collisionStart.position,
            new Vector3(0.9f, 0.9f, 0.9f),
            _redMaterial,
            roots);
        var probeBody = probe.AddComponent<Rigidbody>();
        probeBody.mass = 1.25f;
        probeBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        var wall = CreatePrimitive(
            PrimitiveType.Cube,
            "Collision Target Wall",
            new Vector3(8.5f, 0.8f, 6.5f),
            new Vector3(0.7f, 1.6f, 3f),
            _redMaterial,
            roots);
        var collisionSource = wall.AddComponent<CollisionActionSource>();
        SetString(collisionSource, "_bindingId", "step-04.collision");
        SetString(collisionSource, "_targetId", "step-04");
        bindings.Add(collisionSource);
        AddMarker(wall, scenario.Steps[3].StepId, wall.GetComponent<Renderer>());

        SetObject(physicsDriver, "_collisionProbe", probeBody);
        SetObject(physicsDriver, "_collisionStart", collisionStart);
        SetObject(physicsDriver, "_collisionTarget", collisionTarget);
        SetFloat(physicsDriver, "_collisionSpeed", 9f);

        CreateStation(roots, "05 Equipment Rack", new Vector3(-3f, 0f, 1f), "05\nEQUIPMENT", "Select equipment", scenario.Steps[4].StepId, "step-05.button", "step-05", ScenarioActionTypes.ButtonClicked, bindings);
        CreateStation(roots, "06 Pressure Gauge", new Vector3(1f, 0f, 2f), "06\nPRESSURE", "Check pressure", scenario.Steps[5].StepId, "step-06.button", "step-06", ScenarioActionTypes.ButtonClicked, bindings);
        CreateStation(roots, "07 Inlet Valve", new Vector3(5f, 0f, 2f), "07\nINLET VALVE", "Open inlet valve", scenario.Steps[6].StepId, "step-07.button", "step-07", ScenarioActionTypes.ButtonClicked, bindings, true);
        CreateStation(roots, "08 Pump Start", new Vector3(9.5f, 0f, 1.5f), "08\nSTART PUMP", "Start circulation pump", scenario.Steps[7].StepId, "step-08.button", "step-08", ScenarioActionTypes.ButtonClicked, bindings, true);
        CreateStation(roots, "09 Flow Meter", new Vector3(2.5f, 0f, -2.5f), "09\nFLOW METER", "Check flow reading", scenario.Steps[8].StepId, "step-09.button", "step-09", ScenarioActionTypes.ButtonClicked, bindings);
        CreateStation(roots, "10 Temperature", new Vector3(7f, 0f, -2.5f), "10\nTEMPERATURE", "Confirm temperature", scenario.Steps[9].StepId, "step-10.button", "step-10", ScenarioActionTypes.ButtonClicked, bindings);
        CreateStation(roots, "11 Pump Stop", new Vector3(4.5f, 0f, -6.5f), "11\nSTOP PUMP", "Stop circulation pump", scenario.Steps[10].StepId, "step-11.button", "step-11", ScenarioActionTypes.ButtonClicked, bindings, true, true);

        var step12 = CreateStation(
            roots,
            "12 Report Terminal",
            new Vector3(10f, 0f, -6.5f),
            "12\nREPORT",
            "Complete final report",
            scenario.Steps[11].StepId,
            null,
            null,
            null,
            bindings);
        SetObject(step12.Interactable, "_buttonAction", step12Button);
        AddScreen(step12.Root.transform, new Color(0.2f, 1f, 0.45f));

        CreateDecorativeGate(roots, new Vector3(11.5f, 0f, -3.5f));
    }

    private static Station CreateStation(
        Transform parent,
        string name,
        Vector3 position,
        string label,
        string prompt,
        string stepId,
        string bindingId,
        string targetId,
        string actionType,
        List<SceneBindingBehaviour> bindings,
        bool addLever = false,
        bool redIndicator = false)
    {
        var root = new GameObject(name);
        root.transform.SetParent(parent);
        root.transform.position = position;

        CreatePrimitive(
            PrimitiveType.Cube,
            "Pedestal",
            position + new Vector3(0f, 0.45f, 0f),
            new Vector3(1.7f, 0.9f, 1.7f),
            _baseMaterial,
            root.transform);
        var indicatorObject = CreatePrimitive(
            addLever ? PrimitiveType.Cylinder : PrimitiveType.Cube,
            "Indicator",
            position + new Vector3(0f, addLever ? 1.05f : 0.98f, 0f),
            addLever ? new Vector3(0.45f, 0.18f, 0.45f) : new Vector3(1.25f, 0.16f, 1.25f),
            redIndicator ? _redMaterial : _indicatorMaterial,
            root.transform);

        Transform visualPivot = null;
        if (addLever)
        {
            var pivotObject = new GameObject("Lever Pivot");
            pivotObject.transform.SetParent(root.transform, false);
            pivotObject.transform.localPosition = new Vector3(0f, 1.12f, 0f);
            pivotObject.transform.localRotation = Quaternion.Euler(0f, 0f, -30f);

            var shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shaft.name = "Lever Shaft";
            shaft.transform.SetParent(pivotObject.transform, false);
            shaft.transform.localPosition = new Vector3(0f, 0.65f, 0f);
            shaft.transform.localScale = new Vector3(0.12f, 0.65f, 0.12f);
            shaft.GetComponent<Renderer>().sharedMaterial = _wallMaterial;
            UnityEngine.Object.DestroyImmediate(shaft.GetComponent<Collider>());

            var knob = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            knob.name = "Lever Knob";
            knob.transform.SetParent(pivotObject.transform, false);
            knob.transform.localPosition = new Vector3(0f, 1.32f, 0f);
            knob.transform.localScale = Vector3.one * 0.36f;
            knob.GetComponent<Renderer>().sharedMaterial = _redMaterial;
            UnityEngine.Object.DestroyImmediate(knob.GetComponent<Collider>());

            visualPivot = pivotObject.transform;
        }

        var interactable = root.AddComponent<WorldInteractable>();
        SetString(interactable, "_prompt", prompt);
        if (visualPivot)
        {
            SetObject(interactable, "_visualPivot", visualPivot);
            SetVector3(interactable, "_usedLocalEulerAngles", new Vector3(0f, 0f, 30f));
        }

        WorldActionSource source = null;
        if (!string.IsNullOrWhiteSpace(bindingId))
        {
            source = root.AddComponent<WorldActionSource>();
            SetString(source, "_bindingId", bindingId);
            SetString(source, "_targetId", targetId);
            SetString(source, "_actionType", actionType);
            SetObject(interactable, "_actionSource", source);
            bindings.Add(source);
        }

        var indicator = indicatorObject.GetComponent<Renderer>();
        AddMarker(root, stepId, indicator);
        CreateWorldLabel(root.transform, label, new Vector3(0f, 2.25f, 0f));
        return new Station
        {
            Root = root,
            Interactable = interactable,
            Source = source,
            Indicator = indicator,
            VisualPivot = visualPivot
        };
    }

    private static void CreateTriggerZone(
        Transform parent,
        string name,
        Vector3 position,
        Vector3 scale,
        string label,
        string stepId,
        string bindingId,
        string targetId,
        List<SceneBindingBehaviour> bindings)
    {
        var zone = CreatePrimitive(
            PrimitiveType.Cube,
            name,
            position,
            scale,
            _blueZoneMaterial,
            parent);
        var collider = zone.GetComponent<BoxCollider>();
        collider.isTrigger = true;
        var source = zone.AddComponent<TriggerActionSource>();
        SetString(source, "_bindingId", bindingId);
        SetString(source, "_targetId", targetId);
        bindings.Add(source);
        AddMarker(zone, stepId, zone.GetComponent<Renderer>());
        CreateWorldLabel(zone.transform, label, new Vector3(0f, 0.78f, 0f));
    }

    private static void AddMarker(GameObject target, string stepId, params Renderer[] renderers)
    {
        var marker = target.AddComponent<WorldStepMarker>();
        SetString(marker, "_stepId", stepId);
        SetObjectArray(marker, "_renderers", renderers);
    }

    private static Button CreateActionProxy(
        Transform parent,
        string name,
        string bindingId,
        string targetId,
        List<SceneBindingBehaviour> bindings)
    {
        var proxy = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        proxy.transform.SetParent(parent, false);
        var rect = proxy.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.anchoredPosition = new Vector2(-2000f, -2000f);
        rect.sizeDelta = Vector2.one;
        proxy.GetComponent<Image>().color = Color.clear;
        var source = proxy.AddComponent<ButtonActionSource>();
        SetString(source, "_bindingId", bindingId);
        SetString(source, "_targetId", targetId);
        bindings.Add(source);
        return proxy.GetComponent<Button>();
    }

    private static void CreateEnvironment()
    {
        var environment = new GameObject("Training Arena").transform;
        CreatePrimitive(PrimitiveType.Cube, "Floor", new Vector3(0f, -0.3f, 0f), new Vector3(28f, 0.6f, 20f), _floorMaterial, environment);

        CreateWall(environment, "North Wall", new Vector3(0f, 1f, 9.7f), new Vector3(28f, 2f, 0.6f));
        CreateWall(environment, "South Wall", new Vector3(0f, 1f, -9.7f), new Vector3(28f, 2f, 0.6f));
        CreateWall(environment, "West Wall", new Vector3(-13.7f, 1f, 0f), new Vector3(0.6f, 2f, 20f));
        CreateWall(environment, "East Wall", new Vector3(13.7f, 1f, 0f), new Vector3(0.6f, 2f, 20f));

        CreateWall(environment, "Divider NW", new Vector3(-8.8f, 0.7f, 1.3f), new Vector3(4.5f, 1.4f, 0.45f));
        CreateWall(environment, "Divider Center", new Vector3(0f, 0.7f, 4f), new Vector3(0.45f, 1.4f, 3.5f));
        CreateWall(environment, "Divider East", new Vector3(10.8f, 0.7f, -1f), new Vector3(0.45f, 1.4f, 4f));
        CreateWall(environment, "Divider South", new Vector3(-7.5f, 0.7f, -6f), new Vector3(3f, 1.4f, 0.45f));

        for (var x = -12; x <= 12; x += 2)
        {
            var line = CreatePrimitive(PrimitiveType.Cube, $"Floor Line X {x}", new Vector3(x, 0.015f, 0f), new Vector3(0.025f, 0.02f, 18.8f), _wallMaterial, environment);
            UnityEngine.Object.DestroyImmediate(line.GetComponent<Collider>());
        }

        for (var z = -8; z <= 8; z += 2)
        {
            var line = CreatePrimitive(PrimitiveType.Cube, $"Floor Line Z {z}", new Vector3(0f, 0.015f, z), new Vector3(26.8f, 0.02f, 0.025f), _wallMaterial, environment);
            UnityEngine.Object.DestroyImmediate(line.GetComponent<Collider>());
        }
    }

    private static Camera CreateCameraAndLighting()
    {
        var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 20f, -19f);
        cameraObject.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 0.45f, 0.5f) - cameraObject.transform.position);
        _camera = cameraObject.GetComponent<Camera>();
        _camera.orthographic = false;
        _camera.fieldOfView = 35f;
        _camera.nearClipPlane = 0.1f;
        _camera.farClipPlane = 100f;
        _camera.clearFlags = CameraClearFlags.SolidColor;
        _camera.backgroundColor = new Color(0.055f, 0.065f, 0.09f);
        _camera.allowHDR = true;

        var lightObject = new GameObject("Directional Light", typeof(Light));
        lightObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
        var light = lightObject.GetComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.35f;
        light.color = new Color(0.92f, 0.95f, 1f);
        light.shadows = LightShadows.Soft;

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.28f, 0.31f, 0.4f);
        RenderSettings.ambientEquatorColor = new Color(0.17f, 0.19f, 0.25f);
        RenderSettings.ambientGroundColor = new Color(0.08f, 0.09f, 0.12f);
        return _camera;
    }

    private static TopDownPlayerController CreatePlayer(Text promptText)
    {
        var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.transform.position = new Vector3(0f, 1f, -7f);
        player.transform.localScale = new Vector3(0.85f, 1f, 0.85f);
        UnityEngine.Object.DestroyImmediate(player.GetComponent<CapsuleCollider>());
        player.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial(
            MaterialFolder + "/Player.mat",
            new Color(0.92f, 0.94f, 1f),
            true);

        var controller = player.AddComponent<CharacterController>();
        controller.height = 2f;
        controller.radius = 0.48f;
        controller.center = Vector3.zero;
        controller.stepOffset = 0.35f;
        var movement = player.AddComponent<TopDownPlayerController>();
        var interactor = player.AddComponent<WorldInteractor>();
        SetObject(interactor, "_promptText", promptText);

        var visor = CreatePrimitive(
            PrimitiveType.Cube,
            "Visor",
            player.transform.position + new Vector3(0f, 0.35f, 0.43f),
            new Vector3(0.52f, 0.22f, 0.08f),
            _indicatorMaterial,
            player.transform);
        UnityEngine.Object.DestroyImmediate(visor.GetComponent<Collider>());
        return movement;
    }

    private static Transform CreatePlayerStart(Vector3 position, Quaternion rotation)
    {
        var start = new GameObject("Player Start").transform;
        start.SetPositionAndRotation(position, rotation);
        return start;
    }

    private static void CreateEventSystem()
    {
        var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        eventSystem.GetComponent<EventSystem>().sendNavigationEvents = true;
    }

    private sealed class HudReferences
    {
        public Canvas Canvas;
        public Text ModeText;
        public Text ActiveStepsText;
        public Text StatusText;
        public Text MessageText;
        public Text GroupText;
        public Text ResultText;
        public Text DiagnosticsText;
        public Text PromptText;
        public Dropdown JournalDropdown;
        public Dropdown AnalyzerDropdown;
        public Text JournalText;
        public Text ReanalysisText;
        public Text StoragePathText;
        public Button TrainingButton;
        public Button ExamButton;
        public Button RestartButton;
        public Button JournalButton;
        public Button RefreshJournalButton;
        public Button OpenJournalButton;
        public Button ReanalyzeButton;
        public Button CloseJournalButton;
        public GameObject JournalPanel;
        public PanelToggle PanelToggle;
    }

    private static HudReferences CreateHud()
    {
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var canvasObject = new GameObject("Gameplay HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.matchWidthOrHeight = 0.5f;

        var topLeft = CreatePanel(canvas.transform, "Active Steps", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -16f), new Vector2(420f, 320f), new Color(0.02f, 0.025f, 0.04f, 0.88f));
        CreateText(topLeft.transform, "Title", "CURRENT OBJECTIVES", 23, TextAnchor.UpperLeft, FontStyle.Bold, new Vector2(18f, -14f), new Vector2(384f, 36f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
        var mode = CreateText(topLeft.transform, "Mode", "Mode: not started", 16, TextAnchor.UpperLeft, FontStyle.Normal, new Vector2(18f, -52f), new Vector2(384f, 26f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
        var active = CreateText(topLeft.transform, "Tasks", "Choose Training or Exam to begin.", 15, TextAnchor.UpperLeft, FontStyle.Normal, new Vector2(18f, -84f), new Vector2(384f, 218f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));

        var topRight = CreatePanel(canvas.transform, "Progress", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-16f, -16f), new Vector2(300f, 145f), new Color(0.02f, 0.025f, 0.04f, 0.86f));
        var status = CreateText(topRight.transform, "Status", "0/12", 23, TextAnchor.UpperRight, FontStyle.Bold, new Vector2(-14f, -10f), new Vector2(272f, 50f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f));
        var group = CreateText(topRight.transform, "Group", "Group: —", 15, TextAnchor.UpperRight, FontStyle.Normal, new Vector2(-14f, -58f), new Vector2(272f, 22f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f));
        var result = CreateText(topRight.transform, "Result", "Result: —", 15, TextAnchor.UpperRight, FontStyle.Normal, new Vector2(-14f, -83f), new Vector2(272f, 48f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f));

        var controls = CreatePanel(canvas.transform, "Controls", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(570f, 52f), new Color(0.02f, 0.025f, 0.04f, 0.82f));
        var trainingButton = CreateButton(controls.transform, "Training", "TRAINING", new Vector2(-207f, 0f), new Vector2(126f, 36f), new Color(0.12f, 0.42f, 0.72f, 0.95f));
        var examButton = CreateButton(controls.transform, "Exam", "EXAM", new Vector2(-69f, 0f), new Vector2(126f, 36f), new Color(0.68f, 0.25f, 0.16f, 0.95f));
        var restartButton = CreateButton(controls.transform, "Restart", "RESTART", new Vector2(69f, 0f), new Vector2(126f, 36f), new Color(0.28f, 0.3f, 0.36f, 0.95f));
        var journalButton = CreateButton(controls.transform, "Journal", "JOURNAL", new Vector2(207f, 0f), new Vector2(126f, 36f), new Color(0.25f, 0.3f, 0.42f, 0.95f));

        var promptPanel = CreatePanel(canvas.transform, "Prompt", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(520f, 48f), new Color(0.02f, 0.025f, 0.04f, 0.88f));
        var prompt = CreateText(promptPanel.transform, "Prompt Text", "WASD — MOVE   E — INTERACT", 18, TextAnchor.MiddleCenter, FontStyle.Bold, Vector2.zero, new Vector2(496f, 42f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

        var messagePanel = CreatePanel(canvas.transform, "Feedback", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(16f, 16f), new Vector2(500f, 70f), new Color(0.02f, 0.025f, 0.04f, 0.84f));
        var message = CreateText(messagePanel.transform, "Message", "Ready.", 16, TextAnchor.MiddleLeft, FontStyle.Normal, new Vector2(14f, 8f), new Vector2(472f, 54f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f));

        var journalPanel = CreatePanel(canvas.transform, "Journal Panel", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-20f, 0f), new Vector2(640f, 790f), new Color(0.018f, 0.022f, 0.035f, 0.97f));
        CreateText(journalPanel.transform, "Journal Title", "JOURNAL & REANALYSIS", 24, TextAnchor.MiddleLeft, FontStyle.Bold, new Vector2(20f, -20f), new Vector2(500f, 42f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
        var close = CreateButton(journalPanel.transform, "Close", "×", new Vector2(-28f, -28f), new Vector2(42f, 42f), new Color(0.45f, 0.16f, 0.18f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f));

        var journalDropdown = CreateDropdown(journalPanel.transform, "Completed Journals", new Vector2(20f, -82f), new Vector2(590f, 42f));
        var analyzerDropdown = CreateDropdown(journalPanel.transform, "Analyzer", new Vector2(20f, -134f), new Vector2(590f, 42f));
        var refresh = CreateButton(journalPanel.transform, "Refresh", "REFRESH", new Vector2(90f, -194f), new Vector2(150f, 38f), new Color(0.24f, 0.31f, 0.43f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 1f));
        var open = CreateButton(journalPanel.transform, "Open", "OPEN", new Vector2(255f, -194f), new Vector2(150f, 38f), new Color(0.24f, 0.31f, 0.43f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 1f));
        var reanalyze = CreateButton(journalPanel.transform, "Reanalyze", "REANALYZE", new Vector2(430f, -194f), new Vector2(170f, 38f), new Color(0.16f, 0.42f, 0.65f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 1f));
        var journalText = CreateText(journalPanel.transform, "Journal Text", "Events will appear after the scenario starts.", 15, TextAnchor.UpperLeft, FontStyle.Normal, new Vector2(20f, -235f), new Vector2(590f, 290f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
        var reanalysisText = CreateText(journalPanel.transform, "Reanalysis", "Reanalysis: —", 15, TextAnchor.UpperLeft, FontStyle.Normal, new Vector2(20f, -535f), new Vector2(590f, 115f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
        var diagnostics = CreateText(journalPanel.transform, "Diagnostics", "Diagnostics: —", 14, TextAnchor.UpperLeft, FontStyle.Normal, new Vector2(20f, -650f), new Vector2(590f, 70f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
        var storagePath = CreateText(journalPanel.transform, "Storage Path", "Journal directory: —", 13, TextAnchor.LowerLeft, FontStyle.Normal, new Vector2(20f, 18f), new Vector2(590f, 42f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f));

        var panelToggle = canvasObject.AddComponent<PanelToggle>();
        SetObject(panelToggle, "_panel", journalPanel);
        journalPanel.SetActive(false);

        return new HudReferences
        {
            Canvas = canvas,
            ModeText = mode,
            ActiveStepsText = active,
            StatusText = status,
            MessageText = message,
            GroupText = group,
            ResultText = result,
            DiagnosticsText = diagnostics,
            PromptText = prompt,
            JournalDropdown = journalDropdown,
            AnalyzerDropdown = analyzerDropdown,
            JournalText = journalText,
            ReanalysisText = reanalysisText,
            StoragePathText = storagePath,
            TrainingButton = trainingButton,
            ExamButton = examButton,
            RestartButton = restartButton,
            JournalButton = journalButton,
            RefreshJournalButton = refresh,
            OpenJournalButton = open,
            ReanalyzeButton = reanalyze,
            CloseJournalButton = close,
            JournalPanel = journalPanel,
            PanelToggle = panelToggle
        };
    }

    private static void ConfigureView(DemoScenarioView view, HudReferences ui)
    {
        SetObject(view, "_modeText", ui.ModeText);
        SetObject(view, "_activeStepsText", ui.ActiveStepsText);
        SetObject(view, "_stepStatusesText", ui.StatusText);
        SetObject(view, "_messageText", ui.MessageText);
        SetObject(view, "_groupText", ui.GroupText);
        SetObject(view, "_resultText", ui.ResultText);
        SetObject(view, "_diagnosticsText", ui.DiagnosticsText);
    }

    private static void ConfigureJournal(DemoJournalController journal, ScenarioDefinition scenario, HudReferences ui)
    {
        SetObject(journal, "_scenario", scenario);
        var analyzers = new List<AnalyzerProvider>
        {
            AssetDatabase.LoadAssetAtPath<AnalyzerProvider>(TrainingAnalyzerPath),
            AssetDatabase.LoadAssetAtPath<AnalyzerProvider>(ExamAnalyzerPath)
        };
        SetObjectList(journal, "_analyzerProviders", analyzers);
        SetObject(journal, "_journalDropdown", ui.JournalDropdown);
        SetObject(journal, "_analyzerDropdown", ui.AnalyzerDropdown);
        SetObject(journal, "_journalText", ui.JournalText);
        SetObject(journal, "_reanalysisText", ui.ReanalysisText);
        SetObject(journal, "_storagePathText", ui.StoragePathText);
    }

    private static void ConfigureWorldState(
        DemoWorldStateController worldState,
        TopDownPlayerController player,
        Transform playerStart)
    {
        SetObject(worldState, "_player", player);
        SetObject(worldState, "_playerStart", playerStart);
    }

    private static void ConfigureRuntime(
        ScenarioSceneContext context,
        SceneBindingRegistry registry,
        DemoScenarioController controller,
        ScenarioDefinition scenario,
        DemoScenarioView view,
        DemoJournalController journal,
        DemoPhysicsDriver physicsDriver,
        DemoWorldStateController worldState)
    {
        SetObject(context, "_scenario", scenario);
        SetObject(context, "_sceneBindings", registry);
        SetObject(controller, "_sceneContext", context);
        SetObject(controller, "_scenario", scenario);
        SetObject(controller, "_view", view);
        SetObject(controller, "_journalController", journal);
        SetObject(controller, "_physicsDriver", physicsDriver);
        SetObject(controller, "_worldState", worldState);
    }

    private static void ConfigureControlButtons(
        DemoScenarioController controller,
        DemoJournalController journal,
        HudReferences ui)
    {
        UnityEventTools.AddPersistentListener(ui.TrainingButton.onClick, controller.StartTraining);
        UnityEventTools.AddPersistentListener(ui.ExamButton.onClick, controller.StartExam);
        UnityEventTools.AddPersistentListener(ui.RestartButton.onClick, controller.RestartScenario);
        UnityEventTools.AddPersistentListener(ui.JournalButton.onClick, ui.PanelToggle.Toggle);
        UnityEventTools.AddPersistentListener(ui.CloseJournalButton.onClick, ui.PanelToggle.Close);
        UnityEventTools.AddPersistentListener(ui.RefreshJournalButton.onClick, journal.RefreshJournalFiles);
        UnityEventTools.AddPersistentListener(ui.OpenJournalButton.onClick, journal.OpenSelectedJournal);
        UnityEventTools.AddPersistentListener(ui.ReanalyzeButton.onClick, journal.ReanalyzeSelectedJournal);
    }

    private static void AddPersistentListener(WorldInteractable interactable, UnityAction action)
    {
        var field = typeof(WorldInteractable).GetField("_onInteract", BindingFlags.Instance | BindingFlags.NonPublic);
        var interactionEvent = (UnityEvent)field.GetValue(interactable);
        UnityEventTools.AddPersistentListener(interactionEvent, action);
        EditorUtility.SetDirty(interactable);
    }

    private static void UpdateScenarioLabels(ScenarioDefinition scenario)
    {
        var names = new[]
        {
            "01 — Confirm briefing",
            "02 — Enter the work zone",
            "03 — Perform ordered check A -> B",
            "04 — Enable safety and launch",
            "05 — Select equipment",
            "06 — Check pressure",
            "07 — Open the inlet valve",
            "08 — Start the circulation pump",
            "09 — Check flow",
            "10 — Confirm temperature",
            "11 — Stop the circulation pump",
            "12 — Complete the final report"
        };
        var instructions = new[]
        {
            "Go to the BRIEFING terminal and press E.",
            "Walk into the highlighted WORK ZONE.",
            "Activate CHECK A first, then activate CHECK B. Reversing the order counts as a violation.",
            "Activate SAFETY first, then activate LAUNCH. The probe collision completes the step.",
            "Go to the EQUIPMENT station and press E.",
            "Go to the PRESSURE station and confirm the reading.",
            "Open the INLET VALVE.",
            "Start the circulation pump.",
            "Confirm the FLOW METER reading.",
            "Confirm the TEMPERATURE reading.",
            "Stop the circulation pump.",
            "Go to the REPORT terminal and complete the final report."
        };

        for (var index = 0; index < scenario.Steps.Count; index++)
        {
            var serializedStep = new SerializedObject(scenario.Steps[index]);
            serializedStep.FindProperty("_displayName").stringValue = names[index];
            serializedStep.FindProperty("_instruction").stringValue = instructions[index];
            serializedStep.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(scenario.Steps[index]);
        }

        var serializedScenario = new SerializedObject(scenario);
        serializedScenario.FindProperty("_displayName").stringValue = "12-Step Training Facility";
        serializedScenario.FindProperty("_description").stringValue = "A playable 12-step training scenario with ordered actions, guarded actions, physics events, Training mode, and Exam mode.";
        serializedScenario.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(scenario);
    }

    private static void EnsureMaterials()
    {
        EnsureFolder(MaterialFolder);
        _floorMaterial = GetOrCreateMaterial(MaterialFolder + "/Floor.mat", new Color(0.18f, 0.2f, 0.25f), false);
        _wallMaterial = GetOrCreateMaterial(MaterialFolder + "/Walls.mat", new Color(0.32f, 0.34f, 0.42f), false);
        _baseMaterial = GetOrCreateMaterial(MaterialFolder + "/Pedestals.mat", new Color(0.23f, 0.25f, 0.31f), false);
        _indicatorMaterial = GetOrCreateMaterial(MaterialFolder + "/Indicator.mat", new Color(0.08f, 0.46f, 1f), true);
        _redMaterial = GetOrCreateMaterial(MaterialFolder + "/Danger.mat", new Color(0.95f, 0.12f, 0.16f), true);
        _greenMaterial = GetOrCreateMaterial(MaterialFolder + "/Success.mat", new Color(0.1f, 0.9f, 0.32f), true);
        _blueZoneMaterial = GetOrCreateMaterial(MaterialFolder + "/TriggerZone.mat", new Color(0.08f, 0.38f, 1f, 0.34f), true, true);
    }

    private static Material GetOrCreateMaterial(string path, Color color, bool emission, bool transparent = false)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        if (!material)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        material.shader = shader;
        material.SetColor("_BaseColor", color);
        material.SetColor("_Color", color);
        material.SetFloat("_Smoothness", 0.48f);

        if (emission)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 1.4f);
        }

        if (transparent)
        {
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
        }
        else
        {
            material.SetFloat("_Surface", 0f);
            material.SetFloat("_ZWrite", 1f);
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = -1;
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    private static void EnsureFolder(string folder)
    {
        var parts = folder.Split('/');
        var current = parts[0];
        for (var index = 1; index < parts.Length; index++)
        {
            var next = current + "/" + parts[index];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[index]);
            }
            current = next;
        }
    }

    private static GameObject CreatePrimitive(
        PrimitiveType type,
        string name,
        Vector3 position,
        Vector3 scale,
        Material material,
        Transform parent)
    {
        var gameObject = GameObject.CreatePrimitive(type);
        gameObject.name = name;
        gameObject.transform.SetParent(parent);
        gameObject.transform.position = position;
        gameObject.transform.localScale = scale;
        var renderer = gameObject.GetComponent<Renderer>();
        if (renderer)
        {
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }
        return gameObject;
    }

    private static void CreateWall(Transform parent, string name, Vector3 position, Vector3 scale)
    {
        CreatePrimitive(PrimitiveType.Cube, name, position, scale, _wallMaterial, parent);
    }

    private static Transform CreateAnchor(string name, Vector3 position, Transform parent)
    {
        var anchor = new GameObject(name).transform;
        anchor.SetParent(parent);
        anchor.position = position;
        return anchor;
    }

    private static void CreateWorldLabel(Transform parent, string text, Vector3 localPosition)
    {
        var labelObject = new GameObject("Label", typeof(TextMesh));
        var worldPosition = parent.position + localPosition;
        var labelParent = parent.lossyScale == Vector3.one ? parent : parent.parent;
        labelObject.transform.SetParent(labelParent, true);
        labelObject.transform.position = worldPosition;
        labelObject.transform.rotation = _camera.transform.rotation;
        labelObject.transform.localScale = Vector3.one;
        var textMesh = labelObject.GetComponent<TextMesh>();
        textMesh.text = text;
        textMesh.font = _font ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textMesh.fontSize = 52;
        textMesh.characterSize = 0.065f;
        textMesh.lineSpacing = 0.8f;
        textMesh.fontStyle = FontStyle.Bold;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.color = new Color(0.9f, 0.94f, 1f);
        var renderer = textMesh.GetComponent<MeshRenderer>();
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private static void AddScreen(Transform parent, Color color)
    {
        var screen = CreatePrimitive(
            PrimitiveType.Cube,
            "Terminal Screen",
            parent.position + new Vector3(0f, 1.45f, 0.48f),
            new Vector3(0.9f, 0.55f, 0.08f),
            GetOrCreateMaterial(MaterialFolder + "/TerminalScreen.mat", color, true),
            parent);
        UnityEngine.Object.DestroyImmediate(screen.GetComponent<Collider>());
    }

    private static void CreateDecorativeGate(Transform parent, Vector3 position)
    {
        CreatePrimitive(PrimitiveType.Cube, "Gate Left", position + new Vector3(-1.5f, 1f, 0f), new Vector3(0.5f, 2f, 0.7f), _wallMaterial, parent);
        CreatePrimitive(PrimitiveType.Cube, "Gate Right", position + new Vector3(1.5f, 1f, 0f), new Vector3(0.5f, 2f, 0.7f), _wallMaterial, parent);
        var gate = CreatePrimitive(PrimitiveType.Cube, "Completion Gate", position + new Vector3(0f, 1f, 0f), new Vector3(2.5f, 1.4f, 0.18f), _greenMaterial, parent);
        UnityEngine.Object.DestroyImmediate(gate.GetComponent<Collider>());
    }

    private static GameObject CreatePanel(
        Transform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 size,
        Color color)
    {
        var panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(parent, false);
        ConfigureRect(panel.GetComponent<RectTransform>(), anchorMin, anchorMax, pivot, anchoredPosition, size);
        panel.GetComponent<Image>().color = color;
        return panel;
    }

    private static Text CreateText(
        Transform parent,
        string name,
        string value,
        int fontSize,
        TextAnchor alignment,
        FontStyle fontStyle,
        Vector2 anchoredPosition,
        Vector2 size,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot)
    {
        var textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObject.transform.SetParent(parent, false);
        ConfigureRect(textObject.GetComponent<RectTransform>(), anchorMin, anchorMax, pivot, anchoredPosition, size);
        var text = textObject.GetComponent<Text>();
        text.font = _font;
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.color = new Color(0.94f, 0.96f, 1f);
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private static Button CreateButton(
        Transform parent,
        string name,
        string label,
        Vector2 anchoredPosition,
        Vector2 size,
        Color color,
        Vector2? anchorMin = null,
        Vector2? anchorMax = null,
        Vector2? pivot = null)
    {
        var buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        ConfigureRect(
            buttonObject.GetComponent<RectTransform>(),
            anchorMin ?? new Vector2(0.5f, 0.5f),
            anchorMax ?? new Vector2(0.5f, 0.5f),
            pivot ?? new Vector2(0.5f, 0.5f),
            anchoredPosition,
            size);
        buttonObject.GetComponent<Image>().color = color;
        var button = buttonObject.GetComponent<Button>();
        var colors = button.colors;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.18f);
        colors.pressedColor = Color.Lerp(color, Color.black, 0.2f);
        button.colors = colors;
        CreateText(buttonObject.transform, "Text", label, 17, TextAnchor.MiddleCenter, FontStyle.Bold, Vector2.zero, size - new Vector2(8f, 6f), Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f);
        return button;
    }

    private static Dropdown CreateDropdown(Transform parent, string name, Vector2 anchoredPosition, Vector2 size)
    {
        var resources = new DefaultControls.Resources
        {
            standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
            background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
            inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd"),
            knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
            checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"),
            dropdown = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd"),
            mask = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd")
        };
        var dropdownObject = DefaultControls.CreateDropdown(resources);
        dropdownObject.name = name;
        dropdownObject.transform.SetParent(parent, false);
        ConfigureRect(dropdownObject.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), anchoredPosition, size);
        foreach (var text in dropdownObject.GetComponentsInChildren<Text>(true))
        {
            text.font = _font;
            text.fontSize = 15;
        }
        return dropdownObject.GetComponent<Dropdown>();
    }

    private static void ConfigureRect(
        RectTransform rect,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 size)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
    }

    private static void SetObject(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
    {
        var serialized = new SerializedObject(target);
        var property = serialized.FindProperty(propertyName) ?? throw new InvalidOperationException($"Missing property {target.GetType().Name}.{propertyName}");
        property.objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static void SetString(UnityEngine.Object target, string propertyName, string value)
    {
        var serialized = new SerializedObject(target);
        var property = serialized.FindProperty(propertyName) ?? throw new InvalidOperationException($"Missing property {target.GetType().Name}.{propertyName}");
        property.stringValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static void SetFloat(UnityEngine.Object target, string propertyName, float value)
    {
        var serialized = new SerializedObject(target);
        var property = serialized.FindProperty(propertyName) ?? throw new InvalidOperationException($"Missing property {target.GetType().Name}.{propertyName}");
        property.floatValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static void SetVector3(UnityEngine.Object target, string propertyName, Vector3 value)
    {
        var serialized = new SerializedObject(target);
        var property = serialized.FindProperty(propertyName) ?? throw new InvalidOperationException($"Missing property {target.GetType().Name}.{propertyName}");
        property.vector3Value = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static void SetObjectArray<T>(UnityEngine.Object target, string propertyName, IReadOnlyList<T> values)
        where T : UnityEngine.Object
    {
        var serialized = new SerializedObject(target);
        var property = serialized.FindProperty(propertyName) ?? throw new InvalidOperationException($"Missing property {target.GetType().Name}.{propertyName}");
        property.arraySize = values.Count;
        for (var index = 0; index < values.Count; index++)
        {
            property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static void SetObjectList<T>(UnityEngine.Object target, string propertyName, IReadOnlyList<T> values)
        where T : UnityEngine.Object
    {
        SetObjectArray(target, propertyName, values);
    }
}
