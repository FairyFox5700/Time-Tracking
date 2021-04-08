using System;

namespace TimeTracking.Identity.BL.Impl.Helpers
{
    public static class DateTimeHelpers
    {
        /// <summary>
        ///  Coverts <param name="date"/> covered to seconds since Unix epoch (Jan 1, 1970, midnight UTC)  
        /// </summary>
        /// <param name="date"></param>
        /// <returns>Converted data in seconds since Unix epoch</returns>
        public static long ToUnixEpochDate(DateTime date)
            => (long)Math.Round((date.ToUniversalTime() -
                                 new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero))
                .TotalSeconds);
    }
}