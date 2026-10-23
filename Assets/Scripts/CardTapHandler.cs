using UnityEngine;

public class CardTapHandler : MonoBehaviour
{
    void OnMouseDown()
    {
        // This changes the color to green when tapped to confirm interaction
        GetComponent<Renderer>().material.color = Color.green;
        Debug.Log("Task card tapped!");
    }
}
