using System;
using System.Collections.Generic;
using Alguin.Utilities;
using Alguin.AppWrapper;
using Alguin.VisualMethods;
using System.Drawing;

namespace Alguin.TestStructures
{
    /// <summary>
    /// Scenario result evaluation type
    /// </summary>
    public enum EvalType { Passed, Skipped, Failed };

    /// <summary>
    /// Base class for test scenario
    /// </summary>
    public abstract class GenericScenario : IScenario
    {
        #region Attributes

        private int executed = 0;
        private int count = 0;
        private bool lastFailed = false;
        private bool skipAll = false;
        private bool logicalFail;
        private string failMessage;
        private List<Func<ITestData, IStepResult>> testSteps;
        private List<StepResult> stepResults;
        private ReportCreator reportCreator;
        private BitmapUtils bitmapUtils; 

        protected ApplicationWrapper AppWrapper { get; set; }

        public string ScenarioName { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        public GenericScenario()
        {
            this.testSteps = new List<Func<ITestData, IStepResult>>();
            this.stepResults = new List<StepResult>();
            this.ScenarioName = this.GetType().Name;
            this.reportCreator = new ReportCreator(this.ScenarioName);
            this.bitmapUtils = new BitmapUtils();
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Add a test step
        /// </summary>
        /// <param name="step">step</param>
        public void AddStep(Func<ITestData, IStepResult> step)
        {
            this.testSteps.Add(step);
            this.stepResults.Add(new StepResult());
            this.count++;
        }

        /// <summary>
        /// Run all steps. Clean up and create report after.
        /// </summary>
        public void RunAllSteps()
        {
            this.testSteps.ForEach(x => RunNext());
            CreateReport();
            CleanUp();
        }

        /// <summary>
        /// Run the test step currently in order
        /// </summary>
        /// <param name="data">data to be sent to this particular step</param>
        /// <returns>this particular step result</returns>
        public IStepResult RunNext(ITestData data = null)
        {
            if (this.executed >= this.count)
                throw new Exception("No more steps to execute.");

            IStepResult result = null;
            this.logicalFail = false;
            if (this.lastFailed || this.skipAll)
            {
                AddResult(EvalType.Skipped, executed);
                executed++;
                return result;
            }
            try
            {
                result = this.testSteps[executed](data);
                if (this.logicalFail)
                    AddResult(EvalType.Failed, executed, message:this.failMessage);
                else
                    AddResult(EvalType.Passed, executed);
            }
            catch (Exception e)
            {
                this.lastFailed = true;
                AddResult(EvalType.Failed, executed, exception:e, 
                    screenshot: this.bitmapUtils.TakeScreenshot());
            }
            finally
            {
                executed++;
            }
            return result;
        }

        /// <summary>
        /// Create report
        /// </summary>
        public void CreateReport()
        {
            for (int i = 0; i < this.count; i++)
            {
                this.reportCreator.Append(
                    i, 
                    this.testSteps[i], 
                    this.stepResults[i]
                );
            }
            this.reportCreator.CreateReport();
        }

        /// <summary>
        /// Dispose the AppWrapper thus closing all associated windows
        /// </summary>
        public void CleanUp()
        {
            this.AppWrapper.Dispose();
        }

        /// <summary>
        /// Skip the step currently in order
        /// </summary>
        public void SkipNext() {
            AddResult(EvalType.Skipped, executed);
            executed++;
        }

        /// <summary>
        /// Skip all further steps
        /// </summary>
        public void SkipAll()
        {
            this.skipAll = true;
        }

        /// <summary>
        /// Fail current test step on logical condition
        /// </summary>
        /// <param name="message">reason</param>
        /// <param name="skipAll">skip further steps, default is false</param>
        protected void LogicalFail(string message, bool skipAll = false)
        {
            this.logicalFail = true;
            this.failMessage = message;

            if (skipAll)
                SkipAll();
        }

        #endregion

        #region Other methods

        private void AddResult(EvalType type, int index, string message = null, 
            Exception exception = null, Bitmap screenshot = null)
        {
            this.stepResults[index].SimpleResult = type;
            if (message != null)
                this.stepResults[index].ErrorMessage = message;
            if (exception != null)
                this.stepResults[index].ExceptionStack += exception.ToString();
            if (screenshot != null)
                this.stepResults[index].Screenshot = screenshot;
        }

        #endregion
    }
}
