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
