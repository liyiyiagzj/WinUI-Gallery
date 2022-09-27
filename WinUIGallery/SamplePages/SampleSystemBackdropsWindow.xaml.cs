using Windows.Foundation.Metadata;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System.Runtime.InteropServices; // For DllImport
using WinRT; // 需要支持 Window.As<ICompositionSupportsSystemBackdrop>()

namespace AppUIBasics.SamplePages
{
    class WindowsSystemDispatcherQueueHelper
    {
        [StructLayout(LayoutKind.Sequential)]
        struct DispatcherQueueOptions
        {
            internal int dwSize;
            internal int threadType;
            internal int apartmentType;
        }
        
        [DllImport("CoreMessaging.dll")]
        private static extern int CreateDispatcherQueueController([In] DispatcherQueueOptions options, [In, Out, MarshalAs(UnmanagedType.IUnknown)] ref object dispatcherQueueController);

        object m_dispatcherQueueController = null;
        public void EnsureWindowsSystemDispatcherQueueController()
        {
            if (Windows.System.DispatcherQueue.GetForCurrentThread() != null)
            {
                // 一个已经存在，所以我们将使用它。
                return;
            }

            if (m_dispatcherQueueController == null)
            {
                DispatcherQueueOptions options;
                options.dwSize = Marshal.SizeOf(typeof(DispatcherQueueOptions));
                options.threadType = 2;    // DQTYPE_THREAD_CURRENT
                options.apartmentType = 2; // DQTAT_COM_STA

                CreateDispatcherQueueController(options, ref m_dispatcherQueueController);
            }
        }
    }

    public sealed partial class SampleSystemBackdropsWindow : Window
    {
        public SampleSystemBackdropsWindow()
        {
            this.InitializeComponent();
            ((FrameworkElement)this.Content).RequestedTheme = AppUIBasics.Helper.ThemeHelper.RootTheme;

            m_wsdqHelper = new WindowsSystemDispatcherQueueHelper();
            m_wsdqHelper.EnsureWindowsSystemDispatcherQueueController();

            SetBackdrop(BackdropType.Mica);
        }


        public enum BackdropType
        {
            Mica,
            DesktopAcrylic,
            DefaultColor,
        }

        WindowsSystemDispatcherQueueHelper m_wsdqHelper;
        BackdropType m_currentBackdrop;
        Microsoft.UI.Composition.SystemBackdrops.MicaController m_micaController;
        Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicController m_acrylicController;
        Microsoft.UI.Composition.SystemBackdrops.SystemBackdropConfiguration m_configurationSource;

        public void SetBackdrop(BackdropType type)
        {
            // 重置为默认颜色。 如果支持请求的类型，我们将更新到该类型。
            // 注意：此示例完全删除任何以前的控制器以重置为默认状态。
            // 这样做是为了让这个示例可以显示应用程序最常见的模式，
            // 只需选择它在启动时设置的一种控制器类型。
            // 如果应用想要在 Mica 和 Acrylic 之间切换，
            // 它可以简单地在旧控制器上调用 RemoveSystemBackdropTarget()，
            // 然后设置新控制器，
            // 重用任何现有的 m_configurationSource 和 Activated/Closed 事件处理程序。
            m_currentBackdrop = BackdropType.DefaultColor;
            tbCurrentBackdrop.Text = "None (default theme color)";
            tbChangeStatus.Text = "";
            if (m_micaController != null)
            {
                m_micaController.Dispose();
                m_micaController = null;
            }
            if (m_acrylicController != null)
            {
                m_acrylicController.Dispose();
                m_acrylicController = null;
            }
            this.Activated -= Window_Activated;
            this.Closed -= Window_Closed;
            ((FrameworkElement)this.Content).ActualThemeChanged -= Window_ThemeChanged;
            m_configurationSource = null;

            if (type == BackdropType.Mica)
            {
                if (TrySetMicaBackdrop())
                {
                    tbCurrentBackdrop.Text = "Mica";
                    m_currentBackdrop = type;
                }
                else
                {
                    // 不支持云母。 试试亚克力。
                    type = BackdropType.DesktopAcrylic;
                    tbChangeStatus.Text += "  Mica isn't supported. Trying Acrylic.";
                }
            }
            if (type == BackdropType.DesktopAcrylic)
            {
                if (TrySetAcrylicBackdrop())
                {
                    tbCurrentBackdrop.Text = "Acrylic";
                    m_currentBackdrop = type;
                }
                else
                {
                    // 不支持亚克力，所以选择下一个选项，即默认颜色，它已经设置好了。
                    tbChangeStatus.Text += "  Acrylic isn't supported. Switching to default color.";
                }
            }
        }

        bool TrySetMicaBackdrop()
        {
            if (Microsoft.UI.Composition.SystemBackdrops.MicaController.IsSupported())
            {
                // 连接策略对象
                m_configurationSource = new Microsoft.UI.Composition.SystemBackdrops.SystemBackdropConfiguration();
                this.Activated += Window_Activated;
                this.Closed += Window_Closed;
                ((FrameworkElement)this.Content).ActualThemeChanged += Window_ThemeChanged;

                // 初始配置状态。
                m_configurationSource.IsInputActive = true;
                SetConfigurationSourceTheme();

                m_micaController = new Microsoft.UI.Composition.SystemBackdrops.MicaController();

                // 启用系统背景。
                // 注意：一定要有“using WinRT;” 支持 Window.As<...>() 调用。
                m_micaController.AddSystemBackdropTarget(this.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>());
                m_micaController.SetSystemBackdropConfiguration(m_configurationSource);
                return true; // 成功了
            }

            return false; // 此系统不支持 Mica
        }

        bool TrySetAcrylicBackdrop()
        {
            if (Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicController.IsSupported())
            {
                // 连接策略对象
                m_configurationSource = new Microsoft.UI.Composition.SystemBackdrops.SystemBackdropConfiguration();
                this.Activated += Window_Activated;
                this.Closed += Window_Closed;
                ((FrameworkElement)this.Content).ActualThemeChanged += Window_ThemeChanged;

                // 初始配置状态。
                m_configurationSource.IsInputActive = true;
                SetConfigurationSourceTheme();

                m_acrylicController = new Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicController();

                // 启用系统背景。
                // 注意：一定要有“using WinRT;” 支持 Window.As<...>() 调用。
                m_acrylicController.AddSystemBackdropTarget(this.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>());
                m_acrylicController.SetSystemBackdropConfiguration(m_configurationSource);
                return true; // 成功了
            }

            return false; // 此系统不支持亚克力
        }

        private void Window_Activated(object sender, WindowActivatedEventArgs args)
        {
            m_configurationSource.IsInputActive = args.WindowActivationState != WindowActivationState.Deactivated;
        }

        private void Window_Closed(object sender, WindowEventArgs args)
        {
            // 确保所有 Mica/Acrylic 控制器都已处理好，这样它就不会尝试使用这个关闭的窗口。
            if (m_micaController != null)
            {
                m_micaController.Dispose();
                m_micaController = null;
            }
            if (m_acrylicController != null)
            {
                m_acrylicController.Dispose();
                m_acrylicController = null;
            }
            this.Activated -= Window_Activated;
            m_configurationSource = null;
        }

        private void Window_ThemeChanged(FrameworkElement sender, object args)
        {
            if (m_configurationSource != null)
            {
                SetConfigurationSourceTheme();
            }
        }

        private void SetConfigurationSourceTheme()
        {
            switch (((FrameworkElement)this.Content).ActualTheme)
            {
                case ElementTheme.Dark:    m_configurationSource.Theme = Microsoft.UI.Composition.SystemBackdrops.SystemBackdropTheme.Dark; break;
                case ElementTheme.Light:   m_configurationSource.Theme = Microsoft.UI.Composition.SystemBackdrops.SystemBackdropTheme.Light; break;
                case ElementTheme.Default: m_configurationSource.Theme = Microsoft.UI.Composition.SystemBackdrops.SystemBackdropTheme.Default; break;
            }
        }

        void ChangeBackdropButton_Click(object sender, RoutedEventArgs e)
        {
            BackdropType newType;
            switch (m_currentBackdrop)
            {
                case BackdropType.Mica:           newType = BackdropType.DesktopAcrylic; break;
                case BackdropType.DesktopAcrylic: newType = BackdropType.DefaultColor; break;
                default:
                case BackdropType.DefaultColor:   newType = BackdropType.Mica; break;
            }
            SetBackdrop(newType);
        }
    }
}
