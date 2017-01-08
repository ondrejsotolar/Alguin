using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TestStack.White.UIItems;

namespace Alguin.FormStructures
{
    /// <summary>
    /// Base class for panels
    /// </summary>
    public abstract class GenericPanel : GenericForm
    {
        private UIItemContainer panel;
        public UIItemContainer Panel
        {
            get { return this.panel; }
            set
            {
                this.panel = value;
                this.Visual.Window = value;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="parent">parent window</param>
        /// <param name="panel">panel</param>
        public GenericPanel(UIItemContainer parent, UIItemContainer panel) : base(parent)
        {
            this.panel = panel;
        }
    }
}
