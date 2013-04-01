using System;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Diagnostics;
using System.IO.IsolatedStorage;
using System.Runtime.Serialization.Json;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace MobileMuni
{
    public static class Fetcher
    {

        public delegate void FetchingBeganDelegate();
        public static FetchingBeganDelegate FetchingBegan;
        public delegate void FetchingCompleteDelegate();
        public static FetchingCompleteDelegate FetchingComplete;

        public delegate void RestoringBeganDelegate();
        public static RestoringBeganDelegate RestoringBegan;
        public delegate void RestoringCompleteDelegate();
        public static RestoringCompleteDelegate RestoringComplete;

        private const string FETCH_DATE_TIME = "FetchDateTime";
        private const string FETCH_VERSION = "FetchVersion";

        public static void fetch(bool forced)
        {
           bool restoreSuccessful = false;

            if (IsolatedStorageSettings.ApplicationSettings.Contains(FETCH_DATE_TIME) && IsolatedStorageSettings.ApplicationSettings.Contains(FETCH_VERSION))
            {
                DateTime fetchDateTime = (DateTime)IsolatedStorageSettings.ApplicationSettings[FETCH_DATE_TIME];
                string fetchVersion = (string)IsolatedStorageSettings.ApplicationSettings[FETCH_VERSION];

                TimeSpan fetchTimeSpan = DateTime.Now - fetchDateTime;
                Debug.WriteLine("Last fetch was {0} days, {1} hours, {2} minutes ago.", fetchTimeSpan.Days, fetchTimeSpan.Hours, fetchTimeSpan.Minutes);
                Debug.WriteLine("Last fetch was for version {0}. We are version {1}", fetchVersion, Utility.AppVersion);
                if (fetchTimeSpan.Days < 7 && fetchVersion == Utility.AppVersion)
                {
                    // data hasn't "expired" yet, go ahead and restore.
                    restoreSuccessful = true;
                }
            }

            if (restoreSuccessful)
            {
                RestoringBegan();

                var restoreDataWorker = new BackgroundWorker();
                restoreDataWorker.DoWork += new DoWorkEventHandler(restoreDataWorker_DoWork);
                restoreDataWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(restoreDataWorker_RunWorkerCompleted);
                restoreDataWorker.RunWorkerAsync();
            }
            else
            {
                FetchingBegan();

                Debug.WriteLine("restore failed, attempting to fetch...");

                BookmarkStore.Restore();

                WebClient wc = new WebClient();
                wc.DownloadStringAsync(new Uri("https://s3-us-west-1.amazonaws.com/mobilemuniroutes/" + Settings.NextBusCityCode + ".routeConfig.xml", UriKind.Absolute));
                wc.DownloadStringCompleted += new DownloadStringCompletedEventHandler(wc_DownloadStringCompleted);
            }
        }

        static void restoreDataWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Deployment.Current.Dispatcher.BeginInvoke(new Action(RestoringComplete));
        }

        static void restoreDataWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            RouteStore.Restore();
            FavoriteStore.Restore();
            StopStore.Restore();
            BookmarkStore.Restore();
        }


        private static void wc_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            StringReader sr = new StringReader(e.Result);
            var reader = XmlReader.Create(sr);
            var document = XDocument.Load(reader);

            var routes = from route in document.Descendants("route") select route;

            // if we've fetched new routes, clear the old ones
            StopStore.Stops.Clear();
            RouteStore.Routes.Clear();

            foreach (var routeEle in routes)
            {
                Route route = new Route(routeEle);
                RouteStore.Routes.Add(route);

                Debug.WriteLine("Fetched Route {0}", route.Tag);

                // add any new stops to the stop store
                var stops = from stop in routeEle.Descendants("stop") where stop.Parent == routeEle select stop;
                foreach (var stopEle in stops)
                {
                    Stop stop = new Stop(stopEle);
                    StopStore.Stops[stop.Tag] = stop;
                }

                // outbound stops
                var outboundDirections = from direction in routeEle.Descendants("direction") where (string)direction.Attribute("name") == "Outbound" select direction;
                if (outboundDirections != null && outboundDirections.Count() as int? > 0)
                {
                    var outboundDirection = outboundDirections.First();
                    route.OutboundTitle = (string)outboundDirection.Attribute("title");
                    foreach (var stopEle in outboundDirection.Descendants("stop"))
                    {
                        string tag = (string)stopEle.Attribute("tag");
                        route.OutboundStopTags.Add(tag);
                    }
                }

                // inbound stops
                var inboundDirections = from direction in routeEle.Descendants("direction") where (string)direction.Attribute("name") == "Inbound" select direction;
                if (inboundDirections != null && inboundDirections.Count() as int? > 0)
                {
                    var inboundDirection = inboundDirections.First();
                    route.InboundTitle = (string)inboundDirection.Attribute("title");
                    foreach (var stopEle in inboundDirection.Descendants("stop"))
                    {
                        string tag = (string)stopEle.Attribute("tag");
                        route.InboundStopTags.Add(tag);
                    }
                }
            }

            // go ahead and sort routes now!
            RouteStore.Routes.Sort();

            SaveData();
            FetchingComplete();
        }

        private static void SaveData()
        {
            Debug.WriteLine("Saving RouteStore and StopStore");
            RouteStore.Save();
            StopStore.Save();
            BookmarkStore.Save();

            // save the timespan we grabbed this at so we know when to re-fetch
            IsolatedStorageSettings.ApplicationSettings[FETCH_DATE_TIME] = DateTime.Now;
            IsolatedStorageSettings.ApplicationSettings[FETCH_VERSION] = Utility.AppVersion;                        
            IsolatedStorageSettings.ApplicationSettings.Save();
        }
    }
}