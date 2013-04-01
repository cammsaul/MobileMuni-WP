using System;
using System.Collections;
using System.Net;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Threading;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Windows.Threading;

namespace MobileMuni
{
    public class Prediction
    {
        public string RouteTag { get; set; }
        public string StopTag { get; set; }
        public Route.Direction Direction { get; set; }
        public string Text { get; set; }
    }

    /// <summary>
    /// An AUTO PredictionFetcher... will update predictions based on current stop and whatever's in Favorites every minute or so, other people can just query for predictions
    /// </summary>
    public class PredictionFetcher
    {
        public delegate void PredictionsUpdatedDelegate();
        public static PredictionsUpdatedDelegate PredictionsUpdated;

        public static StateMachine.State FetchedState { get; private set; }

        private static Timer Timer;
        public static List<Prediction> AdditionalPredictionsToFetch = new List<Prediction>(); // this is a list of additional predictions to be fetched
        private static List<Prediction> Predictions; // this is the list of actual completed predictions

        public const string FETCHING_PREDICTIONS_STRING = "fetching predictions...";
        public const string BUS_NOT_RUNNING_AT_THIS_TIME_STRING = "bus not running at this time";        

        public static void StartFecthing()
        {
            Timer = new Timer(callback: Update, state:null, dueTime: 0, period: 30000); // start immediately and call back every 30 seconds
            Predictions = new List<Prediction>();
            StateMachine.StateChanged += new StateMachine.StateChangedDelegate(StateMachine_StateChanged);
        }

        public static void StateMachine_StateChanged(StateMachine.State newState)
        {
            if (StateMachine.RouteChanged() || StateMachine.StopsChanged() || StateMachine.DirectionChanged())
            {
                if (newState.Route != null && newState.Stop != null && newState.Direction != Route.Direction.None)
                {
                    // update right away
                    Update(null);
                }
            }
        }

        private static void Update(object state)
        {
            // gather all of the stuff we want to fetch
            List<Favorite> targets = new List<Favorite>(FavoriteStore.Favorites);
            StateMachine.State newState = StateMachine.CurrentState;

            if (newState.Route != null && newState.Stop != null && newState.Direction != Route.Direction.None)
            {
                Favorite currentTarget = new Favorite()
                {
                    RouteTag = newState.Route.Tag,
                    StopTag = newState.Stop.Tag,
                    Direction = newState.Direction
                };
                targets.Add(currentTarget);
            }

            // add all the predictions from the PredictionsToFetch list
            foreach (var prediction in AdditionalPredictionsToFetch)
            {
                var target = new Favorite()
                {
                    RouteTag = prediction.RouteTag,
                    StopTag = prediction.StopTag,
                    Direction = prediction.Direction
                };
                targets.Add(target);
            }

            if (targets.Count == 0)
            {
                return; // nothing to do
            }

            string paramStr = "";
            foreach (Favorite target in targets)
            {
                string targetStr = String.Format("&stops={0}|{1}", target.RouteTag, target.StopTag);
                paramStr += targetStr;
            }

            // fetch from here http://webservices.nextbus.com/service/publicXMLFeed?command=predictionsForMultiStops&a=sf-muni&stops=N|6997&stops=N|3909
            string uriStr = "http://webservices.nextbus.com/service/publicXMLFeed?command=predictionsForMultiStops&a=" + Settings.NextBusCityCode + paramStr + "&junk=" + DateTime.Now.Ticks; // junk param is used to prevent result caching

            Debug.WriteLine("Fetching predictions with URI: {0}", uriStr);

            WebClient wc = new WebClient();
            wc.DownloadStringAsync(new Uri(uriStr, UriKind.Absolute));
            wc.DownloadStringCompleted += new DownloadStringCompletedEventHandler(wc_DownloadStringCompleted);
        }

        private static void wc_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            Predictions.Clear();


            StringReader sr;
            try
            {
                sr = new StringReader(e.Result);    
            }
            catch
            {
                // just didn't get any predictions, nothing to do
                return;
            }

            var reader = XmlReader.Create(sr);
            var document = XDocument.Load(reader);
            var routes = from route in document.Descendants("predictions") select route;

            foreach (var route in routes)
            {
                string routeTag = (string)route.Attribute("routeTag");
                string stopTag = (string)route.Attribute("stopTag");
                
                string predictionsStr = "Bus in";
                
                var predictions = from prediction in route.Descendants("prediction") select prediction;
                foreach (var prediction in predictions)
                {
                    int minutes = (int)prediction.Attribute("minutes");
                    predictionsStr += String.Format(" {0}", minutes);
                    if (prediction != predictions.Last()) {
                        predictionsStr += ",";
                    }
                }
                predictionsStr += " minutes";

                if (predictions.Count() == 0)
                {
                    predictionsStr = BUS_NOT_RUNNING_AT_THIS_TIME_STRING;
                }

                Debug.WriteLine("Fetched predictions for route {0} stop #{1}: {2}", routeTag, stopTag, predictionsStr);

                Predictions.Add(new Prediction()
                {
                    RouteTag = routeTag,
                    StopTag = stopTag,
                    Text = predictionsStr
                });
            }

            if (PredictionsUpdated != null)
            {
                Deployment.Current.Dispatcher.BeginInvoke(new Action(PredictionsUpdated));
            }

            e = null;
        }

        /// <summary>
        /// Returns the prediction string for given routeTag/stopTag/direction.
        /// Actually for the time being we're ignoring direction tag (?)
        /// </summary>
        /// <param name="routeTag"></param>
        /// <param name="stopTag"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        public static string GetPrediction(string routeTag, string stopTag, Route.Direction direction)
        {
            if (routeTag == null || direction == Route.Direction.None || stopTag == null) {
                return null; // precondition
            }

            foreach (Prediction p in Predictions)
            {
                if (p.RouteTag.Equals(routeTag) && p.StopTag == stopTag)
                {
                    return p.Text;
                }
            }
            return FETCHING_PREDICTIONS_STRING;
        }
    }
}
