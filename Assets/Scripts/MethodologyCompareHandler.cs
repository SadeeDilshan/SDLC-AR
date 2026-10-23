using UnityEngine;

public class MethodologyCompareHandler : MonoBehaviour
{
    private bool isHighlighted = false;

    void OnMouseDown()
    {
        isHighlighted = !isHighlighted;
        // Toggle color between cyan (selected) and original gray/white
        GetComponent<Renderer>().material.color = isHighlighted ? Color.cyan : Color.white;
        Debug.Log(gameObject.name + " selected for comparison inspection.");
    }
}
