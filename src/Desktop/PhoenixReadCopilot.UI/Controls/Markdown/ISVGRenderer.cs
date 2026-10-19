namespace PhoenixReadCopilot.UI.Controls.Markdown;

public interface ISVGRenderer
{
    Task<Image> SvgToImage(string svgString);
}
