using System;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Xml.Linq;
using System.Device.Location;

namespace MobileMuni
{
    public class Stop
    {
        public string Tag { get; set; }
        public string Title { get; set; }
        public float Latitude { get; set; }
        public float Longitude { get; set; }
        public GeoCoordinate Coordinate { get { return new GeoCoordinate(Latitude, Longitude); } } 

        public Stop() { }

        public Stop(XElement xmlElement)
        {
            // <stop tag="4529" title="The Embarcadero & Sansome St" lat="37.8050199" lon="-122.4033099" stopId="14529"/>
            this.Tag = (string)xmlElement.Attribute("tag");
            this.Title = (string)xmlElement.Attribute("title");
            this.Latitude = (float)xmlElement.Attribute("lat");
            this.Longitude = (float)xmlElement.Attribute("lon");
        }

        public double DistanceFromLocation(GeoCoordinate location)
        {
            return this.Coordinate.GetDistanceTo(location);   
        }

        public IEnumerable<Route> RoutesServedByThisStop()
        {
            return from route in RouteStore.Routes where route.InboundStopTags.Contains(this.Tag) || route.OutboundStopTags.Contains(this.Tag) select route;
        }

        /// <summary>
        /// This is in miles
        /// </summary>
        /// <param name="endStop"></param>
        /// <param name="route"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        public double CumulativeDistanceToStop(Stop endStop, Route route, Route.Direction direction)
        {
            var stopTags = direction == Route.Direction.Inbound ? route.InboundStopTags : route.OutboundStopTags;
            var stops = StopStore.StopsWithTags(stopTags);
            
            bool startStopFound = false;
            Stop previousStop = null;
            double cumulativeDistance = 0.0;

            foreach (Stop stop in stops)
            {
                if (stop == this)
                {
                    startStopFound = true;
                    previousStop = this;
                    continue;
                }
                if (startStopFound)
                {
                    cumulativeDistance += Utility.DistanceBetweenCoordinates(previousStop.Latitude, previousStop.Longitude, stop.Latitude, stop.Longitude);
                    previousStop = stop;
                }
                if (stop == endStop)
                {
                    break; // we're done
                }                
            }

            return cumulativeDistance;
        }
    }
}
