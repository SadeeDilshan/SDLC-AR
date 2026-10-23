using UnityEngine;
using TMPro;

public class StepThroughEngine : MonoBehaviour
{
    [Header("3D Scene Objects")]
    public Transform workItemOrb;
    public MeshRenderer[] phaseBlocks; // Drag Step1_Req to Step5_Deploy here
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

    [Header("Audio Narration (Optional)")]
    public AudioSource audioSource;
    public AudioClip[] stepNarrations; // 5 clips for steps 1-5
    public AudioClip errorBuzzClip;
    public bool isVoiceMuted = false;

    private int currentStep = 0; // 0 to 4
    private bool isAgileMode = false;
    private int reworkCost = 0;

    private readonly string[] phaseShortNames = { "REQ", "DESIGN", "CODE", "TEST", "DEPLOY" };

    private readonly string[] codeLines = {
        "1: GatherRequirements(backlog);",
        "2: DesignArchitecture(sprintPlan);",
        "3: BuildAndCode(featureBranch);",
        "4: RunAutomatedTests(ciServer);",
        "5: DeployRelease(production);"
    };

    private readonly string[] waterfallNarrations = {
        "Step 1: Requirements. In Waterfall, all client requirements are locked upfront before any design or coding begins.",
        "Step 2: System Design. Complete database schemas and software architecture are frozen before writing code.",
        "Step 3: Implementation. Developers write the entire system monolithically based on the frozen design.",
        "Step 4: Integration & Testing. QA tests the whole system at the end. Late bugs discovered here are expensive!",
        "Step 5: Deployment. The finished software is released to the customer after months of sequential phases."
    };

    private readonly string[] agileNarrations = {
        "Sprint Step 1: Backlog Refinement. The team pulls top-priority User Stories (34 pts) into the 2-week Sprint.",
        "Sprint Step 2: Just-in-Time Design. Architecture evolves incrementally for the current Sprint stories only.",
        "Sprint Step 3: Pair Programming & Coding. Features are built on short-lived branches with continuous integration.",
        "Sprint Step 4: Automated CI Testing. Every commit triggers unit tests immediately, catching bugs early.",
        "Sprint Step 5: Sprint Review & Ship. A working increment is demonstrated to the client for fast feedback."
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
    }

    public void PrevStep()
    {
        if (currentStep > 0)
        {
            currentStep--;
            UpdateVisuals();
        }
    }

    public void ToggleMode()
    {
        isAgileMode = !isAgileMode;
        if (modeButtonText != null)
            modeButtonText.text = isAgileMode ? "Mode: Agile Scrum" : "Mode: Waterfall";
        ResetSimulation();
    }

    public void InjectRequirementChange()
    {
        if (!isAgileMode)
        {
            // Waterfall penalty: late changes force jump back to Requirements with heavy rework cost
            int penalty = (currentStep + 1) * 25;
            reworkCost += penalty;
            currentStep = 0; // Forced back to REQ!
            UpdateVisuals();
            if (narrationSubtitleText != null)
                narrationSubtitleText.text = $"ALERT! Client changed requirements at Step {currentStep + 1}. Waterfall must restart from REQ! Rework Cost +{penalty} hrs.";
        }
        else
        {
            // Agile absorbs change into next Sprint Backlog with minimal rework
            reworkCost += 5;
            UpdateVisuals();
            if (narrationSubtitleText != null)
                narrationSubtitleText.text = "AGILE ADAPTATION: New client request added to Product Backlog for the next Sprint without breaking the current Sprint (+5 hrs).";
        }

        if (audioSource != null && errorBuzzClip != null && !isVoiceMuted)
            audioSource.PlayOneShot(errorBuzzClip);
    }

    public void ResetSimulation()
    {
        currentStep = 0;
        reworkCost = 0;
        UpdateVisuals();
    }

    public void ToggleVoice()
    {
        isVoiceMuted = !isVoiceMuted;
        if (isVoiceMuted && audioSource != null) audioSource.Stop();
    }

    public void SelectStepDirectly(int index)
    {
        if (index >= 0 && index < 5)
        {
            currentStep = index;
            UpdateVisuals();
        }
    }

    private void UpdateVisuals()
    {
        // 1. Move Orb and highlight active 3D block
        if (phaseBlocks != null && phaseBlocks.Length > currentStep)
        {
            for (int i = 0; i < phaseBlocks.Length; i++)
            {
                if (phaseBlocks[i] != null)
                    phaseBlocks[i].material = (i == currentStep) ? matOrangeActive : matBluePhase;
            }

            if (workItemOrb != null && phaseBlocks[currentStep] != null)
            {
                Vector3 blockPos = phaseBlocks[currentStep].transform.localPosition;
                workItemOrb.localPosition = new Vector3(blockPos.x, blockPos.y + 0.03f, blockPos.z);
            }
        }

        // 2. Update Synchronized Code Box with '>>' pointer
        if (codeEngineText != null)
        {
            string formattedCode = "";
            for (int i = 0; i < codeLines.Length; i++)
            {
                if (i == currentStep)
                    formattedCode += $"<color=#E69F00>>> {codeLines[i]}</color>\n";
                else
                    formattedCode += $"   {codeLines[i]}\n";
            }
            codeEngineText.text = formattedCode.TrimEnd();
        }

        // 3. Update Subtitles & Metrics
        if (narrationSubtitleText != null)
            narrationSubtitleText.text = isAgileMode ? agileNarrations[currentStep] : waterfallNarrations[currentStep];

        if (stepCounterText != null)
            stepCounterText.text = $"STEP {currentStep + 1}/5";

        if (reworkCostText != null)
            reworkCostText.text = reworkCost.ToString();

        if (currentPhaseText != null)
            currentPhaseText.text = phaseShortNames[currentStep];

        // 4. Play Voiceover if available
        if (audioSource != null && !isVoiceMuted && stepNarrations != null && currentStep < stepNarrations.Length && stepNarrations[currentStep] != null)
        {
            audioSource.Stop();
            audioSource.clip = stepNarrations[currentStep];
            audioSource.Play();
        }
    }
}