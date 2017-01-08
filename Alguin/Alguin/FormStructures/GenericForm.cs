using System.Collections.Generic;
using Alguin.VisualMethods;
using TestStack.White.UIItems;

namespace Alguin.FormStructures
{
    /// <summary>
    /// Base class for all forms
    /// </summary>
    public abstract class GenericForm : IForm
    {
        protected Visual Visual { get; private set; }
        protected Dictionary<System.Drawing.Point, string> Labels { get; set; }
        
        private UIItemContainer mainWindow;
        public UIItemContainer MainWindow {
            get { return this.mainWindow; }
            set 
            {
                this.mainWindow = value;
                this.Visual.Window = value;
            }
        }

        /// <summary>
        /// Create new BaseForm from a window
        /// </summary>
        /// <param name="window">window</param>
        public GenericForm(UIItemContainer window)
        {
            this.Visual = new Visual();
            this.MainWindow = window;
        }
    }
}
