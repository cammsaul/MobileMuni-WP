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

namespace MobileMuni
{
    public class Favorite
    {

        public string RouteTag { get; set; }
        public string StopTag { get; set; }
        public Route.Direction Direction { get; set; }

        public string Predictions { get { return PredictionFetcher.GetPrediction(RouteTag, StopTag, Direction); } }

        public Route Route
        {
            get
            {
                return RouteStore.RouteForTag(RouteTag);
            }
        }

        public Stop Stop
        {
            get
            {
                return StopStore.Stops[StopTag];
            }
        }

        public string DirectionString
        {
            get
            {
                return Direction == Route.Direction.Inbound ? "inbound" : "outbound";
            }
        }

        public Favorite() { }
    }
}
