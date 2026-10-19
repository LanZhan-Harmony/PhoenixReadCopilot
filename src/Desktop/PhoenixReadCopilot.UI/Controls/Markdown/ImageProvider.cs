namespace PhoenixReadCopilot.UI.Controls.Markdown;

public interface IImageProvider
{
    Task<Image> GetImage(string url);
    bool ShouldUseThisProvider(string url);
}
