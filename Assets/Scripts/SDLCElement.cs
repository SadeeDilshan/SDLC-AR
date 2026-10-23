using UnityEngine;

public class SDLCElement : MonoBehaviour
{
    [Header("SE2010 Phase Details")]
    public int stepIndex; // 0 = REQ, 1 = DESIGN, 2 = CODE, 3 = TEST, 4 = DEPLOY
    public string phaseCode = "REQ";
    public string phaseTitle = "1. Requirements Engineering";
    
    [TextArea(3, 5)]
    public string definition = "In Waterfall, all client requirements are locked upfront in an SRS document before design begins. In Scrum, requirements evolve continuously in a prioritized Product Backlog.";

    [TextArea(2, 4)]
    public string codeSnippet = ">> 1: GatherRequirements(backlog);";
}