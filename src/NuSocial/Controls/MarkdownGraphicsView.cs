using BindableProps;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.Maui.Graphics.Text;
using Microsoft.Maui.Graphics.Text.Renderer;
using Font = Microsoft.Maui.Graphics.Font;
using GHorizontalAlignment = Microsoft.Maui.Graphics.HorizontalAlignment;
using GVerticalAlignment = Microsoft.Maui.Graphics.VerticalAlignment;

namespace NuSocial.Controls
{
    public class MarkdownDrawable : IDrawable
    {
        private readonly string _text;
        private readonly Color _fontColor;
        private readonly float _fontSize;
        private readonly int _markdownWidth;
        private readonly int _markdownHeight;

        public MarkdownDrawable(string text, Color fontColor, double fontSize, double markdownWidth, double markdownHeight)
        {
            _text = text;
            _fontColor = fontColor;
            _fontSize = (float)fontSize;
            _markdownWidth = (int)markdownWidth;
            _markdownHeight = (int)markdownHeight;
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            if (canvas is null)
            {
                throw new ArgumentNullException(nameof(canvas));
            }

            canvas.SaveState();

            canvas.Font = Font.Default;
            canvas.FontSize = _fontSize;
            canvas.FontColor = _fontColor;

            var x = dirtyRect.X;
            var y = dirtyRect.Y;
            var width = _markdownWidth > 0 ? _markdownWidth : dirtyRect.Width;
            var height = _markdownHeight > 0 ? _markdownHeight : dirtyRect.Height;

            var attributedText = Read(_text);
#if WINDOWS
		canvas.DrawString(attributedText.Text, x, y, Math.Max(0, width), Math.Max(0, height), GHorizontalAlignment.Center, GVerticalAlignment.Center);
#else
            canvas.DrawText(attributedText, x, y, width, height);
#endif

            canvas.RestoreState();
        }

        private static IAttributedText Read(string text)
        {
            var renderer = new AttributedTextRenderer();
            renderer.ObjectRenderers.Add(new MauiCodeInlineRenderer());
            renderer.ObjectRenderers.Add(new MauiCodeBlockRenderer());
            renderer.ObjectRenderers.Add(new MauiHeadingRenderer());
            var builder = new MarkdownPipelineBuilder()
                          .UseEmojiAndSmiley()
                          .UseEmphasisExtras();
            var pipeline = builder.Build();
            Markdown.Convert(text, renderer, pipeline);
            return renderer.GetAttributedText();
        }
    }

    public class MauiCodeBlockRenderer : AttributedTextObjectRenderer<CodeBlock>
    {
        protected override void Write(AttributedTextRenderer renderer, CodeBlock obj)
        {
            if (renderer is null)
            {
                throw new ArgumentNullException(nameof(renderer));
            }

            if (obj is null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            var start = renderer.Count;
            var attributes = new TextAttributes();
            attributes.SetBackgroundColor("#f5f2f0");
            var randomColor = new Color(Random.Shared.Next(100), Random.Shared.Next(100), Random.Shared.Next(100));
            attributes.SetForegroundColor(randomColor.ToArgbHex());

            if (obj.Lines.Lines != null)
            {
                var lines = obj.Lines;
                var slices = lines.Lines;
                for (int i = 0; i < lines.Count; i++)
                {
                    renderer.Write(ref slices[i].Slice);
                    renderer.WriteLine();
                }
            }

            var length = renderer.Count - start;
            renderer.Call("AddTextRun", start, length, attributes);
        }
    }

    public class MauiCodeInlineRenderer : AttributedTextObjectRenderer<CodeInline>
    {
        protected override void Write(AttributedTextRenderer renderer, CodeInline obj)
        {
            if (renderer is null)
            {
                throw new ArgumentNullException(nameof(renderer));
            }

            if (obj is null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            var start = renderer.Count;
            var attributes = new TextAttributes();
            attributes.SetForegroundColor("#d63384");
            attributes.SetFontSize(35f);
            renderer.Write(obj.Content);
            var length = renderer.Count - start;
            renderer.Call("AddTextRun", start, length, attributes);
        }
    }

    public class MauiHeadingRenderer : AttributedTextObjectRenderer<HeadingBlock>
    {
        protected override void Write(AttributedTextRenderer renderer, HeadingBlock obj)
        {
            if (renderer is null)
            {
                throw new ArgumentNullException(nameof(renderer));
            }

            if (obj is null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            var start = renderer.Count;
            var attributes = new TextAttributes();
            attributes.SetFontSize(obj.Level switch
            {
                1 => 24,
                2 => 20,
                3 => 16,
                4 => 14,
                5 => 12,
                6 => 10,
                _ => 8
            });
            if (obj.Line > 0)
            {
                renderer.WriteLine();
            }

            renderer.WriteLeafInline(obj);
            renderer.WriteLine();
            var length = renderer.Count - start;
            renderer.Call("AddTextRun", start, length, attributes);
        }
    }

    public partial class MarkdownGraphicsView : GraphicsView
    {
        [BindableProp(PropertyChangedDelegate = nameof(OnTextChanged))]
        private string? _text;

        [BindableProp(PropertyChangedDelegate = nameof(OnFontSizeChanged))]
        private float _fontSize;

        [BindableProp(PropertyChangedDelegate = nameof(OnFontColorChanged))]
        private Color _fontColor = Colors.Black;

        private static void OnTextChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is MarkdownGraphicsView control)
            {
                control.Render();
            }
        }

        private static void OnFontSizeChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is MarkdownGraphicsView control)
            {
                control.Render();
            }
        }

        private static void OnFontColorChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is MarkdownGraphicsView control)
            {
                control.Render();
            }
        }

        private void Render()
        {
            Drawable = new MarkdownDrawable(Text ?? string.Empty, FontColor, FontSize, Width, Height);
        }
    }

    internal static class AccessExtensions
    {
        public static void Call(this object o, string methodName, params object[] args)
        {
            var mi = o.GetType().GetMethod(methodName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            mi?.Invoke(o, args);
        }
    }
}