namespace AVFM.Models;

public class TabItemHeader
{
    public TabItemHeader(string text, string icon)
    {
        Text = text;
        Icon = icon;
    }

    public string Text {get; set; }
    public string Icon { get; set; }
}