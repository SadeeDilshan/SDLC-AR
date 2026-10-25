using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Full Figma-Matched 5-Scene Engine + Procedural 3D Stage Builder.
/// Supports direct Tab Bar selection, Guide Overlay, and dynamic 3D props per scene:
/// 0 = Waterfall, 1 = Agile Scrum, 2 = Kanban WIP, 3 = CI/CD Pipeline, 4 = W vs A (60%).
/// </summary>
public class StepThroughEngine : MonoBehaviour
{
    [Header("3D Scene Objects")]
    public Transform workItemOrb;
    public MeshRenderer[] phaseBlocks;
    public Material matBluePhase;      // #0072B2
    public Material matOrangeActive;   // #E69F00
    public Material matRedError;       // #D55E00

    [Header("UI Text References (Match Figma HUD)")]
    public TMP_Text codeEngineText;
    public TMP_Text narrationSubtitleText;
    public TMP_Text stepCounterText;
    public TMP_Text reworkCostText;
    public TMP_Text currentPhaseText;
    public TMP_Text modeButtonText;
    public TMP_Text objectiveText;

    [Header("Figma Top Tab Bar & Overlays (Auto-Wired)")]
    public Image[] sceneTabBackgrounds;
    public GameObject quizPanel;
    public GameObject guidePanel;

    [Header("AR Stage Root Reference")]
    public Transform sdlcStageRoot;

    [Header("Audio Narration (Optional)")]
    public AudioSource audioSource;
    public AudioClip[] stepNarrations;
    public AudioClip errorBuzzClip;
    public bool isVoiceMuted = false;

    private int currentSceneMode = 0; // 0..4
    private int currentStep = 0;      // 0..4
    private int reworkCost = 0;
    private bool hasAnomalyTriggered = false;

    // Container for procedural 3D props (pipes, Kanban cards, Scrum ring, base plate)
    private Transform propsContainer;
    private List<GameObject> spawnedProps = new List<GameObject>();
    private Material matDarkBoard;
    private Material matGreenPass;

    private readonly string[] modeButtonNames = {
        "Scene 1: Waterfall",
        "Scene 2: Agile Scrum",
        "Scene 3: Kanban WIP",
        "Scene 4: CI/CD Pipe",
        "Scene 5: W vs A (60%)"
    };

    private readonly string[] sceneObjectives = {
        "<color=#38BDF8>SCENE 1 · WATERFALL MODEL (UNDERSTAND & TRACE)</color>\nUnderstand why sequential phases cannot easily flow backward.",
        "<color=#38BDF8>SCENE 2 · AGILE SCRUM SPRINT CYCLE (APPLY)</color>\nIterative 2-week sprint loop adapts to change with minimal rework.",
        "<color=#38BDF8>SCENE 3 · KANBAN BOARD & WIP LIMITS (ANALYZE)</color>\nVisualize task cards in columns and prevent Work-In-Progress bottlenecks.",
        "<color=#38BDF8>SCENE 4 · CI/CD DEVOPS PIPELINE (EVALUATE)</color>\nAutomated build, test, and deployment gates catch bugs before production.",
        "<color=#38BDF8>SCENE 5 · WATERFALL VS AGILE AT 60% CHANGE</color>\nCompare rework spike (125h) vs. iterative sprint adaptation (15h)."
    };

    private readonly string[][] phaseLabelsPerMode = new string[][] {
        new string[] { "REQ", "DESIGN", "CODE", "TEST", "DEPLOY" },
        new string[] { "BACKLOG", "SPRINT", "CODE", "CI TEST", "REVIEW" },
        new string[] { "BACKLOG", "TO DO", "WIP [2/2]", "QA", "DONE" },
        new string[] { "COMMIT", "BUILD", "UNIT TEST", "STAGING", "PROD" },
        new string[] { "WF 60%", "WF 125h", "60% PIVOT", "AG 60%", "AG 15h" }
    };

    private readonly string[][] codeLinesPerMode = new string[][] {
        new string[] {
            "1: GatherRequirements(frozenSpec);",
            "2: DesignArchitecture(monolithDB);",
            "3: BuildAndCode(allModules);",
            "4: RunSystemTests(endOfProject);",
            "5: DeployRelease(production);"
        },
        new string[] {
            "1: PullSprintBacklog(storyPoints: 34);",
            "2: SprintPlanning(twoWeekTimebox);",
            "3: DailyScrumAndPairCode(branch);",
            "4: ContinuousIntegration(autoTest);",
            "5: SprintReviewAndRetro(increment);"
        },
        new string[] {
            "1: CardCreated(column: 'Backlog');",
            "2: PullTicket(column: 'Selected To-Do');",
            "3: EnforceWIPLimit(activeCards <= 2);",
            "4: PeerCodeReview(column: 'QA Verify');",
            "5: MoveCardToDone(leadTime: 2.4d);"
        },
        new string[] {
            "1: git push origin feat/ar-module",
            "2: job: build_container (Docker 24s)",
            "3: job: run_unit_tests (coverage 94%)",
            "4: job: deploy_staging (smoke_test OK)",
            "5: job: release_prod (blue_green_live)"
        },
        new string[] {
            "1: Waterfall.At60Percent(specLocked: true);",
            "2: Waterfall.ReworkSpike(cost: +125_hrs);",
            "3: EVENT: ClientChangesAuthAt60Percent();",
            "4: Agile.At60Percent(sprintsShipped: 3);",
            "5: Agile.NextSprintAdaptation(cost: +15_hrs);"
        }
    };

    private readonly string[][] narrationsPerMode = new string[][] {
        new string[] {
            "Step 1: Requirements. In Waterfall, all client requirements are locked upfront before any design or coding begins.",
            "Step 2: System Design. Complete database schemas and software architecture are frozen before writing code.",
            "Step 3: Implementation. Developers write the entire system monolithically based on the frozen design.",
            "Step 4: Integration & Testing. QA tests the whole system at the end. Late bugs discovered here are expensive!",
            "Step 5: Deployment. The finished software is released to the customer after months of sequential phases."
        },
        new string[] {
            "Sprint Step 1: Backlog Refinement. The team pulls top-priority User Stories (34 pts) into the 2-week Sprint.",
            "Sprint Step 2: Just-in-Time Design. Architecture evolves incrementally for the current Sprint stories only.",
            "Sprint Step 3: Pair Programming & Coding. Features are built on short-lived branches with continuous integration.",
            "Sprint Step 4: Automated CI Testing. Every commit triggers unit tests immediately, catching bugs early.",
            "Sprint Step 5: Sprint Review & Ship. A working increment is demonstrated to the client for fast feedback."
        },
        new string[] {
            "Kanban Col 1 (Backlog): Incoming user story cards wait in the queue without overloading developers.",
            "Kanban Col 2 (To Do): Next highest-priority task cards are queued for pull when team capacity opens.",
            "Kanban Col 3 (In Progress - WIP Limit = 2): Capping active task cards at 2 prevents context-switching bottlenecks!",
            "Kanban Col 4 (QA Review): Completed code cards wait for peer verification before merging.",
            "Kanban Col 5 (Done): Task cards complete flow! Cycle time is measured from 'In Progress' to 'Done'."
        },
        new string[] {
            "Stage 1 (Git Commit): Developer pushes a small commit to main/feature branch, triggering the CI webhook.",
            "Stage 2 (Automated Build): CI server compiles dependencies and packages the Docker artifact automatically.",
            "Stage 3 (Automated Unit Tests): 120+ unit tests run in seconds. If any test fails, the pipeline halts here!",
            "Stage 4 (Staging Canary): Artifact deploys to a pre-production mirror for automated smoke testing.",
            "Stage 5 (Production Release): Zero-downtime deployment pushes the verified build live to users."
        },
        new string[] {
            "Waterfall at 60%: Months spent locking REQ, DESIGN, and CODE with zero working software shipped to the client yet.",
            "Waterfall Rework Tower (+125 hrs): Changing requirements at 60% invalidates frozen architecture and forces a restart!",
            "60% Requirement Pivot: The client mandates a major OAuth & payment workflow change mid-way through the project.",
            "Agile at 60%: By the 60% mark, the Agile team has already delivered 3 working Sprint increments to production.",
            "Agile Adaptation Bar (+15 hrs): Agile reprioritizes the Sprint 4 Backlog, cutting rework cost by 88%!"
        }
    };

    void Start()
    {
        InitHelperMaterials();
        UpdateVisuals();
    }

    private void InitHelperMaterials()
    {
        Shader baseShader = (matBluePhase != null) ? matBluePhase.shader : Shader.Find("Universal Render Pipeline/Lit");
        if (baseShader == null) baseShader = Shader.Find("Standard");

        matDarkBoard = new Material(baseShader);
        matDarkBoard.color = new Color(0.08f, 0.12f, 0.20f, 1f); // Dark Slate #141E33

        matGreenPass = new Material(baseShader);
        matGreenPass.color = new Color(0.00f, 0.62f, 0.45f, 1f); // Emerald Green #009E73
    }

    public void NextStep()
    {
        hasAnomalyTriggered = false;
        if (currentStep < 4)
        {
            currentStep++;
            UpdateVisuals();
        }
        else if (currentSceneMode == 1)
        {
            currentStep = 0;
            UpdateVisuals();
        }
    }

    public void PrevStep()
    {
        hasAnomalyTriggered = false;
        if (currentStep > 0)
        {
            currentStep--;
            UpdateVisuals();
        }
    }

    public void ToggleMode()
    {
        SelectScene((currentSceneMode + 1) % 5);
    }

    /// <summary>
    /// Called directly by the Figma Top Navigation Tab Bar buttons (0 to 4).
    /// </summary>
    public void SelectScene(int sceneIndex)
    {
        if (sceneIndex < 0 || sceneIndex > 4) return;
        currentSceneMode = sceneIndex;
        hasAnomalyTriggered = false;

        if (quizPanel != null) quizPanel.SetActive(false);
        if (guidePanel != null) guidePanel.SetActive(false);

        if (modeButtonText != null)
            modeButtonText.text = modeButtonNames[currentSceneMode];

        if (objectiveText != null)
            objectiveText.text = sceneObjectives[currentSceneMode];

        ResetSimulation();
    }

    public void OpenQuizOverlay()
    {
        if (guidePanel != null) guidePanel.SetActive(false);
        if (quizPanel != null) quizPanel.SetActive(!quizPanel.activeSelf);
    }

    public void OpenGuideOverlay()
    {
        if (quizPanel != null) quizPanel.SetActive(false);
        if (guidePanel != null) guidePanel.SetActive(!guidePanel.activeSelf);
    }

    public void InjectRequirementChange()
    {
        hasAnomalyTriggered = true;

        if (currentSceneMode == 0)
        {
            int interruptedStep = currentStep + 1;
            int penalty = interruptedStep * 25;
            reworkCost += penalty;
            currentStep = 0;
            UpdateVisuals();
            if (narrationSubtitleText != null)
                narrationSubtitleText.text = $"WATERFALL ALERT! Requirement changed at Step {interruptedStep}. Forced back to REQ! Rework +{penalty} hrs.";
        }
        else if (currentSceneMode == 1)
        {
            reworkCost += 5;
            UpdateVisuals();
            if (narrationSubtitleText != null)
                narrationSubtitleText.text = "AGILE ADAPTATION: New user story added to next Sprint Backlog without breaking current Sprint (+5 hrs).";
        }
        else if (currentSceneMode == 2)
        {
            reworkCost += 15;
            currentStep = 2; // WIP Column
            UpdateVisuals();
            if (phaseBlocks != null && phaseBlocks.Length > 2 && phaseBlocks[2] != null && matRedError != null)
                phaseBlocks[2].material = matRedError;

            if (narrationSubtitleText != null)
                narrationSubtitleText.text = "KANBAN BOTTLENECK! 3rd urgent task card forced into 'WIP' (Limit 3/2 Exceeded!). Context-switching delay +15 hrs!";
        }
        else if (currentSceneMode == 3)
        {
            reworkCost += 2;
            currentStep = 2; // Unit Test Gate
            UpdateVisuals();
            if (phaseBlocks != null && phaseBlocks.Length > 2 && phaseBlocks[2] != null && matRedError != null)
                phaseBlocks[2].material = matRedError;

            if (narrationSubtitleText != null)
                narrationSubtitleText.text = "CI/CD GATE BLOCKED! Unit test failed at Stage 3. Red pipe halts deployment before reaching Production! (+2 hrs)";
        }
        else if (currentSceneMode == 4)
        {
            reworkCost = 125;
            currentStep = 1; // Focus on Tall Red Waterfall Rework Tower
            UpdateVisuals();
            if (narrationSubtitleText != null)
                narrationSubtitleText.text = "60% CHANGE SHOCK TRIGGERED! Waterfall Rework spikes to 125 hrs (Red Tower) vs. Agile's 15 hrs (Green Sprint Bar)!";
        }

        if (audioSource != null && errorBuzzClip != null && !isVoiceMuted)
            audioSource.PlayOneShot(errorBuzzClip);
    }

    public void ResetSimulation()
    {
        currentStep = 0;
        hasAnomalyTriggered = false;
        reworkCost = (currentSceneMode == 4) ? 60 : 0;
        UpdateVisuals();
    }

    public void ToggleVoice()
    {
        isVoiceMuted = !isVoiceMuted;
        if (isVoiceMuted && audioSource != null)
            audioSource.Stop();
    }

    public void SelectStepDirectly(int index)
    {
        if (index >= 0 && index < 5)
        {
            hasAnomalyTriggered = false;
            currentStep = index;
            UpdateVisuals();
        }
    }

    public void OnSliderScaleChanged(float scaleValue)
    {
        if (sdlcStageRoot != null)
            sdlcStageRoot.localScale = Vector3.one * scaleValue;
    }

    /// <summary>
    /// Helper to spawn non-colliding decorative 3D elements (pipes, task cards, sprint ring, base platform).
    /// </summary>
    private GameObject CreateProp(PrimitiveType type, string name, Vector3 localPos, Vector3 localScale, Vector3 localEuler, Material mat)
    {
        if (propsContainer == null) return null;
        GameObject obj = GameObject.CreatePrimitive(type);
        obj.name = name;
        Collider col = obj.GetComponent<Collider>();
        if (col != null) Destroy(col); // Remove collider so taps still hit the 5 main phase blocks

        obj.transform.SetParent(propsContainer, false);
        obj.transform.localPosition = localPos;
        obj.transform.localScale = localScale;
        obj.transform.localRotation = Quaternion.Euler(localEuler);

        MeshRenderer mr = obj.GetComponent<MeshRenderer>();
        if (mr != null && mat != null) mr.material = mat;

        spawnedProps.Add(obj);
        return obj;
    }

    /// <summary>
    /// Builds authentic 3D Figma scene details around the 5 main interactive blocks!
    /// </summary>
    private void RebuildScene3DProps()
    {
        if (sdlcStageRoot == null && phaseBlocks != null && phaseBlocks.Length > 0 && phaseBlocks[0] != null)
            sdlcStageRoot = phaseBlocks[0].transform.parent;

        if (sdlcStageRoot == null) return;
        if (matDarkBoard == null) InitHelperMaterials();

        if (propsContainer == null)
        {
            Transform existing = sdlcStageRoot.Find("ProceduralSceneProps");
            if (existing != null) propsContainer = existing;
            else
            {
                GameObject containerObj = new GameObject("ProceduralSceneProps");
                containerObj.transform.SetParent(sdlcStageRoot, false);
                propsContainer = containerObj.transform;
            }
        }

        for (int i = 0; i < spawnedProps.Count; i++)
        {
            if (spawnedProps[i] != null) Destroy(spawnedProps[i]);
        }
        spawnedProps.Clear();

        // 1. Common Dark AR Pedestal Base Plate under every scene
        CreateProp(PrimitiveType.Cube, "PedestalBase", new Vector3(0f, -0.115f, 0f), new Vector3(0.46f, 0.008f, 0.14f), Vector3.zero, matDarkBoard);

        if (currentSceneMode == 0) // SCENE 1: Waterfall Downward Cascade Connectors
        {
            for (int i = 0; i < 4; i++)
            {
                Vector3 p1 = new Vector3(-0.16f + (i * 0.08f), 0.08f - (i * 0.04f), 0f);
                Vector3 p2 = new Vector3(-0.16f + ((i + 1) * 0.08f), 0.08f - ((i + 1) * 0.04f), 0f);
                Vector3 mid = (p1 + p2) * 0.5f;
                Material pipeMat = (i < currentStep) ? matOrangeActive : matDarkBoard;
                CreateProp(PrimitiveType.Cube, $"WF_Connector_{i}", mid, new Vector3(0.085f, 0.008f, 0.015f), new Vector3(0f, 0f, -26.5f), pipeMat);
            }
        }
        else if (currentSceneMode == 1) // SCENE 2: Agile Scrum Circular Sprint Ring + Central 34-Pt Hub
        {
            Vector3[] ringPts = {
                new Vector3( 0.00f,  0.10f, 0f),
                new Vector3( 0.12f,  0.03f, 0f),
                new Vector3( 0.07f, -0.09f, 0f),
                new Vector3(-0.07f, -0.09f, 0f),
                new Vector3(-0.12f,  0.03f, 0f)
            };

            // Connect the 5 sprint stations in a closed 3D loop
            for (int i = 0; i < 5; i++)
            {
                Vector3 a = ringPts[i];
                Vector3 b = ringPts[(i + 1) % 5];
                Vector3 mid = (a + b) * 0.5f;
                mid.z += 0.015f;
                float dist = Vector3.Distance(a, b);
                float angleZ = Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;
                Material segMat = (i <= currentStep) ? matGreenPass : matDarkBoard;
                CreateProp(PrimitiveType.Cube, $"SprintRingSeg_{i}", mid, new Vector3(dist, 0.01f, 0.01f), new Vector3(0f, 0f, angleZ), segMat);
            }

            // Central Sprint Velocity Hub
            CreateProp(PrimitiveType.Cylinder, "SprintCenterHub", new Vector3(0f, 0.0f, 0.01f), new Vector3(0.055f, 0.008f, 0.055f), new Vector3(90f, 0f, 0f), matDarkBoard);
        }
        else if (currentSceneMode == 2) // SCENE 3: Kanban Board Backing + 3D Sticky Task Cards Inside Columns!
        {
            // Dark Kanban Backboard behind the 5 columns
            CreateProp(PrimitiveType.Cube, "KanbanBackboard", new Vector3(0f, 0.0f, 0.022f), new Vector3(0.46f, 0.19f, 0.008f), Vector3.zero, matDarkBoard);

            // Place 3D Task Cards onto the columns
            int[] cardsPerCol = { 2, 2, hasAnomalyTriggered ? 3 : 2, 1, 2 };
            for (int col = 0; col < 5; col++)
            {
                float colX = -0.18f + (col * 0.09f);
                int count = cardsPerCol[col];
                for (int c = 0; c < count; c++)
                {
                    float cardY = 0.04f - (c * 0.036f);
                    Material cardMat = matGreenPass;
                    if (col == 2 && c == 2 && hasAnomalyTriggered && matRedError != null)
                        cardMat = matRedError; // 3rd overloaded card in WIP turns bright Red!
                    else if (col == currentStep)
                        cardMat = matOrangeActive;

                    CreateProp(PrimitiveType.Cube, $"KanbanCard_{col}_{c}", new Vector3(colX, cardY, -0.014f), new Vector3(0.042f, 0.024f, 0.008f), Vector3.zero, cardMat);
                }
            }
        }
        else if (currentSceneMode == 3) // SCENE 4: CI/CD Pipeline Cylindrical Conduit Pipes + Gate Nodes
        {
            for (int i = 0; i < 4; i++)
            {
                float midX = -0.135f + (i * 0.09f);
                Material pipeMat = matDarkBoard;
                if (hasAnomalyTriggered && i == 2 && matRedError != null)
                    pipeMat = matRedError; // Blocked pipe after Unit Test failure
                else if (i < currentStep)
                    pipeMat = matGreenPass; // Passed pipeline stages glow Emerald Green!

                CreateProp(PrimitiveType.Cylinder, $"CICD_Pipe_{i}", new Vector3(midX, 0.0f, 0f), new Vector3(0.014f, 0.045f, 0.014f), new Vector3(0f, 0f, 90f), pipeMat);
            }
        }
        else if (currentSceneMode == 4) // SCENE 5: W vs A 60% Change Split Platform
        {
            // Left Platform (Waterfall Red Risk Zone) & Right Platform (Agile Green Safe Zone)
            if (matRedError != null)
                CreateProp(PrimitiveType.Cube, "WF_RiskZone", new Vector3(-0.135f, -0.105f, 0f), new Vector3(0.18f, 0.006f, 0.10f), Vector3.zero, matRedError);
            CreateProp(PrimitiveType.Cube, "Agile_SafeZone", new Vector3(0.135f, -0.105f, 0f), new Vector3(0.18f, 0.006f, 0.10f), Vector3.zero, matGreenPass);
        }
    }

    private void UpdateStageLayout()
    {
        if (phaseBlocks == null || phaseBlocks.Length < 5) return;

        for (int i = 0; i < 5; i++)
        {
            if (phaseBlocks[i] == null) continue;
            Transform block = phaseBlocks[i].transform;

            if (currentSceneMode == 0) // Scene 1: Waterfall Staircase
            {
                block.localPosition = new Vector3(-0.16f + (i * 0.08f), 0.08f - (i * 0.04f), 0f);
                block.localScale = new Vector3(0.065f, 0.025f, 0.065f);
            }
            else if (currentSceneMode == 1) // Scene 2: Agile Scrum Ring
            {
                Vector3[] agileRing = {
                    new Vector3( 0.00f,  0.10f, 0f),
                    new Vector3( 0.12f,  0.03f, 0f),
                    new Vector3( 0.07f, -0.09f, 0f),
                    new Vector3(-0.07f, -0.09f, 0f),
                    new Vector3(-0.12f,  0.03f, 0f)
                };
                block.localPosition = agileRing[i];
                block.localScale = new Vector3(0.065f, 0.025f, 0.065f);
            }
            else if (currentSceneMode == 2) // Scene 3: Kanban Swimlane Columns
            {
                block.localPosition = new Vector3(-0.18f + (i * 0.09f), 0.0f, 0f);
                block.localScale = new Vector3(0.065f, 0.14f, 0.018f);
            }
            else if (currentSceneMode == 3) // Scene 4: CI/CD Pipeline Nodes
            {
                block.localPosition = new Vector3(-0.18f + (i * 0.09f), 0.0f, 0f);
                block.localScale = new Vector3(0.06f, 0.032f, 0.06f);
            }
            else if (currentSceneMode == 4) // Scene 5: W vs A 60% Change Comparison Bars
            {
                float[] barHeights = { 0.065f, 0.15f, 0.03f, 0.065f, 0.022f };
                float h = barHeights[i];
                block.localPosition = new Vector3(-0.18f + (i * 0.09f), -0.10f + (h * 0.5f), 0f);
                block.localScale = new Vector3(0.058f, h, 0.058f);
            }

            TMP_Text label3D = block.GetComponentInChildren<TMP_Text>();
            if (label3D != null)
            {
                label3D.text = phaseLabelsPerMode[currentSceneMode][i];
            }
        }

        RebuildScene3DProps();
    }

    private void UpdateVisuals()
    {
        UpdateStageLayout();

        // Highlight active Top Navigation Tab if present
        if (sceneTabBackgrounds != null && sceneTabBackgrounds.Length == 5)
        {
            for (int i = 0; i < 5; i++)
            {
                if (sceneTabBackgrounds[i] != null)
                {
                    sceneTabBackgrounds[i].color = (i == currentSceneMode)
                        ? new Color(0.0f, 0.45f, 0.70f, 1f)  // Active Sky Blue #0072B2
                        : new Color(0.12f, 0.16f, 0.23f, 1f); // Inactive Dark Slate #1E293B
                }
            }
        }

        if (phaseBlocks != null && phaseBlocks.Length > currentStep)
        {
            for (int i = 0; i < phaseBlocks.Length; i++)
            {
                if (phaseBlocks[i] != null)
                {
                    if (currentSceneMode == 4 && i == 1 && currentStep != 1 && matRedError != null)
                        phaseBlocks[i].material = matRedError;
                    else if (currentSceneMode == 4 && i == 4 && currentStep != 4 && matGreenPass != null)
                        phaseBlocks[i].material = matGreenPass;
                    else
                        phaseBlocks[i].material = (i == currentStep) ? matOrangeActive : matBluePhase;
                }
            }

            if (workItemOrb != null && phaseBlocks[currentStep] != null)
            {
                Transform activeBlock = phaseBlocks[currentStep].transform;
                float topOffset = (activeBlock.localScale.y * 0.5f) + 0.025f;
                workItemOrb.localPosition = new Vector3(
                    activeBlock.localPosition.x,
                    activeBlock.localPosition.y + topOffset,
                    activeBlock.localPosition.z
                );
            }
        }

        if (codeEngineText != null)
        {
            string[] activeLines = codeLinesPerMode[currentSceneMode];
            string formattedCode = "";
            for (int i = 0; i < activeLines.Length; i++)
            {
                if (i == currentStep)
                    formattedCode += $"<color=#E69F00>>> {activeLines[i]}</color>\n";
                else
                    formattedCode += $"   {activeLines[i]}\n";
            }
            codeEngineText.text = formattedCode.TrimEnd();
        }

        if (narrationSubtitleText != null)
            narrationSubtitleText.text = narrationsPerMode[currentSceneMode][currentStep];

        if (stepCounterText != null)
            stepCounterText.text = $"STEP {currentStep + 1}/5";

        if (reworkCostText != null)
        {
            if (currentSceneMode == 4)
            {
                int[] scene5Costs = { 60, 125, 60, 60, 15 };
                reworkCostText.text = scene5Costs[currentStep].ToString();
            }
            else
            {
                reworkCostText.text = reworkCost.ToString();
            }
        }

        if (currentPhaseText != null)
            currentPhaseText.text = phaseLabelsPerMode[currentSceneMode][currentStep];

        if (currentSceneMode == 0 && audioSource != null && !isVoiceMuted && stepNarrations != null && currentStep < stepNarrations.Length && stepNarrations[currentStep] != null)
        {
            audioSource.Stop();
            audioSource.clip = stepNarrations[currentStep];
            audioSource.Play();
        }
    }

    public void BindSpawnedStage(GameObject stage)
    {
        sdlcStageRoot = stage.transform;
        propsContainer = null;

        Transform orb = stage.transform.Find("WorkItemOrb");
        if (orb != null) workItemOrb = orb;

        string[] blockNames = { "Step1_Req", "Step2_Design", "Step3_Code", "Step4_Test", "Step5_Deploy" };
        phaseBlocks = new MeshRenderer[5];
        for (int i = 0; i < 5; i++)
        {
            Transform block = stage.transform.Find(blockNames[i]);
            if (block != null)
                phaseBlocks[i] = block.GetComponent<MeshRenderer>();
        }

        UpdateVisuals();
    }
}