using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections.Generic;
using System.Device.Location;
using System.IO.IsolatedStorage;
using System.Runtime.Serialization.Json;
using System.IO;

namespace MobileMuni
{
    public static class StopStore
    {
        private const string STOP_STORE_KEY = "StopStore";

        private static int NEARBY_STOPS_MAX_DISTANCE = 500; // this is in meters!

        public static Dictionary<string, Stop> Stops = new Dictionary<string, Stop>();

        public static List<Stop> StopsWithTags(List<string> tags)
        {
            if (tags == null)
            {
                return null;
            }

            List<Stop> stopsList = new List<Stop>();
            foreach (string tag in tags)
            {
                stopsList.Add(Stops[tag]);
            }
            return stopsList;
        }

        public static List<Stop> StopsNearLocation(GeoCoordinate location)
        {
            if (location == null)
            {
                return null;
            }

            List<Stop> nearbyStops = new List<Stop>();

            foreach (Stop stop in Stops.Values)
            {
                double dist = stop.DistanceFromLocation(location);
                if (dist < NEARBY_STOPS_MAX_DISTANCE)
                {
                    nearbyStops.Add(stop);
                }
            }

            return nearbyStops;
        }

        public static void Save()
        {
            Stop[] stopArray = new Stop[Stops.Count];
            int i = 0;
            foreach (Stop stop in Stops.Values)
            {
                stopArray[i] = stop;
                i++;
            }

            using (var store = IsolatedStorageFile.GetUserStoreForApplication())
            using (var file = store.CreateFile("StopStore.json"))
            {
                // create the serializer for the class
                var serializer = new DataContractJsonSerializer(typeof(Stop[]));

                // save the object as json
                serializer.WriteObject(file, stopArray);
            }
        }

        public static bool Restore()
        {
            using (var store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (store.FileExists("StopStore.json"))
                {
                    using (var file = store.OpenFile("StopStore.json", FileMode.Open))
                    {
                        // create the serializer
                        var serializer = new DataContractJsonSerializer(typeof(Stop[]));

                        // load the object from JSON
                        Stop[] stopArray = (Stop[])serializer.ReadObject(file);
                        Stops = new Dictionary<string, Stop>();

                        foreach (Stop stop in stopArray)
                        {
                            Stops[stop.Tag] = stop;
                        }
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
