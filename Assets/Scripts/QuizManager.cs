using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Complete 20-Question Pre/Post Assessment Engine for SDLC & Agile AR Explorer.
/// Automatically binds all buttons at runtime and disables stray raycast targets
/// so Retake, Next, and Option buttons always respond reliably.
/// </summary>
public class QuizManager : MonoBehaviour
{
    [Header("Root Panels")]
    public GameObject quizPanel;
    public GameObject questionViewContainer;
    public GameObject resultsViewContainer;

    [Header("Top Metric Badges (Pre / Post / Gain / Timer)")]
    public TMP_Text preScoreBadgeText;
    public TMP_Text postScoreBadgeText;
    public TMP_Text learningGainBadgeText;
    public TMP_Text timerText;
    public TMP_Text progressCounterText;

    [Header("Question & Options UI")]
    public TMP_Text categoryBadgeText;
    public TMP_Text questionPromptText;
    public Button[] optionButtons;      // 4 option buttons
    public TMP_Text[] optionTexts;      // 4 option labels
    public Image[] optionBackgrounds;   // 4 button backgrounds
    public TMP_Text feedbackExplanationText;
    public Button nextQuestionButton;
    public TMP_Text nextButtonLabel;

    [Header("End-of-Quiz Results Screen UI")]
    public TMP_Text resultsTitleText;
    public TMP_Text resultsSummaryText;
    public TMP_Text resultsGainHighlightText;
    public Button retakeQuizButton;
    public Button returnToARButton;

    [System.Serializable]
    public struct QuizQuestion
    {
        public string sceneCategory;
        public string prompt;
        public string[] options;
        public int correctIndex;
        public string explanation;

        public QuizQuestion(string cat, string q, string a, string b, string c, string d, int correct, string exp)
        {
            sceneCategory = cat;
            prompt = q;
            options = new string[] { a, b, c, d };
            correctIndex = correct;
            explanation = exp;
        }
    }

    private QuizQuestion[] questions;
    private int currentQuestionIndex = 0;
    private int currentCorrectCount = 0;
    private bool hasAnsweredCurrent = false;
    private bool isQuizFinished = false;

    // Timer & Attempt Tracking
    private float elapsedSeconds = 0f;
    private int attemptNumber = 0;      // 1 = Pre-Test (Baseline), 2+ = Post-Test (Retake)
    private int savedPreTestPercent = -1;
    private int savedPostTestPercent = -1;

    // Figma Palette Colors
    private readonly Color colDefaultOption = new Color(0.12f, 0.16f, 0.23f, 1f); // #1E293B Slate
    private readonly Color colCorrectGreen = new Color(0.00f, 0.62f, 0.45f, 1f); // #009E73 Emerald
    private readonly Color colWrongOrange = new Color(0.84f, 0.37f, 0.00f, 1f); // #D55E00 Vermilion

    void Awake()
    {
        Initialize20Questions();
        AutoBindButtonsAndFixRaycasts();
    }

    /// <summary>
    /// Guarantees that all Quiz buttons (including Retake Quiz) are wired at runtime
    /// and prevents decorative TMP_Text / Image overlays from blocking button clicks.
    /// </summary>
    private void AutoBindButtonsAndFixRaycasts()
    {
        if (quizPanel == null)
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                Transform qp = canvas.transform.Find("QuizPanel");
                if (qp != null) quizPanel = qp.gameObject;
            }
        }

        if (quizPanel == null) return;

        // 1. Disable raycastTarget on all TMP_Text elements inside QuizPanel so text never blocks clicks
        TMP_Text[] allTexts = quizPanel.GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text t in allTexts)
        {
            t.raycastTarget = false;
        }

        // 2. Find and force-bind the Retake and Return buttons inside ResultsViewContainer
        if (resultsViewContainer == null)
        {
            Transform rv = quizPanel.transform.Find("ResultsViewContainer");
            if (rv != null) resultsViewContainer = rv.gameObject;
        }

        if (resultsViewContainer != null)
        {
            Transform retakeTr = resultsViewContainer.transform.Find("Btn_RetakeQuiz");
            if (retakeTr != null)
            {
                retakeQuizButton = retakeTr.GetComponent<Button>();
                retakeTr.SetAsLastSibling(); // Bring to front of hierarchy
            }

            Transform returnTr = resultsViewContainer.transform.Find("Btn_ReturnARFromResults");
            if (returnTr != null)
            {
                returnToARButton = returnTr.GetComponent<Button>();
                returnTr.SetAsLastSibling(); // Bring to front of hierarchy
            }
        }

        if (retakeQuizButton != null)
        {
            retakeQuizButton.onClick.RemoveAllListeners();
            retakeQuizButton.onClick.AddListener(RetakeQuizNow);
        }

        if (returnToARButton != null)
        {
            returnToARButton.onClick.RemoveAllListeners();
            returnToARButton.onClick.AddListener(CloseQuizBackToAR);
        }

        // 3. Re-verify Next Question & Back buttons inside QuestionViewContainer
        if (questionViewContainer == null)
        {
            Transform qv = quizPanel.transform.Find("QuestionViewContainer");
            if (qv != null) questionViewContainer = qv.gameObject;
        }

        if (questionViewContainer != null)
        {
            Transform backTr = questionViewContainer.transform.Find("Btn_BackToAR");
            if (backTr != null)
            {
                Button bBtn = backTr.GetComponent<Button>();
                if (bBtn != null)
                {
                    bBtn.onClick.RemoveAllListeners();
                    bBtn.onClick.AddListener(CloseQuizBackToAR);
                }
            }
        }

        if (nextQuestionButton != null)
        {
            nextQuestionButton.onClick.RemoveAllListeners();
            nextQuestionButton.onClick.AddListener(NextQuestion);
        }

        // 4. Re-verify the 4 Option Buttons
        if (optionButtons != null)
        {
            for (int i = 0; i < optionButtons.Length; i++)
            {
                if (optionButtons[i] != null)
                {
                    int idx = i;
                    optionButtons[i].onClick.RemoveAllListeners();
                    optionButtons[i].onClick.AddListener(() => SelectOption(idx));
                }
            }
        }
    }

    void OnEnable()
    {
        if (questions == null || questions.Length == 0)
            Initialize20Questions();

        AutoBindButtonsAndFixRaycasts();

        if (attemptNumber == 0)
            StartNewQuizAttempt();
    }

    void Update()
    {
        if (quizPanel != null && quizPanel.activeSelf && !isQuizFinished)
        {
            elapsedSeconds += Time.deltaTime;
            UpdateTimerUI();
        }
    }

    private void UpdateTimerUI()
    {
        if (timerText != null)
        {
            int mins = Mathf.FloorToInt(elapsedSeconds / 60f);
            int secs = Mathf.FloorToInt(elapsedSeconds % 60f);
            timerText.text = $"⏱ {mins:00}:{secs:00}";
        }
    }

    /// <summary>
    /// Explicitly called when clicking the "↻ Retake Quiz (Post-Test)" button.
    /// </summary>
    public void RetakeQuizNow()
    {
        Debug.Log("[QuizManager] Retake Quiz triggered! Starting Attempt #" + (attemptNumber + 1));
        StartNewQuizAttempt();
    }

    /// <summary>
    /// Starts a fresh 20-question run, resetting score, question index, and timer.
    /// Attempt 1 = Pre-Test Baseline; Attempt 2+ = Post-Test & Learning Gain.
    /// </summary>
    public void StartNewQuizAttempt()
    {
        if (questions == null || questions.Length == 0)
            Initialize20Questions();

        attemptNumber++;
        currentQuestionIndex = 0;
        currentCorrectCount = 0;
        elapsedSeconds = 0f;
        isQuizFinished = false;
        hasAnsweredCurrent = false;

        if (resultsViewContainer != null) resultsViewContainer.SetActive(false);
        if (questionViewContainer != null)
        {
            questionViewContainer.SetActive(true);
            questionViewContainer.transform.SetAsLastSibling();
        }

        UpdateTimerUI();
        UpdateTopBadgesUI();
        LoadCurrentQuestion();
    }

    private void LoadCurrentQuestion()
    {
        hasAnsweredCurrent = false;
        QuizQuestion q = questions[currentQuestionIndex];

        if (progressCounterText != null)
            progressCounterText.text = $"QUESTION {currentQuestionIndex + 1} / {questions.Length}";

        if (categoryBadgeText != null)
            categoryBadgeText.text = q.sceneCategory;

        if (questionPromptText != null)
            questionPromptText.text = q.prompt;

        if (feedbackExplanationText != null)
            feedbackExplanationText.text = "<color=#94A3B8>Select one of the 4 options above to verify your understanding.</color>";

        for (int i = 0; i < 4; i++)
        {
            if (optionTexts != null && i < optionTexts.Length && optionTexts[i] != null)
                optionTexts[i].text = q.options[i];

            if (optionBackgrounds != null && i < optionBackgrounds.Length && optionBackgrounds[i] != null)
                optionBackgrounds[i].color = colDefaultOption;

            if (optionButtons != null && i < optionButtons.Length && optionButtons[i] != null)
                optionButtons[i].interactable = true;
        }

        if (nextQuestionButton != null)
            nextQuestionButton.interactable = false;

        if (nextButtonLabel != null)
            nextButtonLabel.text = (currentQuestionIndex == questions.Length - 1) ? "Finish Quiz ►" : "Next Question ►";
    }

    public void SelectOption(int selectedIndex)
    {
        if (hasAnsweredCurrent || isQuizFinished) return;
        hasAnsweredCurrent = true;

        QuizQuestion q = questions[currentQuestionIndex];
        bool isCorrect = (selectedIndex == q.correctIndex);

        if (isCorrect)
            currentCorrectCount++;

        for (int i = 0; i < 4; i++)
        {
            if (optionBackgrounds != null && i < optionBackgrounds.Length && optionBackgrounds[i] != null)
            {
                if (i == q.correctIndex)
                    optionBackgrounds[i].color = colCorrectGreen;
                else if (i == selectedIndex && !isCorrect)
                    optionBackgrounds[i].color = colWrongOrange;
                else
                    optionBackgrounds[i].color = colDefaultOption;
            }

            if (optionButtons != null && i < optionButtons.Length && optionButtons[i] != null)
                optionButtons[i].interactable = false;
        }

        if (feedbackExplanationText != null)
        {
            string prefix = isCorrect
                ? "<color=#009E73><b>✓ CORRECT!</b></color> "
                : "<color=#E69F00><b>✕ INCORRECT.</b></color> ";
            feedbackExplanationText.text = prefix + q.explanation;
        }

        int livePercent = Mathf.RoundToInt(((float)currentCorrectCount / questions.Length) * 100f);
        if (attemptNumber == 1 && preScoreBadgeText != null)
            preScoreBadgeText.text = $"PRE: {livePercent}%";
        else if (attemptNumber > 1 && postScoreBadgeText != null)
        {
            postScoreBadgeText.text = $"POST: {livePercent}%";
            if (savedPreTestPercent >= 0 && learningGainBadgeText != null)
            {
                int liveGain = livePercent - savedPreTestPercent;
                string sign = (liveGain >= 0) ? "+" : "";
                learningGainBadgeText.text = $"GAIN: {sign}{liveGain}%";
            }
        }

        if (nextQuestionButton != null)
            nextQuestionButton.interactable = true;
    }

    public void NextQuestion()
    {
        if (!hasAnsweredCurrent) return;

        if (currentQuestionIndex < questions.Length - 1)
        {
            currentQuestionIndex++;
            LoadCurrentQuestion();
        }
        else
        {
            FinishQuizAndShowResults();
        }
    }

    private void FinishQuizAndShowResults()
    {
        isQuizFinished = true;

        int finalPercent = Mathf.RoundToInt(((float)currentCorrectCount / questions.Length) * 100f);
        int mins = Mathf.FloorToInt(elapsedSeconds / 60f);
        int secs = Mathf.FloorToInt(elapsedSeconds % 60f);

        if (attemptNumber == 1)
        {
            savedPreTestPercent = finalPercent;
        }
        else
        {
            savedPostTestPercent = finalPercent;
        }

        UpdateTopBadgesUI();

        if (questionViewContainer != null) questionViewContainer.SetActive(false);
        if (resultsViewContainer != null)
        {
            resultsViewContainer.SetActive(true);
            resultsViewContainer.transform.SetAsLastSibling();
        }

        // Re-ensure Retake and Return buttons are on top and clickable
        if (retakeQuizButton != null)
        {
            retakeQuizButton.interactable = true;
            retakeQuizButton.transform.SetAsLastSibling();
        }
        if (returnToARButton != null)
        {
            returnToARButton.interactable = true;
            returnToARButton.transform.SetAsLastSibling();
        }

        int effectivePost = (savedPostTestPercent >= 0) ? savedPostTestPercent : finalPercent;
        int effectivePre = (savedPreTestPercent >= 0) ? savedPreTestPercent : 40;
        int gain = effectivePost - effectivePre;
        string gainSign = (gain >= 0) ? "+" : "";

        if (resultsTitleText != null)
        {
            resultsTitleText.text = (attemptNumber == 1)
                ? $"PRE-TEST COMPLETE (ATTEMPT #{attemptNumber})"
                : $"POST-TEST COMPLETE (ATTEMPT #{attemptNumber})";
        }

        if (resultsSummaryText != null)
        {
            resultsSummaryText.text =
                $"• <b>Questions Answered:</b> {currentCorrectCount} / {questions.Length} Correct (<b>{finalPercent}%</b>)\n" +
                $"• <b>Completion Time:</b> {mins:00}m {secs:00}s (Avg {(elapsedSeconds / questions.Length):0.0}s / question)\n" +
                $"• <b>Pre-Test Baseline Score:</b> {effectivePre}%\n" +
                $"• <b>Post-Test Score:</b> {(savedPostTestPercent >= 0 ? savedPostTestPercent + "%" : "Pending Retake")}\n\n" +
                ((attemptNumber == 1)
                    ? "<color=#38BDF8>Click <b>↻ Retake Quiz (Post-Test)</b> below right now or after exploring the AR scenes to record your Post-Test score and compute Learning Gain!</color>"
                    : "<color=#009E73>Great job! Your Pre-Test vs. Post-Test Learning Gain has been calculated above.</color>");
        }

        if (resultsGainHighlightText != null)
        {
            resultsGainHighlightText.text = (attemptNumber == 1)
                ? $"BASELINE RECORDED: {finalPercent}%  — Click 'Retake Quiz' below for Post-Test!"
                : $"LEARNING GAIN: {gainSign}{gain}%   (Target >= +20% : {(gain >= 20 ? "ACHIEVED ✓" : "COMPLETED")})";
        }
    }

    private void UpdateTopBadgesUI()
    {
        if (preScoreBadgeText != null)
            preScoreBadgeText.text = (savedPreTestPercent >= 0) ? $"PRE: {savedPreTestPercent}%" : "PRE: 0%";

        if (postScoreBadgeText != null)
            postScoreBadgeText.text = (savedPostTestPercent >= 0) ? $"POST: {savedPostTestPercent}%" : "POST: --%";

        if (learningGainBadgeText != null)
        {
            if (savedPreTestPercent >= 0 && savedPostTestPercent >= 0)
            {
                int diff = savedPostTestPercent - savedPreTestPercent;
                string sign = (diff >= 0) ? "+" : "";
                learningGainBadgeText.text = $"GAIN: {sign}{diff}%";
            }
            else
            {
                learningGainBadgeText.text = "GAIN: --%";
            }
        }
    }

    public void CloseQuizBackToAR()
    {
        if (quizPanel != null)
            quizPanel.SetActive(false);
    }

    private void Initialize20Questions()
    {
        questions = new QuizQuestion[20]
        {
            new QuizQuestion("SCENE 1 · WATERFALL MODEL",
                "Q1. In the Waterfall model, what happens when a critical requirement flaw is discovered during Step 4 (System Testing)?",
                "A) It is patched immediately without changing design docs",
                "B) The project must jump back to Requirements, incurring heavy rework cost",
                "C) It is deferred to the next 2-week Sprint backlog",
                "D) The CI/CD pipeline automatically fixes the requirement",
                1, "Waterfall phases are strictly sequential; late requirement changes invalidate frozen design and code (+100h rework)."),

            new QuizQuestion("SCENE 1 · WATERFALL MODEL",
                "Q2. Which precondition must be satisfied before Step 2 (System Design) begins in a strict Waterfall lifecycle?",
                "A) 100% of client requirements are gathered and frozen upfront",
                "B) A working prototype is deployed to production",
                "C) Unit tests reach 90% code coverage",
                "D) Velocity story points are estimated by the Scrum Master",
                0, "Waterfall locks the Software Requirements Specification (SRS) completely before architectural design starts."),

            new QuizQuestion("SCENE 1 · WATERFALL MODEL",
                "Q3. When does the client first see working, executable software in a traditional Waterfall project?",
                "A) At the end of every 2-week iteration",
                "B) Immediately after Step 2 (System Design)",
                "C) Only at Step 5 (Deployment) at the very end of the timeline",
                "D) Whenever the Kanban WIP column is full",
                2, "Waterfall delivers the entire monolithic system at the end of the project, delaying user feedback."),

            new QuizQuestion("SCENE 1 · WATERFALL MODEL",
                "Q4. For which type of software project is the Waterfall model still appropriate?",
                "A) A startup mobile app with rapidly changing market needs",
                "B) Safety-critical aviation software with fixed, stable regulatory requirements",
                "C) An experimental AI interface with unknown user workflows",
                "D) A social media platform releasing daily UI experiments",
                1, "Waterfall works well when requirements are completely stable, predictable, and subject to strict regulatory sign-off."),

            new QuizQuestion("SCENE 2 · AGILE SCRUM SPRINT",
                "Q5. How does an Agile Scrum team handle a new client feature request mid-way through an active 2-week Sprint?",
                "A) Abort the Sprint immediately and restart from scratch",
                "B) Reject all client changes permanently",
                "C) Add the user story to the Product Backlog for the next Sprint Planning (+5h)",
                "D) Delete automated unit tests to save time",
                2, "Scrum protects the active Sprint goal while welcoming new stories into the Product Backlog for the next iteration."),

            new QuizQuestion("SCENE 2 · AGILE SCRUM SPRINT",
                "Q6. What is the primary output of every 2-week Agile Scrum Sprint cycle?",
                "A) A 300-page frozen architecture document",
                "B) A potentially shippable, tested product increment",
                "C) A Gantt chart for the next 18 months",
                "D) A final sign-off contract closing the project",
                1, "Every Sprint concludes with a working, tested increment demonstrated during the Sprint Review."),

            new QuizQuestion("SCENE 2 · AGILE SCRUM SPRINT",
                "Q7. In Scene 2, what does a Sprint Burndown Chart track across the 2-week timebox?",
                "A) Total server electricity consumption",
                "B) Remaining Story Points (e.g., 34 pts down to 0 pts) over Sprint days",
                "C) Number of lines of code written per developer",
                "D) Employee vacation hours",
                1, "A Burndown Chart plots remaining work (Story Points) against remaining days in the Sprint."),

            new QuizQuestion("SCENE 2 · AGILE SCRUM SPRINT",
                "Q8. Which Scrum ceremony focuses on inspecting how the team collaborated and improving processes for the next Sprint?",
                "A) Sprint Retrospective",
                "B) Daily Stand-up (Daily Scrum)",
                "C) Requirement Freezing Ceremony",
                "D) Monolithic Integration Sign-off",
                0, "The Sprint Retrospective occurs after the Sprint Review so the team can continuously improve its workflow."),

            new QuizQuestion("SCENE 3 · KANBAN & WIP LIMITS",
                "Q9. In Scene 3, why does the Kanban 'In Progress' column enforce a strict Work-In-Progress (WIP) Limit of 2 cards?",
                "A) To reduce the number of developers on the team",
                "B) To prevent multitasking bottlenecks and reduce task cycle time",
                "C) To force developers to work overtime",
                "D) To stop QA engineers from testing code",
                1, "Capping WIP prevents context-switching overload and exposes downstream bottlenecks early."),

            new QuizQuestion("SCENE 3 · KANBAN & WIP LIMITS",
                "Q10. According to Little's Law in Kanban flow, what happens to Lead Time when Work-In-Progress (WIP) exceeds capacity?",
                "A) Lead Time drops to zero immediately",
                "B) Average Lead Time and Cycle Time increase due to queuing delays",
                "C) Code compiles twice as fast",
                "D) Unit tests are skipped automatically",
                1, "Higher WIP without increased throughput causes tasks to wait in queues, lengthening cycle time (+15h delay)."),

            new QuizQuestion("SCENE 3 · KANBAN & WIP LIMITS",
                "Q11. If the 'In Progress (WIP 2/2)' column is full and a developer finishes their task, what happens in a Pull system?",
                "A) They push 3 more cards into 'In Progress' immediately",
                "B) QA pulls the finished card into Review before a new 'To Do' card is pulled",
                "C) The entire board is erased",
                "D) The developer starts a new project",
                1, "Kanban is a pull system: capacity must open in a column before upstream work is pulled forward."),

            new QuizQuestion("SCENE 3 · KANBAN & WIP LIMITS",
                "Q12. Unlike Scrum, how does a pure Kanban workflow schedule work delivery?",
                "A) Continuous flow measured by Cycle Time rather than fixed 2-week Sprints",
                "B) One giant release every 2 years",
                "C) Only releasing code on leap years",
                "D) Freezing the board for 6 months",
                0, "Kanban optimizes continuous flow and cycle time (e.g., 2.4 days/card) without mandatory sprint timeboxes."),

            new QuizQuestion("SCENE 4 · CI/CD DEVOPS PIPELINE",
                "Q13. In Scene 4, what happens when a single automated Unit Test fails at Stage 3 of the CI/CD pipeline?",
                "A) The broken code is deployed to Production anyway",
                "B) The pipeline halts automatically at the Unit Test gate, blocking Production release",
                "C) The Git repository is deleted",
                "D) All previous requirements are rewritten",
                1, "Automated quality gates stop defective builds immediately (+2h fix) before they can impact end users."),

            new QuizQuestion("SCENE 4 · CI/CD DEVOPS PIPELINE",
                "Q14. Why is fixing a bug caught by an automated CI gate (+2 hrs) far cheaper than fixing a production bug?",
                "A) The developer still has fresh context and no live customer data is corrupted",
                "B) CI servers write all the application features by themselves",
                "C) Production servers do not use databases",
                "D) Unit tests replace the need for software architecture",
                0, "Catching defects seconds after 'git push' avoids production rollbacks, outages, and context switching."),

            new QuizQuestion("SCENE 4 · CI/CD DEVOPS PIPELINE",
                "Q15. What is the role of Stage 4 (Staging Canary / Pre-Production) in the CI/CD pipeline?",
                "A) Writing the initial client contract",
                "B) Running automated smoke tests in a production-like mirror before going live",
                "C) Designing company logos",
                "D) Manually compiling code on floppy disks",
                1, "Staging validates containerized builds under realistic environment configuration before Blue/Green production rollout."),

            new QuizQuestion("SCENE 4 · CI/CD DEVOPS PIPELINE",
                "Q16. Which practice triggers Stage 1 of the Continuous Integration pipeline?",
                "A) Developers pushing small, frequent code commits to a shared version-control branch",
                "B) Waiting 6 months to merge all developer code at once",
                "C) Printing source code on paper for manual review",
                "D) Disabling version control",
                0, "Continuous Integration relies on frequent, small merges verified automatically on every commit."),

            new QuizQuestion("SCENE 5 · W vs A (60% CHANGE)",
                "Q17. In Scene 5, when the client changes authentication requirements at 60% completion, why does Waterfall spike to 125 hrs of rework?",
                "A) Because Waterfall already shipped 3 working increments",
                "B) Because monolithic design and code were tightly coupled to the frozen 0% specification",
                "C) Because Waterfall uses automated CI/CD containers",
                "D) Because Waterfall teams have no project manager",
                1, "Locking architecture upfront means a 60% mid-project pivot forces tearing down completed design and code layers."),

            new QuizQuestion("SCENE 5 · W vs A (60% CHANGE)",
                "Q18. At the 60% timeline mark in Scene 5, what has the Agile team already delivered compared to Waterfall?",
                "A) 3 working, user-tested Sprint increments in production (vs. 0 in Waterfall)",
                "B) Only requirement documents with zero code",
                "C) 125 hours of uncompiled pseudocode",
                "D) Nothing until 100% completion",
                0, "Iterative delivery ships value early and continuously, reducing risk if requirements pivot at 60%."),

            new QuizQuestion("SCENE 5 · W vs A (60% CHANGE)",
                "Q19. How much rework cost does Agile incur during the 60% requirement pivot compared to Waterfall's 125 hrs?",
                "A) 250 hrs (double Waterfall)",
                "B) 125 hrs (identical to Waterfall)",
                "C) Only ~15 hrs (an 88% reduction in rework cost)",
                "D) 500 hrs",
                2, "Because Agile architecture evolves modularly per Sprint, adapting Sprint 4 takes ~15 hrs instead of 125 hrs."),

            new QuizQuestion("SCENE 5 · W vs A (60% CHANGE)",
                "Q20. Synthesizing all 5 AR scenes, which combination minimizes rework risk for evolving software products?",
                "A) Big-Design-Up-Front Waterfall with manual end-of-year testing",
                "B) Iterative Agile Sprints + Kanban WIP flow limits + automated CI/CD test gates",
                "C) Removing all testing and pushing directly to production",
                "D) Unlimited WIP multitasking without version control",
                1, "Combining Agile iterations, WIP limits, and automated CI/CD gates maximizes adaptability and quality!")
        };
    }
}