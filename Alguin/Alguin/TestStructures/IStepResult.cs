using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alguin.TestStructures
{
    public interface IStepResult
    {
        EvalType SimpleResult { get; set; }
        string ErrorMessage { get; set; }
    }
}
