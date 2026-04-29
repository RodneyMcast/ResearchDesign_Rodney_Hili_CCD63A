using System;
using UnityEngine;

[Serializable]
public class HotspotSpec
{
    public string id = "assistant_button";
    public string actionId = "assistant_button_pressed";

    [Tooltip("Normalized rect relative to the background image. (0..1). (x,y,w,h)")]
    public Rect normalizedRect = new Rect(0.1f, 0.1f, 0.1f, 0.1f);
}
