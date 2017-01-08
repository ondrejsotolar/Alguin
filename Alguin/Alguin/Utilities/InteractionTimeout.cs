using System;
using System.Threading;
using System.Threading.Tasks;
using TestStack.White.Configuration;

namespace Alguin.Utilities
{
    /// <summary>
    /// Class for timeout handling and time constants
    /// </summary>
    public static class InteractionTimeout
    {
        public const int Interaction = 250;
        public const int WindowSeek = 5000;
        
        /// <summary>
        /// Getter and Setter of default busy timeout
        /// </summary>
        public static int DefaultTimeout
        {
            get { return CoreAppXmlConfiguration.Instance.BusyTimeout; }
            set { CoreAppXmlConfiguration.Instance.BusyTimeout = value; }
        }

        /// <summary>
        /// Execute block of code in another thread with time limit
        /// </summary>
        /// <param name="timeSpan">time span</param>
        /// <param name="codeBlock">code block</param>
        /// <returns>true if all tasks were completed</returns>
        public static bool ExecuteWithTimeLimit(TimeSpan timeSpan, Action codeBlock)
        {
            try
            {
                var task = Task.Factory.StartNew(() => codeBlock());
                task.Wait(timeSpan);
                return task.IsCompleted;
            }
            catch (AggregateException ae)
            {
                throw ae.InnerExceptions[0];
            }
        }

        /// <summary>
        /// Wait on current thread
        /// </summary>
        /// <param name="miliseconds">time to wait in miliseconds</param>
        public static void Wait(int miliseconds = Interaction) {
            Thread.Sleep(miliseconds);
        }
    }
}
