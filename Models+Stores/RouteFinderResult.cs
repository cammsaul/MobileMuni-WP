using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MobileMuni
{
    public class RouteFinderResult : IComparable
    {
        public RouteFinderResult()
            {
                RideLengthScore = -1;
                WalkingTimeScore = -1;
                NextBusArrivalScore = -1;
            }

            public Route Route { get; set; }
            public Stop StartStop { get; set; }
            public Stop EndStop { get; set; }
            public Route.Direction Direction { get; set; }
            public double StartDistance { get; set; } // in miles
            public double EndDistance { get; set; } // in miles
            public double TotalDistance // in miles
            {
                get
                {
                    return StartDistance + EndDistance;
                }
            }

            public int RideLengthScore { get; set; }
            public int WalkingTimeScore { get; set; }
            public int NextBusArrivalScore { get; set; }

            public int TotalScore
            {
                get
                {
                    return RideLengthScore + WalkingTimeScore + NextBusArrivalScore;
                }
            }

            public string DirectionString
            {
                get
                {
                    return Direction == Route.Direction.Inbound ? Route.InboundTitle : Route.OutboundTitle;
                }
            }

            public string StartString
            {
                get
                {
                    return "Get on " + Route.Tag + " at " + StartStop.Title + " (" + Utility.MilesToMinutesWalk(StartDistance) + " min walk from " + RouteFinderResultsManager.StartName + ")";
                }
            }

            public string EndString
            {
                get
                {
                    return "Get off " + Route.Tag + " at " + EndStop.Title + " (" + Utility.MilesToMinutesWalk(EndDistance) + " min walk to " + RouteFinderResultsManager.EndName + ")";
                }
            }

            public string PredictionsString
            {
                get
                {
                    return PredictionFetcher.GetPrediction(Route.Tag, StartStop.Tag, Direction);
                }
            }

            /// <summary>
            /// calculates the walking/ride length/next bus scores. Since Walking and Ride Length cannot change they are only calculated once and then
            /// cached. NB is calculated every time the method is called. WT is out of 40 points; RL and NB are out of 20 points.
            /// </summary>
            /// <returns>True if the score has changed since last time the method was called; false otherwise.</returns>
            public bool CalculateScores()
            {
                int oldTotalScore = TotalScore;

                // calcuate ride length score
                if (RideLengthScore == -1)
                {
                    double rideLength = StartStop.CumulativeDistanceToStop(EndStop, Route, Direction);
                    RideLengthScore = (int)rideLength;
                    if (RideLengthScore > 20)
                    {
                        RideLengthScore = 20;
                    }
                }

                // calculate walking time score if needed
                if (WalkingTimeScore == -1)
                {
                    int walkingTime = Utility.MilesToMinutesWalk(TotalDistance);
                    WalkingTimeScore = walkingTime; // / 2;
                    if (WalkingTimeScore > 40)
                    {
                        WalkingTimeScore = 40;
                    }
                }

                // always calculate the NextBusArrivalScore
                var predictionsString = PredictionFetcher.GetPrediction(Route.Tag, StartStop.Tag, Direction);
                if (predictionsString == null || predictionsString == PredictionFetcher.FETCHING_PREDICTIONS_STRING || predictionsString == PredictionFetcher.BUS_NOT_RUNNING_AT_THIS_TIME_STRING)
                {
                    NextBusArrivalScore = 20;
                }
                else
                {
                    var firstPrediction = Regex.Replace(predictionsString, "(?m)^((Bus)|(Train))\\sin\\s([0-9]+).*$", "$4");
                    Debug.WriteLine("predictions string is '{0}'. We got {1} for first time.", predictionsString, firstPrediction);
                    NextBusArrivalScore = int.Parse(firstPrediction) / 2;
                    if (NextBusArrivalScore > 20)
                    {
                        NextBusArrivalScore = 20;
                    }
                }

                Debug.WriteLine("Total score for Route " + Route.Tag + " is " + TotalScore + ". (W: " + WalkingTimeScore + ", RL: " + RideLengthScore + ", NBA: " + NextBusArrivalScore + ")");

                return oldTotalScore != TotalScore;
            }

            int IComparable.CompareTo(object another)
            {
                return TotalScore.CompareTo((another as RouteFinderResult).TotalScore);
            }
    }
}
