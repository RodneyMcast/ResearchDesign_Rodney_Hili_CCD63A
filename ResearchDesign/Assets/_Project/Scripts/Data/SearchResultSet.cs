using UnityEngine;

[CreateAssetMenu(menuName = "AgenticBrowser/Search Result Set")]
public class SearchResultSet : ScriptableObject
{
    public string queryKey; 

    [Header("Assistant Tab")]
    public Sprite assistantClosed;
    public Sprite assistantOpen;

    [Header("Links Tab")]
    public Sprite linksClosed;
    public Sprite linksOpen;

    [Header("Images Tab")]
    public Sprite imagesClosed;
    public Sprite imagesOpen;
}
