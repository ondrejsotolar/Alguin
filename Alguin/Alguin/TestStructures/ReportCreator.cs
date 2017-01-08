using System;
using System.IO;
using Alguin.TestStructures;

namespace Alguin.Utilities
{
    /// <summary>
    /// Class for generating HTML reports
    /// </summary>
    public class ReportCreator
    {
        #region Attributes
        
        private string filenameBase = "{0}_report.txt";
        private string messageBase = "Step: {0} Time: {1:d/M/yyyy HH:mm:ss} Name: {2} Status: {3}\n";
        private string passedTemplate = "<details class=\"passed\"><summary>Step {0}: {1} - Passed</summary><p>Time: {2:d/M/yyyy HH:mm:ss}</p> {3}{4}</details>";
        private string failedTemplate = "<details class=\"failed\"><summary>Step {0}: {1} - Failed</summary><p>Time: {2:d/M/yyyy HH:mm:ss}</p> <pre>{3}</pre><p>{4}</p><p>{5}</p></details>";
        private string skippedTemplate = "<details class=\"skipped\"><summary>Step {0}: {1} - Skipped</summary><p>Time: {2:d/M/yyyy HH:mm:ss}</p> {3}{4}</details>";
        private string headerTemplate = "<b>Scenario name: </b>{0}<br /><b>Time: </b>{1:d/M/yyyy HH:mm:ss}<br /><b>Duration: </b>{2}s<br /><b>Status: </b>{3}<br /><br />";
        private string imgTemplate = "<a href='{0}'><img src='{1}' height='100'></a>";
        private string fullMessage = "";
        private string templatePath = @"ProjectLibraries\reportTemplate.html";
        private string outPath = "report.html";
        private string folderTemplate = @"{0}_{1:yyyy-M-d_HH-mm-ss}";
        private string outNameTemplate = @"{0}_{1:yyyy-M-d_HH-mm-ss}.html";
        private string screenshotTemplate = @"{0}_{1:yyyy-M-d_HH-mm-ss}.png";
        private string delimiter = @"$|$";
        private bool failed = false;
        private DateTime firstStepExec;
        private DateTime lastStepExec;
        private DirectoryInfo outputFolder;
        private string scenarioName;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="scenarioName">scenario name</param>
        public ReportCreator(string scenarioName)
	    {
            this.firstStepExec = DateTime.Now;
            this.scenarioName = scenarioName;
            this.outputFolder = GetOutputFolder();
	    }

        #endregion

        #region Public methods
        
        /// <summary>
        /// Append step result to the report
        /// </summary>
        /// <param name="stepNumber">scenario step number</param>
        /// <param name="step">step result</param>
        public void Append(int stepNumber, Func<ITestData, IStepResult> step, 
            StepResult stepResult)
        {
            string template;
            string screenPath = null;
            switch (stepResult.SimpleResult)
            {
                case EvalType.Passed:
                    template = passedTemplate;
                    break;
                case EvalType.Skipped:
                    template = skippedTemplate;
                    break;
                case EvalType.Failed:
                default:
                    template = failedTemplate;
                    if (stepResult.Screenshot != null)
                    {
                        screenPath = GetOutputPath(true);
                        stepResult.Screenshot.Save(screenPath);
                    }
                    this.failed = true;
                    break;
            }
            this.lastStepExec = DateTime.Now;
            this.fullMessage += string.Format(template, ++stepNumber, step.Method.Name, DateTime.Now, 
                stepResult.ExceptionStack, stepResult.ErrorMessage, 
                screenPath != null ? string.Format(this.imgTemplate, screenPath, screenPath) : null);
        }

        /// <summary>
        /// Create report. Default name is Roaming folder.
        /// </summary>
        public void CreateReport()
        {
            var text = File.ReadAllText(this.templatePath);
            var duration = this.lastStepExec.Subtract(this.firstStepExec);
            var headr = string.Format(this.headerTemplate, scenarioName, firstStepExec, duration.Seconds,
                this.failed ? EvalType.Failed : EvalType.Passed);
            
            text = text.Replace(this.delimiter, headr + this.fullMessage);
            File.WriteAllText(GetOutputPath(), text);
        }

        #endregion

        #region Other Methods

        private string GetOutputPath(bool screenshot = false)
        {
            var folder = this.outputFolder;
            var path = Path.Combine(
                folder.FullName, 
                string.Format(
                    screenshot ? this.screenshotTemplate : this.outNameTemplate, 
                    this.scenarioName, DateTime.Now));
            return path;
        }

        private DirectoryInfo GetOutputFolder()
        {
            var folder = System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var name = string.Format(folderTemplate, this.scenarioName, DateTime.Now);
            var target = Directory.CreateDirectory(Path.Combine(folder, name));
            return target;
        }

        #endregion
    }
}
