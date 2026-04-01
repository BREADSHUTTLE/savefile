using UnityEngine;

public interface IUITheme
{
    public Sprite inGameOptionBack { get; }
    public Color TitleTextColor { get; }
    public Color ToggleTextColor { get; }
   
    public Sprite inGameToggleOn { get; }
    public Sprite inGameToggleOff { get; }
    
   
}
