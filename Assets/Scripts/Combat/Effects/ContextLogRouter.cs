using System;

namespace ReE.Combat.Effects
{
    // Packet_004.8: Context Log Layer Router (Log-Only)
    public static class ContextLogRouter
    {
        /// <summary>
        /// Appends a tag to the original log string if it's not already present.
        /// Ensures no duplication of tags like [P045 ...].
        /// </summary>
        public static string AppendTagOnce(string originalLog, string tag)
        {
            if (string.IsNullOrEmpty(tag)) return originalLog;
            if (originalLog == null) originalLog = "";

            // Simple containment check for MVP/Skeleton
            // In future, better parsing might be needed, but for Log-Only skeleton this is sufficient.
            if (originalLog.Contains(tag))
            {
                return originalLog;
            }

            return originalLog + tag;
        }
    }
}
