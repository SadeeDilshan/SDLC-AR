using UnityEngine;
using TMPro;

public class QuizManager : MonoBehaviour
{
    [System.Serializable]
    public struct Question
    {
        public string prompt;
        public string[] options; // 4 options
        public int correctIndex;
        public string explanation;
    }

    public TMP_Text questionText;
    public TMP_Text[] optionButtonTexts;
    public TMP_Text feedbackText;
    public TMP_Text preScoreText;
    public TMP_Text postScoreText;
    public TMP_Text learningGainText;

    private int currentQuestionIndex = 0;
    private int preScore = 0;
    private int postScore = 0;
    private bool isPostQuiz = false;

    private Question[] questions = new Question[]
    {
        new Question {
            prompt = "Q1: A medical pacemaker has 100% fixed safety regulations before coding starts. Which SDLC model fits best?",
            options = new string[] { "A. Waterfall", "B. Scrum", "C. Extreme Programming", "D. Kanban" },
            correctIndex = 0,
            explanation = "CORRECT! Fixed, safety-critical specs suit Waterfall."
        },
        new Question {
            prompt = "Q2: During Day 4 of a 10-day Scrum Sprint, the client asks for a new feature. What should the team do?",
            options = new string[] { "A. Add it to the active Sprint immediately", "B. Restart the Sprint", "C. Put it in the Product Backlog for the next Sprint", "D. Delete testing tasks" },
            correctIndex = 2,
            explanation = "CORRECT! Mid-sprint requests go to the Product Backlog so the Sprint Goal is protected."
        },
        new Question {
            prompt = "Q3: On a Kanban board, QA has a WIP limit of 2/2 and is full. Developers finish coding Task T3. What happens?",
            options = new string[] { "A. Force T3 into QA anyway", "B. Coding cannot push to QA; team swarms to help clear the QA bottleneck", "C. Increase QA WIP limit to 10", "D. Skip QA and deploy" },
            correctIndex = 1,
            explanation = "CORRECT! WIP limits expose bottlenecks and stop overproduction."
        },
        new Question {
            prompt = "Q4: In a CI/CD pipeline, a developer's commit fails the Automated Unit Test gate. What happens next?",
            options = new string[] { "A. It deploys to Production anyway", "B. Pipeline halts immediately and blocks Staging/Production deploy", "C. It deletes the repository", "D. It skips to manual release" },
            correctIndex = 1,
            explanation = "CORRECT! Automated gates prevent defective code from reaching production."
        },
        new Question {
            prompt = "Q5: Why does injecting a requirement change at 60% completion cause higher rework in Waterfall than Agile?",
            options = new string[] { "A. Waterfall phases are tightly coupled and sequential, forcing upstream design/code rewrites", "B. Agile has no testing", "C. Waterfall uses Git", "D. Agile ignores clients" },
            correctIndex = 0,
            explanation = "CORRECT! Frozen sequential phases make late requirement changes exponentially more expensive."
        }
    };

    void Start()
    {
        preScore = PlayerPrefs.GetInt("PreQuizScore", 40);
        postScore = PlayerPrefs.GetInt("PostQuizScore", 60);
        UpdateScoreBoard();
        LoadQuestion(0);
    }

    public void LoadQuestion(int index)
    {
        currentQuestionIndex = index % questions.Length;
        Question q = questions[currentQuestionIndex];
        if (questionText != null) questionText.text = q.prompt;
        for (int i = 0; i < 4; i++)
        {
            if (optionButtonTexts != null && i < optionButtonTexts.Length)
                optionButtonTexts[i].text = q.options[i];
        }
        if (feedbackText != null) feedbackText.text = "";
    }

    public void SubmitAnswer(int chosenIndex)
    {
        Question q = questions[currentQuestionIndex];
        if (chosenIndex == q.correctIndex)
        {
            if (feedbackText != null)
                feedbackText.text = $"<color=#009E73>{q.explanation}</color>";
            if (!isPostQuiz) preScore = Mathf.Min(100, preScore + 20);
            else postScore = Mathf.Min(100, postScore + 20);
        }
        else
        {
            if (feedbackText != null)
                feedbackText.text = $"<color=#D55E00>Not quite. Review the AR simulation: {q.explanation}</color>";
        }

        PlayerPrefs.SetInt("PreQuizScore", preScore);
        PlayerPrefs.SetInt("PostQuizScore", postScore);
        UpdateScoreBoard();
    }

    public void NextQuestion()
    {
        LoadQuestion(currentQuestionIndex + 1);
    }

    public void SwitchToPostQuiz(bool postMode)
    {
        isPostQuiz = postMode;
        if (!postMode) preScore = 0; else postScore = 0;
        LoadQuestion(0);
    }

    private void UpdateScoreBoard()
    {
        if (preScoreText != null) preScoreText.text = $"{preScore}%";
        if (postScoreText != null) postScoreText.text = $"{postScore}%";
        int gain = postScore - preScore;
        if (learningGainText != null)
            learningGainText.text = (gain >= 0 ? "+" : "") + $"{gain}%";
    }
}