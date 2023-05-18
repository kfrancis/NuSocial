using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Windows.System;

namespace NuSocial.Platforms.Windows
{
    public static class WindowHelpers
    {
        public static void TryMicaOrAcrylic(this Microsoft.UI.Xaml.Window window)
        {
            if (window is null)
            {
                throw new ArgumentNullException(nameof(window));
            }

            var dispatcherQueueHelper = new WindowsSystemDispatcherQueueHelper(); // in Platforms.Windows folder
            dispatcherQueueHelper.EnsureWindowsSystemDispatcherQueueController();

            // Hooking up the policy object
            var configurationSource = new SystemBackdropConfiguration
            {
                IsInputActive = true
            };

            switch (((FrameworkElement)window.Content).ActualTheme)
            {
                case ElementTheme.Dark:
                    configurationSource.Theme = SystemBackdropTheme.Dark;
                    break;
                case ElementTheme.Light:
                    configurationSource.Theme = SystemBackdropTheme.Light;
                    break;
                case ElementTheme.Default:
                    configurationSource.Theme = SystemBackdropTheme.Default;
                    break;
            }

            // Let's try Mica first
            if (MicaController.IsSupported())
            {
                var micaController = new MicaController();
                micaController.AddSystemBackdropTarget(window.As<ICompositionSupportsSystemBackdrop>());
                micaController.SetSystemBackdropConfiguration(configurationSource);

                window.Activated += (object sender, WindowActivatedEventArgs args) =>
                {
                    if (args.WindowActivationState is WindowActivationState.CodeActivated or WindowActivationState.PointerActivated)
                    {
                        // Handle situation where a window is activated and placed on top of other active windows.
                        if (micaController == null)
                        {
                            micaController = new MicaController();
                            micaController.AddSystemBackdropTarget(window.As<ICompositionSupportsSystemBackdrop>()); // Requires 'using WinRT;'
                            micaController.SetSystemBackdropConfiguration(configurationSource);
                        }
                        if (configurationSource != null)
                            configurationSource.IsInputActive = args.WindowActivationState != WindowActivationState.Deactivated;
                    }
                };

                window.Closed += (object sender, WindowEventArgs args) =>
                {
                    if (micaController != null)
                    {
                        micaController.Dispose();
                        micaController = null;
                    }
                    configurationSource = null;
                };
            }
            // If no Mica, maybe we can use Acrylic instead
            else if (DesktopAcrylicController.IsSupported())
            {
                var acrylicController = new DesktopAcrylicController();
                acrylicController.AddSystemBackdropTarget(window.As<ICompositionSupportsSystemBackdrop>());
                acrylicController.SetSystemBackdropConfiguration(configurationSource);

                window.Activated += (object sender, WindowActivatedEventArgs args) =>
                {
                    if (args.WindowActivationState is WindowActivationState.CodeActivated or WindowActivationState.PointerActivated)
                    {
                        // Handle situation where a window is activated and placed on top of other active windows.
                        if (acrylicController == null)
                        {
                            acrylicController = new DesktopAcrylicController();
                            acrylicController.AddSystemBackdropTarget(window.As<ICompositionSupportsSystemBackdrop>()); // Requires 'using WinRT;'
                            acrylicController.SetSystemBackdropConfiguration(configurationSource);
                        }
                    }
                    if (configurationSource != null)
                        configurationSource.IsInputActive = args.WindowActivationState != WindowActivationState.Deactivated;
                };

                window.Closed += (object sender, WindowEventArgs args) =>
                {
                    if (acrylicController != null)
                    {
                        acrylicController.Dispose();
                        acrylicController = null;
                    }
                    configurationSource = null;
                };
            }
        }
    }

    public class WindowsSystemDispatcherQueueHelper
    {
        [StructLayout(LayoutKind.Sequential)]
        struct DispatcherQueueOptions
        {
            internal int dwSize;
            internal int threadType;
            internal int apartmentType;
        }

        [DllImport(
            "CoreMessaging.dll",
            EntryPoint = "CreateDispatcherQueueController",
            SetLastError = true,
            CharSet = CharSet.Unicode,
            ExactSpelling = true,
            CallingConvention = CallingConvention.StdCall
            )]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static extern UInt32 CreateDispatcherQueueController([In] DispatcherQueueOptions options, [In, Out, MarshalAs(UnmanagedType.IUnknown)] ref object dispatcherQueueController);

        private object? _dispatcherQueueController;

        public void EnsureWindowsSystemDispatcherQueueController()
        {
            if (DispatcherQueue.GetForCurrentThread() != null)
            {
                // one already exists, so we'll just use it.
                return;
            }

            if (_dispatcherQueueController == null)
            {
                DispatcherQueueOptions options;

                options.dwSize = Marshal.SizeOf(typeof(DispatcherQueueOptions));
                options.threadType = 2;    // DQTYPE_THREAD_CURRENT
                options.apartmentType = 2; // DQTAT_COM_STA

                _ = CreateDispatcherQueueController(options, ref _dispatcherQueueController!);
                //if (hr == 0)
                //{
                //    var c = Marshal.GetObjectForIUnknown((IntPtr)_dispatcherQueueController) as DispatcherQueueController;
                //    Marshal.Release((IntPtr)_dispatcherQueueController);
                //}
            }
        }
    }
}
