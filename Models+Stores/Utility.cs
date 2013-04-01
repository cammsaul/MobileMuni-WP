using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MobileMuni
{
    public static class Utility
    {
        public const string AppVersion = "2.0";

        /// <summary>
        /// This is in Miles
        /// </summary>
        public static double DistanceBetweenCoordinates(double startLatitude, double startLongitude, double endLatitude, double endLongitude)
        {
            const int RADIUS = 6371000; // Earth's radius in meters
            const double RAD_PER_DEG = 0.017453293;

            double dlat = endLatitude - startLatitude;
            double dlon = endLongitude - startLongitude;

            double dlon_rad = dlon * RAD_PER_DEG;
            double dlat_rad = dlat * RAD_PER_DEG;
            double lat1_rad = startLatitude * RAD_PER_DEG;
            double lon1_rad = startLongitude * RAD_PER_DEG;
            double lat2_rad = endLatitude * RAD_PER_DEG;
            double lon2_rad = endLongitude * RAD_PER_DEG;

            double a = Math.Pow(Math.Sin(dlat_rad / 2), 2) + Math.Cos(lat1_rad) * Math.Cos(lat2_rad) * Math.Pow(Math.Sin(dlon_rad / 2), 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            double d = RADIUS * c;

            return d * 0.000621371192; // convert to miles
        }

        public static int MilesToMinutesWalk(double miles)
        {
            const double AVERAGE_WALKING_SPEED = 2.7; // MPH
            double hours = miles / AVERAGE_WALKING_SPEED;
            return (int)Math.Round(hours * 60);
        }
    }
}
