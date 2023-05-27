using BindableProps;

namespace NuSocial.Controls;

public enum OrientationLayout
{
    Horizontal,
    Vertical,
    InvertedHorizontal,
    InvertedVertical
}

public enum IconAttribute
{
    Line,
    Fill
}

public partial class IconButton : ContentView
{
    public enum Position
    {
        Left,
        Right
    }

    public event EventHandler Clicked;

    public IconButton()
    {
        InitializeComponent();
    }

    private void OnButtonClicked(object sender, EventArgs e)
    {
        Clicked?.Invoke(this, EventArgs.Empty);
    }

    [BindableProp]
    private Style? _buttonStyle;

    [BindableProp]
    private Command? _command;

    [BindableProp]
    private object? _commandParameter;

    [BindableProp(PropertyChangedDelegate = nameof(UpdateIconPosition))]
    private Position _iconPosition = Position.Left;

    [BindableProp(PropertyChangedDelegate = nameof(UpdateBorderColor))]
    private Color? _borderColor;

    [BindableProp(PropertyChangedDelegate = nameof(UpdateTextColor))]
    private Color? _textColor;

    [BindableProp]
    private string? _text;

    [BindableProp]
    private string _iconFontFamily;

    [BindableProp]
    private string? _iconText;

    [BindableProp]
    private Color _iconColor = Colors.Black;

    [BindableProp]
    private double _iconFontSize = 11d;

    [BindableProp(PropertyChangedDelegate = nameof(UpdateBackgroundColor))] 
    private Color? _backgroundColor;

    [BindableProp(PropertyChangedDelegate = nameof(IconAttributeChanged))]
    private IconAttribute _iconAttribute;

    private static void IconAttributeChanged(BindableObject bindable, object? oldValue = null, object? newValue = null)
    {
        if (bindable is IconButton b)
        {             
            if (b.IconAttribute == IconAttribute.Line)
            {
                b.icon.SetDynamicResource(StyleProperty, "FontIcon");
            }
            else
            {
                b.icon.SetDynamicResource(StyleProperty, "FontIconFill");
            }
        }
    }

    private static void UpdateBackgroundColor(BindableObject bindable, object? oldValue = null, object? newValue = null)
    {
    }

    private static void UpdateTextColor(BindableObject bindable, object? oldValue = null, object? newValue = null)
    {
    }

    private static void UpdateBorderColor(BindableObject bindable, object? oldValue = null, object? newValue = null)
    {
    }

    private static void UpdateIconPosition(BindableObject bindable, object? oldValue = null, object? newValue = null)
    {
        if (bindable is IconButton b)
        {
            if (((IconButton)b).IconPosition == Position.Left)
            {
                Grid.SetColumn(((IconButton)b).icon, 0);
                ((IconButton)b).icon.HorizontalOptions = LayoutOptions.End;
            }
            else
            {
                Grid.SetColumn(((IconButton)b).icon, 2);
                ((IconButton)b).icon.HorizontalOptions = LayoutOptions.Start;
            }
        }
    }
}