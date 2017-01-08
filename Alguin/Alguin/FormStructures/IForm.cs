using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TestStack.White.UIItems;

namespace Alguin.FormStructures
{
    /// <summary>
    /// Base interface for forms
    /// </summary>
    public interface IForm
    {
        UIItemContainer MainWindow { get; set; }
    }
}
