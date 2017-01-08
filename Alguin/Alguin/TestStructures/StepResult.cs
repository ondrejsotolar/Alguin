using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace Alguin.TestStructures
{
    public class StepResult : IStepResult
    {
        public EvalType SimpleResult { get; set; }
        public string ExceptionStack { get; set; }
        public string ErrorMessage { get; set; }
        public Bitmap Screenshot { get; set; }
    }
}
