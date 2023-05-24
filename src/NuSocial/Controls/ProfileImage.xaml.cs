using BindableProps;

namespace NuSocial.Controls;

public partial class ProfileImage : ContentView
{
    [BindableProp]
    private Aspect _aspect = Aspect.AspectFit;

    [BindableProp(PropertyChangedDelegate = nameof(UpdateSize))]
    private double _borderSize;

    [BindableProp(PropertyChangedDelegate = nameof(UpdateIsCircularImage))]
    private double _cornerRadius;

    [BindableProp(PropertyChangedDelegate = nameof(UpdateIsCircularImage))]
    private bool _isCircular = false;

    private double _lastCornerRadius;

    [BindableProp(PropertyChangedDelegate = nameof(UpdateSize))]
    private double _size = 50.0;

    [BindableProp(PropertyChangedDelegate = nameof(UpdateSource))]
    private string _source = string.Empty;

    [BindableProp]
    private bool _isAnimationPlaying = false;

    public ProfileImage()
    {
        InitializeComponent();

        //UpdateBadgeType();

        UpdateSize(this);

        //UpdateStatusIndicatorPosition();
    }

    private static void UpdateBorder(BindableObject bindable)
    {
        if (bindable is ProfileImage control)
        {
            if (control.Size > 0)
            {
                if (control.BorderSize == 0)
                {
                    //borderBoxView.IsVisible = false;
                }
                else
                {
                    var cornerRadius = control.CornerRadius + control.BorderSize;
                    var heightRequest = control.Size + (control.BorderSize * 2);
                    var widthRequest = control.Size + (control.BorderSize * 2);

                    //borderBoxView.IsVisible = true;

                    //if (UXDivers.Grial.Effects.GetCornerRadius(borderBoxView) != cornerRadius || borderBoxView.HeightRequest != heightRequest || borderBoxView.WidthRequest != widthRequest)
                    //{
                    //    borderBoxView.HeightRequest = heightRequest;
                    //    borderBoxView.WidthRequest = widthRequest;
                    //    UXDivers.Grial.Effects.SetCornerRadius(borderBoxView, cornerRadius);
                    //}
                }
            }
        }
    }

    private static void UpdateSource(BindableObject bindable, object? oldValue = null, object? newValue = null)
    {
        if (bindable is ProfileImage control)
        {
            if (newValue is string newStr && !string.IsNullOrEmpty(newStr))
            {
                control.Source = newStr;
                if (newStr.EndsWith(".gif", StringComparison.OrdinalIgnoreCase))
                {
                    control.IsAnimationPlaying = true;
                }
                else
                {
                    control.IsAnimationPlaying = false;
                }
            }
        }
    }

    private static void UpdateCornerRadius(BindableObject bindable, object? oldValue = null, object? newValue = null)
    {
        if (bindable is ProfileImage control)
        {
            if (control.IsCircular && control.CornerRadius != control.Size / 2)
            {
                control.CornerRadius = control.Size / 2;
            }

            UpdateBorder(bindable);
        }
    }

    private static void UpdateIsCircularImage(BindableObject bindable, object? oldValue = null, object? newValue = null)
    {
        if (bindable is ProfileImage control)
        {
            if (control.IsCircular)
            {
                control._lastCornerRadius = control.CornerRadius;
                control.CornerRadius = control.Size / 2;
            }
            else
            {
                control.CornerRadius = control._lastCornerRadius;
                control._lastCornerRadius = 0;
            }
        }
    }

    private static void UpdateSize(BindableObject bindable, object? oldValue = null, object? newValue = null)
    {
        if (bindable is ProfileImage control)
        {
            control.HeightRequest = control.Size + (control.BorderSize * 2);
            control.WidthRequest = control.Size + (control.BorderSize * 2);

            UpdateCornerRadius(bindable);
        }
    }
}