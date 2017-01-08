using System;
using TestStack.White;
using TestStack.White.UIItems.WindowItems;
using TestStack.White.Factory;
using Alguin.Utilities;

namespace Alguin.AppWrapper
{
    /// <summary>
    /// Class manages the tested applications
    /// </summary>
    public class ApplicationWrapper : IDisposable
    {
        private readonly Application host;

        /// <summary>
        /// Launch application from name
        /// </summary>
        /// <param name="name">name</param>
        public ApplicationWrapper(string path)
        {
            this.host = Application.Launch(path);
            WrapperPool.Wrappers.Add(this);
        }

        /// <summary>
        /// Get Window by title
        /// </summary>
        /// <param name="title">Case sensitive</param>
        /// <returns>Window</returns>
        public Window GetWindow(string title, 
            int timeout = InteractionTimeout.WindowSeek, 
            int waitAfter = InteractionTimeout.Interaction)
        {
            Window window = null;
            InteractionTimeout.ExecuteWithTimeLimit(TimeSpan.FromMilliseconds(timeout), () => {
                    window = host.GetWindow(title, InitializeOption.NoCache);
            });
            if (window != null)
                InteractionTimeout.Wait(waitAfter);
            
            return window;
        }

        /// <summary>
        /// Kills the attached application
        /// </summary>
        public void Dispose()
        {
            if (this.host != null)
                this.host.Kill();
        }
    }
}
