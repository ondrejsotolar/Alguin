namespace Alguin.TestStructures
{
    /// <summary>
    /// Test step result interface
    /// </summary>
    public interface IStepResult
    {
        EvalType SimpleResult { get; set; }
        string ErrorMessage { get; set; }
    }
}
