using UnityEngine;
using TMPro;

/// <summary>
/// Complete 5-Scene SDLC & Agile AR Explorer Engine.
/// Dynamically morphs the 3D AR stage, 3D labels, code box, subtitles, and metrics across all 5 scenes:
/// 0 = Waterfall, 1 = Agile Scrum, 2 = Kanban WIP Board, 3 = CI/CD Pipeline, 4 = Waterfall vs. Agile (60% Change).
/// </summary>
public class StepThroughEngine : MonoBehaviour
{
    [Header("3D Scene Objects")]
    public Transform workItemOrb;
    public MeshRenderer[] phaseBlocks;
    public Material matBluePhase;      // #0072B2 (Normal phase)
    public Material matOrangeActive;   // #E69F00 (Active step)
    public Material matRedError;       // #D55E00 (Bottleneck / High Rework / Failed CI Gate)

    [Header("UI Text References (Match Figma HUD)")]
    public TMP_Text codeEngineText;
    public TMP_Text narrationSubtitleText;
    public TMP_Text stepCounterText;
    public TMP_Text reworkCostText;
    public TMP_Text currentPhaseText;
    public TMP_Text modeButtonText;
    public TMP_Text objectiveText;     // Drag TopHUD ObjectiveText here

    [Header("AR Stage Root Reference")]
    public Transform sdlcStageRoot;

    [Header("Audio Narration (Optional)")]
    public AudioSource audioSource;
    public AudioClip[] stepNarrations;
    public AudioClip errorBuzzClip;
    public bool isVoiceMuted = false;

    // 0 = Waterfall, 1 = Agile Scrum, 2 = Kanban WIP, 3 = CI/CD Pipeline, 4 = W vs A Comparison
    private int currentSceneMode = 0;
    private int currentStep = 0;
    private int reworkCost = 0;

    private readonly string[] modeButtonNames = {
        "Scene 1: Waterfall",
        "Scene 2: Agile Scrum",
        "Scene 3: Kanban WIP",
        "Scene 4: CI/CD Pipe",
        "Scene 5: W vs A (60%)"
    };

    private readonly string[] sceneObjectives = {
        "<color=#38BDF8>SCENE 1 · WATERFALL MODEL</color>\nUnderstand why sequential phases cannot easily flow backward.",
        "<color=#38BDF8>SCENE 2 · AGILE SCRUM SPRINT</color>\nIterative sprint loop adapts to change with minimal rework cost.",
        "<color=#38BDF8>SCENE 3 · KANBAN & WIP LIMITS</color>\nVisualize workflow columns and prevent Work-In-Progress bottlenecks.",
        "<color=#38BDF8>SCENE 4 · CI/CD DEVOPS PIPELINE</color>\nAutomated build, test, and deployment gates catch bugs before production.",
        "<color=#38BDF8>SCENE 5 · WATERFALL VS AGILE (60% CHANGE)</color>\nCompare rework cost when client requirements change at 60% completion."
    };

    // Short names for both bottom-right HUD badge and 3D floating text labels
    private readonly string[][] phaseLabelsPerMode = new string[][] {
        new string[] { "REQ", "DESIGN", "CODE", "TEST", "DEPLOY" },
        new string[] { "BACKLOG", "SPRINT", "CODE", "CI TEST", "REVIEW" },
        new string[] { "BACKLOG", "TO DO", "WIP (max 2)", "QA", "DONE" },
        new string[] { "COMMIT", "BUILD", "UNIT TEST", "STAGING", "PROD" },
        new string[] { "WF 60%", "WF REWORK", "CHANGE!", "AGILE 60%", "AG REWORK" }
    };

    // Synchronized Code / Config / Simulation lines for all 5 scenes
    private readonly string[][] codeLinesPerMode = new string[][] {
        // 0: Waterfall
        new string[] {
            "1: GatherRequirements(frozenSpec);",
            "2: DesignArchitecture(monolithDB);",
            "3: BuildAndCode(allModules);",
            "4: RunSystemTests(endOfProject);",
            "5: DeployRelease(production);"
        },
        // 1: Agile Scrum
        new string[] {
            "1: PullSprintBacklog(storyPoints: 34);",
            "2: SprintPlanning(twoWeekTimebox);",
            "3: DailyScrumAndPairCode(branch);",
            "4: ContinuousIntegration(autoTest);",
            "5: SprintReviewAndRetro(increment);"
        },
        // 2: Kanban Board
        new string[] {
            "1: CardCreated(column: 'Backlog');",
            "2: PullTicket(column: 'Selected To-Do');",
            "3: CheckWIPLimit(inProgress <= maxWIP);",
            "4: PeerCodeReview(column: 'QA Verify');",
            "5: MoveCardToDone(leadTime: 2.4d);"
        },
        // 3: CI/CD Pipeline
        new string[] {
            "1: git push origin feat/ar-module",
            "2: job: build_container (Docker 24s)",
            "3: job: run_unit_tests (coverage 94%)",
            "4: job: deploy_staging (smoke_test OK)",
            "5: job: release_prod (blue_green_live)"
        },
        // 4: Waterfall vs Agile 60% Change Comparison
        new string[] {
            "1: Waterfall.Progress(60%); // Spec Frozen",
            "2: Waterfall.OnChange() -> Rework = +125 hrs;",
            "3: EVENT: ClientPivotAt60Percent(newAuthAPI);",
            "4: Agile.Progress(Sprint_3); // 3 Increments Live",
            "5: Agile.OnChange() -> NextSprint = +15 hrs;"
        }
    };

    // Educational subtitle narrations for all 5 scenes
    private readonly string[][] narrationsPerMode = new string[][] {
        // 0: Waterfall
        new string[] {
            "Step 1: Requirements. In Waterfall, all client requirements are locked upfront before any design or coding begins.",
            "Step 2: System Design. Complete database schemas and software architecture are frozen before writing code.",
            "Step 3: Implementation. Developers write the entire system monolithically based on the frozen design.",
            "Step 4: Integration & Testing. QA tests the whole system at the end. Late bugs discovered here are expensive!",
            "Step 5: Deployment. The finished software is released to the customer after months of sequential phases."
        },
        // 1: Agile Scrum
        new string[] {
            "Sprint Step 1: Backlog Refinement. The team pulls top-priority User Stories (34 pts) into the 2-week Sprint.",
            "Sprint Step 2: Just-in-Time Design. Architecture evolves incrementally for the current Sprint stories only.",
            "Sprint Step 3: Pair Programming & Coding. Features are built on short-lived branches with continuous integration.",
            "Sprint Step 4: Automated CI Testing. Every commit triggers unit tests immediately, catching bugs early.",
            "Sprint Step 5: Sprint Review & Ship. A working increment is demonstrated to the client for fast feedback."
        },
        // 2: Kanban WIP
        new string[] {
            "Kanban Col 1 (Backlog): Raw incoming tasks wait in the queue without overloading developers.",
            "Kanban Col 2 (To Do): Next highest-priority cards are queued for pull when capacity opens.",
            "Kanban Col 3 (In Progress - WIP Limit = 2): Capping active cards prevents context-switching bottlenecks!",
            "Kanban Col 4 (QA Review): Completed code waits for peer verification before merging.",
            "Kanban Col 5 (Done): Card completes flow! Cycle time is measured from 'In Progress' to 'Done'."
        },
        // 3: CI/CD Pipeline
        new string[] {
            "Stage 1 (Git Commit): Developer pushes a small commit to main/feature branch, triggering the webhook.",
            "Stage 2 (Automated Build): CI server compiles dependencies and packages the artifact automatically.",
            "Stage 3 (Automated Unit Tests): 120+ unit tests run in seconds. If any test fails, the pipeline halts here!",
            "Stage 4 (Staging Canary): Artifact deploys to a pre-production mirror for automated smoke testing.",
            "Stage 5 (Production Release): Zero-downtime deployment pushes the verified build live to users."
        },
        // 4: Waterfall vs Agile (60% Change Comparison)
        new string[] {
            "Bar 1 (Waterfall at 60%): The team has spent months locking REQ, DESIGN, and CODE without shipping a single increment.",
            "Bar 2 (Waterfall Rework Tower = 125 hrs): Changing requirements at 60% scraps frozen design & code, spiking rework!",
            "Bar 3 (60% Requirement Shock): The client requests a major OAuth security change mid-way through development.",
            "Bar 4 (Agile at 60%): By 60% timeline, the Agile team has already shipped 3 working Sprint increments to the user.",
            "Bar 5 (Agile Adaptation Bar = 15 hrs): Agile slots the new request into Sprint 4 Backlog with 88% less rework!"
        }
    };

    void Start()
    {
        UpdateVisuals();
    }

    public void NextStep()
    {
        if (currentStep < 4)
        {
            currentStep++;
            UpdateVisuals();
        }
        else if (currentSceneMode == 1) // Agile loops around the circle
        {
            currentStep = 0;
            UpdateVisuals();
        }
    }

    public void PrevStep()
    {
        if (currentStep > 0)
        {
            currentStep--;
            UpdateVisuals();
        }
    }

    /// <summary>
    /// Cycles through all 5 scenes:
    /// 0: Waterfall -> 1: Agile Scrum -> 2: Kanban WIP -> 3: CI/CD Pipe -> 4: W vs A (60%)
    /// </summary>
    public void ToggleMode()
    {
        currentSceneMode = (currentSceneMode + 1) % 5;

        if (modeButtonText != null)
            modeButtonText.text = modeButtonNames[currentSceneMode];

        if (objectiveText != null)
            objectiveText.text = sceneObjectives[currentSceneMode];

        ResetSimulation();
    }

    /// <summary>
    /// Context-aware anomaly injection for each of the 5 scenes!
    /// </summary>
    public void InjectRequirementChange()
    {
        if (currentSceneMode == 0) // Scene 1: Waterfall
        {
            int interruptedStep = currentStep + 1;
            int penalty = interruptedStep * 25;
            reworkCost += penalty;
            currentStep = 0;
            UpdateVisuals();
            if (narrationSubtitleText != null)
                narrationSubtitleText.text = $"WATERFALL ALERT! Requirement changed at Step {interruptedStep}. Forced back to REQ! Rework +{penalty} hrs.";
        }
        else if (currentSceneMode == 1) // Scene 2: Agile Scrum
        {
            reworkCost += 5;
            UpdateVisuals();
            if (narrationSubtitleText != null)
                narrationSubtitleText.text = "AGILE ADAPTATION: New user story added to next Sprint Backlog without breaking current Sprint (+5 hrs).";
        }
        else if (currentSceneMode == 2) // Scene 3: Kanban WIP Bottleneck
        {
            reworkCost += 15;
            currentStep = 2; // Jump to Column 3 (WIP)
            UpdateVisuals();
            if (phaseBlocks != null && phaseBlocks.Length > 2 && phaseBlocks[2] != null && matRedError != null)
                phaseBlocks[2].material = matRedError;

            if (narrationSubtitleText != null)
                narrationSubtitleText.text = "KANBAN BOTTLENECK! Urgent task forced into 'In Progress' (WIP 3/2 Exceeded!). Context-switching delay +15 hrs!";
        }
        else if (currentSceneMode == 3) // Scene 4: CI/CD Failed Unit Test Gate
        {
            reworkCost += 2;
            currentStep = 2; // Halts at Stage 3 (Unit Test Gate)
            UpdateVisuals();
            if (phaseBlocks != null && phaseBlocks.Length > 2 && phaseBlocks[2] != null && matRedError != null)
                phaseBlocks[2].material = matRedError;

            if (narrationSubtitleText != null)
                narrationSubtitleText.text = "CI/CD GATE BLOCKED! Unit test failed at Stage 3. Pipeline halted automatically before reaching Production! Fix time: +2 hrs.";
        }
        else if (currentSceneMode == 4) // Scene 5: 60% Mid-Project Requirement Shock Comparison!
        {
            reworkCost = 125; // Shows Waterfall spike vs Agile 15 hrs
            currentStep = 1;  // Focuses on the tall Waterfall Rework Tower
            UpdateVisuals();

            if (narrationSubtitleText != null)
                narrationSubtitleText.text = "60% CHANGE SHOCK TRIGGERED! Waterfall Rework spikes to 125 hrs (Red Tower) vs. Agile's 15 hrs (88% cost savings)!";
        }

        if (audioSource != null && errorBuzzClip != null && !isVoiceMuted)
            audioSource.PlayOneShot(errorBuzzClip);
    }

    public void ResetSimulation()
    {
        currentStep = 0;
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
    /// Morphs both the positions AND scales of the 5 3D blocks to match all 5 scenes!
    /// </summary>
    private void UpdateStageLayout()
    {
        if (phaseBlocks == null || phaseBlocks.Length < 5) return;

        for (int i = 0; i < 5; i++)
        {
            if (phaseBlocks[i] == null) continue;
            Transform block = phaseBlocks[i].transform;

            if (currentSceneMode == 0) // Scene 1: Waterfall (Downward Staircase)
            {
                block.localPosition = new Vector3(-0.16f + (i * 0.08f), 0.08f - (i * 0.04f), 0f);
                block.localScale = new Vector3(0.065f, 0.025f, 0.065f);
            }
            else if (currentSceneMode == 1) // Scene 2: Agile Scrum (Circular Pentagon Ring)
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
            else if (currentSceneMode == 2) // Scene 3: Kanban Board (5 Tall Vertical Swimlane Columns)
            {
                block.localPosition = new Vector3(-0.18f + (i * 0.09f), 0.0f, 0f);
                block.localScale = new Vector3(0.055f, 0.085f, 0.02f);
            }
            else if (currentSceneMode == 3) // Scene 4: CI/CD Pipeline (Flat Horizontal Conveyor Stages)
            {
                block.localPosition = new Vector3(-0.18f + (i * 0.09f), 0.0f, 0f);
                block.localScale = new Vector3(0.065f, 0.02f, 0.065f);
            }
            else if (currentSceneMode == 4) // Scene 5: Waterfall vs Agile 3D Cost Comparison Bars!
            {
                // Heights represent relative cost/effort:
                // [0] WF 60% Base (0.06), [1] WF Rework Spike (0.14 - Tall Tower!), [2] 60% Shock Divider (0.03),
                // [3] Agile 60% Base (0.06), [4] Agile Rework (0.02 - Tiny Bar!)
                float[] barHeights = { 0.06f, 0.14f, 0.03f, 0.06f, 0.02f };
                float h = barHeights[i];
                // Align bottoms of all 5 bars neatly on the baseline (Y = -0.07f)
                block.localPosition = new Vector3(-0.18f + (i * 0.09f), -0.07f + (h * 0.5f), 0f);
                block.localScale = new Vector3(0.055f, h, 0.055f);
            }

            // Automatically update the 3D floating text label child inside each block
            TMP_Text label3D = block.GetComponentInChildren<TMP_Text>();
            if (label3D != null)
            {
                label3D.text = phaseLabelsPerMode[currentSceneMode][i];
            }
        }
    }

    private void UpdateVisuals()
    {
        // 1. Morph 3D block positions, scales, and 3D labels for current scene
        UpdateStageLayout();

        // 2. Highlight active block & position Orb right above the active block's top surface
        if (phaseBlocks != null && phaseBlocks.Length > currentStep)
        {
            for (int i = 0; i < phaseBlocks.Length; i++)
            {
                if (phaseBlocks[i] != null)
                {
                    // In Scene 5, keep the tall Waterfall Rework Tower (Block 1) tinted Red when not selected!
                    if (currentSceneMode == 4 && i == 1 && currentStep != 1 && matRedError != null)
                        phaseBlocks[i].material = matRedError;
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

        // 3. Update Synchronized Code / Simulation Box
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

        // 4. Update Subtitles & Bottom Badges
        if (narrationSubtitleText != null)
            narrationSubtitleText.text = narrationsPerMode[currentSceneMode][currentStep];

        if (stepCounterText != null)
            stepCounterText.text = $"STEP {currentStep + 1}/5";

        if (reworkCostText != null)
        {
            // In Scene 5, dynamically show the exact rework cost of whichever bar you are inspecting!
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

        // 5. Play Voiceover (for Scene 0 Waterfall)
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