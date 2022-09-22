using System;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation.Metadata;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Windowing;
using AppUIBasics.Helper;

namespace AppUIBasics.TabViewPages
{
    public sealed partial class TabViewWindowingSamplePage : Page
    {
        private const string DataIdentifier = "MyTabItem";
        public TabViewWindowingSamplePage()
        {
            this.InitializeComponent();

            Tabs.TabItemsChanged += Tabs_TabItemsChanged;

            Loaded += TabViewWindowingSamplePage_Loaded;
        }

        private void TabViewWindowingSamplePage_Loaded(object sender, RoutedEventArgs e)
        {
            var currentWindow = WindowHelper.GetWindowForElement(this);
            currentWindow.ExtendsContentIntoTitleBar = true;
            currentWindow.SetTitleBar(CustomDragRegion);
            CustomDragRegion.MinWidth = 188;
        }

        private void Tabs_TabItemsChanged(TabView sender, Windows.Foundation.Collections.IVectorChangedEventArgs args)
        {
            // 如果没有更多选项卡，请关闭窗口。
            if (sender.TabItems.Count == 0)
            {
                WindowHelper.GetWindowForElement(this).Close();
            }
            // 如果仅剩下一个选项卡，请禁用标签的拖放和重新排序。
            else if (sender.TabItems.Count == 1)
            {
                sender.CanReorderTabs = false;
                sender.CanDragTabs = false;
            }
            else
            {
                sender.CanReorderTabs = true;
                sender.CanDragTabs = true;
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            SetupWindow();
        }

        void SetupWindow()
        {

            // 主窗口 - 添加一些默认项目
            for (int i = 0; i < 3; i++)
            {
                Tabs.TabItems.Add(new TabViewItem() { IconSource = new Microsoft.UI.Xaml.Controls.SymbolIconSource() { Symbol = Symbol.Placeholder }, Header = $"Item {i}", Content = new MyTabContentControl() { DataContext = $"Page {i}" } });
            }

            Tabs.SelectedIndex = 0;


#if UNIVERSAL
            // 扩展到标题栏（Extend into the titlebar）
            var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;

            coreTitleBar.LayoutMetricsChanged += CoreTitleBar_LayoutMetricsChanged;

            var titleBar = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().TitleBar;
            titleBar.ButtonBackgroundColor = Microsoft.UI.Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Microsoft.UI.Colors.Transparent;
#endif
        }

        private void CoreTitleBar_LayoutMetricsChanged(CoreApplicationViewTitleBar sender, object args)
        {
            // 为了确保标题栏中的标签不会被Shell Content遮住，
            // 我们必须确保计算左右覆盖。
            // 在LTR布局中，
            // 正确的插图包括标题按钮和拖动区域，
            // 该拖动区域以RTL为单位。

            // SystemOverLayleFtinset和SystemOverLayrightInset值在物理左右方面。
            // 因此，当我们的流动方向为RTL时，
            // 我们需要翻转。
            if (FlowDirection == FlowDirection.LeftToRight)
            {
                CustomDragRegion.MinWidth = sender.SystemOverlayRightInset;
                ShellTitleBarInset.MinWidth = sender.SystemOverlayLeftInset;
            }
            else
            {
                CustomDragRegion.MinWidth = sender.SystemOverlayLeftInset;
                ShellTitleBarInset.MinWidth = sender.SystemOverlayRightInset;
            }

            // 确保自定义区域的高度与标题栏相同。
            CustomDragRegion.Height = ShellTitleBarInset.Height = sender.Height;
        }

        public void AddTabToTabs(TabViewItem tab)
        {
            Tabs.TabItems.Add(tab);
        }

        // 将选项卡拖出外面后，创建一个新窗口。
        private void Tabs_TabDroppedOutside(TabView sender, TabViewTabDroppedOutsideEventArgs args)
        {
            var newPage = new TabViewWindowingSamplePage();

            Tabs.TabItems.Remove(args.Tab);
            newPage.AddTabToTabs(args.Tab);

            var newWindow = WindowHelper.CreateWindow();
            newWindow.ExtendsContentIntoTitleBar = true;
            newWindow.Content = newPage;

            newWindow.Activate();
        }

        private void Tabs_TabDragStarting(TabView sender, TabViewTabDragStartingEventArgs args)
        {
            // 我们一次只能拖动一个选项卡，所以抓住第一个选项卡...
            var firstItem = args.Tab;

            // ... 将阻力数据设置为选项卡...
            args.Data.Properties.Add(DataIdentifier, firstItem);

            // ... 并表明我们可以移动它
            args.Data.RequestedOperation = DataPackageOperation.Move;
        }

        private void Tabs_TabStripDrop(object sender, DragEventArgs e)
        {
            // 当我们在不同的选项卡视图之间拖动时，
            // 该事件是调用的，
            // 它负责将项目滴入第二个选项卡视图中

            if (e.DataView.Properties.TryGetValue(DataIdentifier, out object obj))
            {
                // 在继续之前，请确保设置OBJ属性。
                if (obj == null)
                {
                    return;
                }

                var destinationTabView = sender as TabView;
                var destinationItems = destinationTabView.TabItems;

                if (destinationItems != null)
                {
                    // 首先，我们需要将列表中的位置置于
                    var index = -1;

                    // 确定列表中的哪个项目之间的指针。
                    for (int i = 0; i < destinationTabView.TabItems.Count; i++)
                    {
                        var item = destinationTabView.ContainerFromIndex(i) as TabViewItem;

                        if (e.GetPosition(item).X - item.ActualWidth < 0)
                        {
                            index = i;
                            break;
                        }
                    }

                    // TAB视图一次只能在一棵树中。 将其移至新的标签视图之前，请将其从旧视图中删除。
                    var destinationTabViewListView = ((obj as TabViewItem).Parent as TabViewListView);
                    destinationTabViewListView.Items.Remove(obj);

                    if (index < 0)
                    {
                        // 我们没有找到过渡点，所以我们处在列表的尽头
                        destinationItems.Add(obj);
                    }
                    else if (index < destinationTabView.TabItems.Count)
                    {
                        // 否则，以提供的索引插入。
                        destinationItems.Insert(index, obj);
                    }

                    // 选择新拖动的选项卡
                    destinationTabView.SelectedItem = obj;
                }
            }
        }

        // 此方法可防止TabView处理不文本的内容（即文件，图像等）
        private void Tabs_TabStripDragOver(object sender, DragEventArgs e)
        {
            if (e.DataView.Properties.ContainsKey(DataIdentifier))
            {
                e.AcceptedOperation = DataPackageOperation.Move;
            }
        }

        private void Tabs_AddTabButtonClick(TabView sender, object args)
        {
            sender.TabItems.Add(new TabViewItem() { IconSource = new Microsoft.UI.Xaml.Controls.SymbolIconSource() { Symbol = Symbol.Placeholder }, Header = "New Item", Content = new MyTabContentControl() { DataContext = "New Item" } });
        }

        private void Tabs_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
        {
            sender.TabItems.Remove(args.Tab);
        }
    }
}
