using Markdig.Syntax.Inlines;

namespace PhoenixReadCopilot.UI.Controls.Markdown.Renderers.ObjectRenderers.Inlines;

internal class ContainerInlineRenderer : WinUIObjectRenderer<ContainerInline>
{
    protected override void Write(WinUIRenderer renderer, ContainerInline obj)
    {
        foreach (var inline in obj)
        {
            renderer.Write(inline);
        }
    }
}
