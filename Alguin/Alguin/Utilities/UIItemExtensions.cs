using TestStack.White.InputDevices;
using TestStack.White.UIItems;
using TestStack.White.UIItems.ListBoxItems;
using TestStack.White.UIItems.MenuItems;
using System.Threading;

namespace Alguin.Utilities
{
    /// <summary>
    /// Class for UIItem extensions
    /// </summary>
    public static class UIItemExtensions
    {
        /// <summary>
        /// Select text from list and wait
        /// </summary>
        /// <param name="text">text</param>
        /// <param name="timeoutMilis">wait</param>
        public static void SelectAndWait(this ListControl list, string text, 
            int timeoutMilis = InteractionTimeout.Interaction)
        {
            list.Select(text);
            System.Threading.Thread.Sleep(timeoutMilis);
        }

        /// <summary>
        /// Set text value and wait
        /// </summary>
        /// <param name="text">text</param>
        /// <param name="timeoutMilis">wait</param>
        public static void SetValueTabFocusWait(this UIItem item, string text, 
            int timeoutMilis = InteractionTimeout.Interaction)
        {
            item.SetValue(text);
            Keyboard.Instance.PressSpecialKey(TestStack.White.WindowsAPI.KeyboardInput.SpecialKeys.TAB);
            item.Focus();
            Thread.Sleep(timeoutMilis);
        }

        /// <summary>
        /// Click and wait
        /// </summary>
        /// <param name="item">item</param>
        /// <param name="timeoutMilis">wait</param>
        public static void ClickAndWait(this UIItem item, int timeoutMilis = InteractionTimeout.Interaction)
        {
            item.Click();
            Thread.Sleep(timeoutMilis);
        }

        /// <summary>
        /// Click on a point in window 
        /// </summary>
        /// <param name="point">point relative to top left corner</param>
        /// <param name="timeoutMilis">wait</param>
        public static void ClickAndWait(this UIItemContainer window, System.Windows.Point point, 
            int timeoutMilis = InteractionTimeout.Interaction)
        {
            window.Mouse.Location = point;
            window.Mouse.Click();
            Thread.Sleep(timeoutMilis);
        }

        /// <summary>
        /// Double click on a point in window 
        /// </summary>
        /// <param name="point">point relative to top left corner</param>
        /// <param name="timeoutMilis">wait</param>
        public static void DoubleClickAndWait(this UIItemContainer window, System.Windows.Point point, 
            int timeoutMilis = InteractionTimeout.Interaction)
        {
            window.Mouse.Location = point;
            window.Mouse.DoubleClick(point);
            System.Threading.Thread.Sleep(timeoutMilis);
        }
    }
}
