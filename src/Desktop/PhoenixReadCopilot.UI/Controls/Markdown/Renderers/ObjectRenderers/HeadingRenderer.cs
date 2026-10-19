using Markdig.Syntax;
using PhoenixReadCopilot.UI.Controls.Markdown.TextElements;

namespace PhoenixReadCopilot.UI.Controls.Markdown.Renderers.ObjectRenderers;

internal class HeadingRenderer : WinUIObjectRenderer<HeadingBlock>
{
    protected override void Write(WinUIRenderer renderer, HeadingBlock obj)
    {
        if (renderer == null)
        {
            throw new ArgumentNullException(nameof(renderer));
        }

        if (obj == null)
        {
            throw new ArgumentNullException(nameof(obj));
        }

        var paragraph = new MyHeading(obj, renderer.Config);
        renderer.Push(paragraph);
        renderer.WriteLeafInline(obj);
        renderer.Pop();
    }
}
