using System.Collections.Generic;
using System.Linq;

namespace Alguin.AppWrapper
{
    /// <summary>
    /// Class manages instances of ApplicationWrapper
    /// </summary>
    public static class WrapperPool
    {
        private static List<ApplicationWrapper> wrappers = new List<ApplicationWrapper>();
        public static List<ApplicationWrapper> Wrappers
        {
            get { return wrappers; }
        }

        /// <summary>
        /// Close all wrappers in pool and clear the list of wrappers
        /// </summary>
        public static void ClearWrapperPool()
        {
            Wrappers.ForEach(x => x.Dispose());
            Wrappers.Clear();
        }

        /// <summary>
        /// Get the first wrapper in pool
        /// </summary>
        /// <returns>wrapper</returns>
        public static ApplicationWrapper GetFirstWrapper()
        {
            if (!Wrappers.Any())
                return null;

            return Wrappers[0];
        }

        /// <summary>
        /// Get the last wrapper in pool
        /// </summary>
        /// <returns></returns>
        public static ApplicationWrapper GetLastWrapper()
        {
            if (!Wrappers.Any())
                return null;

            return Wrappers[Wrappers.Count() - 1];
        }
    }
}
