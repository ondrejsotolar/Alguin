using System.Drawing;

namespace Alguin.TestStructures
{
    /// <summary>
    /// Test step result data transfer class
    /// </summary>
    public class StepResult : IStepResult
    {
        public EvalType SimpleResult { get; set; }
        public string ExceptionStack { get; set; }
        public string ErrorMessage { get; set; }
        public Bitmap Screenshot { get; set; }
    }
}
