using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.Storage;

namespace AppUIBasics.Helper
{
    public static class UIHelper
    {
        public static bool IsScreenshotMode { get; set; }
#if !UNPACKAGED
        public static StorageFolder ScreenshotStorageFolder { get; set; } = ApplicationData.Current.LocalFolder;
#else
        public static StorageFolder ScreenshotStorageFolder { get; set; } = Task.Run(async () => await StorageFolder.GetFolderFromPathAsync(System.AppContext.BaseDirectory)).Result;
#endif

        public static IEnumerable<T> GetDescendantsOfType<T>(this DependencyObject start) where T : DependencyObject
        {
            return start.GetDescendants().OfType<T>();
        }

        public static IEnumerable<DependencyObject> GetDescendants(this DependencyObject start)
        {
            var queue = new Queue<DependencyObject>();
            var count1 = VisualTreeHelper.GetChildrenCount(start);

            for (int i = 0; i < count1; i++)
            {
                var child = VisualTreeHelper.GetChild(start, i);
                yield return child;
                queue.Enqueue(child);
            }

            while (queue.Count > 0)
            {
                var parent = queue.Dequeue();
                var count2 = VisualTreeHelper.GetChildrenCount(parent);

                for (int i = 0; i < count2; i++)
                {
                    var child = VisualTreeHelper.GetChild(parent, i);
                    yield return child;
                    queue.Enqueue(child);
                }
            }
        }
    }
}
