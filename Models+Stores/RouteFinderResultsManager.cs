
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
namespace MobileMuni
{
    public static class RouteFinderResultsManager
    {
        
        public static double? StartLatitude { get; set; }
        public static double? StartLongitude { get; set; }
        public static string StartName { get; set; }
        public static double? EndLatitude { get; set; }
        public static double? EndLongitude { get; set; }
        public static string EndName { get; set; }

        public static List<RouteFinderResult> Results { get; set; }

        public delegate void CaluclateResultsFinishedDelegate();
        public static CaluclateResultsFinishedDelegate CalculateResultsFinished;
        public delegate void ResultsUpdatedDelegate();
        public static ResultsUpdatedDelegate ResultsUpdated;

        static RouteFinderResultsManager()
        {
            PredictionFetcher.PredictionsUpdated += PredictionFetcher_PredictionsUpdated;
        }

        public static void ResetAllValues()
        {
            StartLatitude = null;
            StartLongitude = null;
            StartName = null;
            EndLatitude = null;
            EndLongitude = null;
            EndLongitude = null;
            Results = new List<RouteFinderResult>();
        }

        public static void CalculateResuts()
        {
            var worker = new BackgroundWorker();
            worker.DoWork += new DoWorkEventHandler(worker_DoWork);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
            worker.RunWorkerAsync();
        }

        static void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Deployment.Current.Dispatcher.BeginInvoke(new Action(CalculateResultsFinished));

            // now try to fetch predictions for all the results            
            PredictionFetcher.AdditionalPredictionsToFetch.Clear();
            foreach (var result in Results)
            {
                PredictionFetcher.AdditionalPredictionsToFetch.Add(new Prediction()
                {
                    RouteTag = result.Route.Tag,
                    StopTag = result.StartStop.Tag,
                    Direction = result.Direction
                });                
            }
            PredictionFetcher.StartFecthing();
        }

        private static void PredictionFetcher_PredictionsUpdated()
        {
            if (ResultsUpdated != null)
            {
                bool scoresHaveChanged = false;
                
                // recalculate the result scores, re-sort
                foreach (var result in Results)
                {
                    scoresHaveChanged = result.CalculateScores() ? true : scoresHaveChanged; // true if any scores have changed
                }
                Results.Sort();

                if (scoresHaveChanged)
                {
                    ResultsUpdated();
                }
            }
        }

        static void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            // calculate the nearest stops
	        // for each route caluclate the nearest stop for each location

            Route.Direction direction = Route.Direction.Inbound;

            foreach (Route route in RouteStore.Routes) 
            {
                var stopTags = direction == Route.Direction.Inbound ? route.InboundStopTags : route.OutboundStopTags;
                var stops = StopStore.StopsWithTags(stopTags);

                if (stops.Count == 0)
                {
                    continue; // ignore empty routes
                }

                Stop nearestStartStop = stops[0];
                double nearestStartStopDist = Utility.DistanceBetweenCoordinates(nearestStartStop.Latitude, nearestStartStop.Longitude, (double)StartLatitude, (double)StartLongitude);

                Stop nearestEndStop = nearestStartStop;
                double nearestEndStopDist = Utility.DistanceBetweenCoordinates(nearestEndStop.Latitude, nearestEndStop.Longitude, (double)EndLatitude, (double)EndLongitude);

                foreach (Stop stop in stops)
                {
                    double startStopDist = Utility.DistanceBetweenCoordinates(stop.Latitude, stop.Longitude, (double)StartLatitude, (double)StartLongitude);
                    if (startStopDist < nearestStartStopDist) 
                    {
                        nearestStartStop = stop;
                        nearestStartStopDist = startStopDist;
                    }

                    double endStopDist = Utility.DistanceBetweenCoordinates(stop.Latitude, stop.Longitude, (double)EndLatitude, (double)EndLongitude);
                     if (endStopDist < nearestEndStopDist) 
                    {
                        nearestEndStop = stop;
                        nearestEndStopDist = endStopDist;
                    }
                }

                // if endStop before startStop then we need to flip directions and redo
                bool endSeenFirst = false;
                foreach (Stop stop in stops)
                {
                    if (stop == nearestStartStop) {
                        break;
                    }
                    else if (stop == nearestEndStop)
                    {
                        endSeenFirst = true;
                        break;
                    }
                }

                if (endSeenFirst || nearestStartStop == null) 
                {
                    // flip the direction and recalculate everything
                    direction = direction == Route.Direction.Inbound ? Route.Direction.Outbound : Route.Direction.Inbound;
                    stopTags = direction == Route.Direction.Inbound ? route.InboundStopTags : route.OutboundStopTags;
                    stops = StopStore.StopsWithTags(stopTags);

                    if (stops.Count == 0)
                    {
                        continue; // ignore empty routes
                    }

                    nearestStartStop = stops[0];
                    nearestStartStopDist = Utility.DistanceBetweenCoordinates(nearestStartStop.Latitude, nearestStartStop.Longitude, (double)StartLatitude, (double)StartLongitude);

                    nearestEndStop = nearestStartStop;
                    nearestEndStopDist = Utility.DistanceBetweenCoordinates(nearestEndStop.Latitude, nearestEndStop.Longitude, (double)EndLatitude, (double)EndLongitude);

                    foreach (Stop stop in stops)
                    {
                        double startStopDist = Utility.DistanceBetweenCoordinates(stop.Latitude, stop.Longitude, (double)StartLatitude, (double)StartLongitude);
                        if (startStopDist < nearestStartStopDist) 
                        {
                            nearestStartStop = stop;
                            nearestStartStopDist = startStopDist;
                        }

                        double endStopDist = Utility.DistanceBetweenCoordinates(stop.Latitude, stop.Longitude, (double)EndLatitude, (double)EndLongitude);
                         if (endStopDist < nearestEndStopDist) 
                        {
                            nearestEndStop = stop;
                            nearestEndStopDist = endStopDist;
                        }
                    }
                }

                if (nearestStartStop != null && nearestEndStop != null)
                {
                    // calculate the total distance and save a result
                    var result = new RouteFinderResult();
                    result.Route = route;
                    result.StartStop = nearestStartStop;
                    result.EndStop = nearestEndStop;
                    result.StartDistance = nearestStartStopDist;
                    result.EndDistance = nearestEndStopDist;
                    result.Direction = direction;
                    result.CalculateScores();
                    Results.Add(result);
                }
            }

            // now sort every thing based on initial score
            Results.Sort();

            // take only the top 10 results, we don't need more than that
            if (Results.Count > 10)
            {
                Results.RemoveRange(10, Results.Count - 10);
            }
        }
    }
}