using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Device.Location;
using Microsoft.Phone.Controls.Maps;
using System.Diagnostics;

namespace MobileMuni
{
    public class MapCredentialsProvider : CredentialsProvider
    {
        public override void GetCredentials(Action<Credentials> callback)
        {
            Credentials credentials = new Credentials();
            credentials.ApplicationId = Settings.MapCredentials;
            callback(credentials);
        }
    }

    public partial class MapControl : UserControl
    {
        private MapLayer RouteLayer;
        private ArrowMapPolyline RouteLine;

        public delegate void StopPushpinClickedListener(Stop stop);
        public StopPushpinClickedListener StopPushpinClicked;

        private Pushpin CurrentLocationPin;

        public MapControl()
        {
            InitializeComponent();
            Map.MapZoom += new EventHandler<MapZoomEventArgs>(Map_MapZoom);

            StateMachine.StateChanged += new StateMachine.StateChangedDelegate(StateMachine_StateChanged);
            
            Map.CredentialsProvider = new MapCredentialsProvider();
        }

        public void StateMachine_StateChanged(StateMachine.State newState)
        {
            if (newState.Layout == StateMachine.State.LayoutState.Favorites)
            {
                return; // no need to change anything
            }

            if (newState.Layout == StateMachine.State.LayoutState.StopOverview)
            {
                if (newState.EndStop == null)
                {
                    bool resize = StateMachine.PreviousState == null || StateMachine.PreviousState.Layout != StateMachine.State.LayoutState.StopOverview;
                    SetCenter(center: newState.Stop.Coordinate, resize: resize);
                }
                else
                {
                    SetViewForRoute(newState.Route);
                    double latMid = (StateMachine.CurrentState.Stop.Latitude + StateMachine.CurrentState.EndStop.Latitude) / 2.0;
                    double lonMid = (StateMachine.CurrentState.Stop.Longitude + StateMachine.CurrentState.EndStop.Longitude) / 2.0;
                    SetCenter(center: new GeoCoordinate(latMid, lonMid), resize: false);
                }                
            }
            else if (newState.Layout == StateMachine.State.LayoutState.RouteOverview)
            {
                if (StateMachine.RouteChanged())
                {
                    SetViewForRoute(newState.Route);
                }
            }
            else if (newState.Layout == StateMachine.State.LayoutState.NearbyStops)
            {
                SetCenter(center: StateMachine.CurrentLocation, resize: true);
            }

            if (StateMachine.StopsChanged() || StateMachine.RouteChanged() || StateMachine.DirectionChanged() || newState.Layout == StateMachine.State.LayoutState.NearbyStops)
            {
                Map.Children.Clear();

                // Nearby Stops or StopOverview via NearbyStops. Keep "nearby stops" annotations.
                if (newState.Route == null && StateMachine.CurrentLocation != StateMachine.DEFAULT_LOCATION) 
                {
                    List<Stop> nearbyStops = StopStore.StopsNearLocation(StateMachine.CurrentLocation);
                    AddStops(nearbyStops, showRouteOverlay: false);
                }

                if (newState.Layout == StateMachine.State.LayoutState.NearbyStops) // nearby stops 
                {
                    List<Stop> nearbyStops = StopStore.StopsNearLocation(StateMachine.CurrentLocation);
                    AddStops(nearbyStops, showRouteOverlay: false);
                }
                else
                {
                    // add stops for the route
                    if (newState.Direction == Route.Direction.Inbound)
                    {
                        AddStops(newState.Route.InboundStopTags, showRouteOverlay: true);
                    }
                    else if (newState.Direction == Route.Direction.Outbound)
                    {
                        AddStops(newState.Route.OutboundStopTags, showRouteOverlay: true);
                    }
                }
            }

            // update current location pin in case we tossed it 
            UpdateCurrentLocationPin();
        }

        void Map_MapZoom(object sender, MapZoomEventArgs e)
        {
            double zoomFactor = Math.Max(1.0, Math.Pow(Map.ZoomLevel / 10, 2));
            if (Map.TargetZoomLevel == Map.ZoomLevel)
            {
                if (this.RouteLine != null)
                {
                    this.RouteLine.StrokeThickness = (Map.ZoomLevel * zoomFactor) / 4.0;
                    this.RouteLine.ArrowLength = (Map.ZoomLevel * zoomFactor) / 3.0;
                    this.RouteLine.UpdateLayout();
                }
                foreach (Pushpin pin in from child in Map.Children where child is Pushpin select child)
                {
                    pin.Width = Map.ZoomLevel * zoomFactor;
                    pin.Height = Map.ZoomLevel * zoomFactor;
                    if (pin.Tag as string != null)
                    {
                        Stop stop = StopStore.Stops[(string)pin.Tag];
                        pin.Location = stop.Coordinate;
                    }
                }
                Map.UpdateLayout();
            }
        }

        public void SetCenter(GeoCoordinate center, bool resize)
        {
            Map.Center = center;
            if (resize)
            {
                Map.ZoomLevel = 17;
            }
        }

        private void SetViewForRoute(Route route)
        {
            if (route == null)
            {
                Map.Children.Clear();
            }
            else
            {
                this.SetViewForRoute(route.LatMin, route.LatMax, route.LonMin, route.LonMax);
            }
        }

        private void SetViewForRoute(float latMin, float latMax, float lonMin, float lonMax)
        {
            GeoCoordinate latMinLonMin, latMinLonMax, latMaxLonMin, latMaxLonMax;
            latMinLonMin = new GeoCoordinate(latMin, lonMin);
            latMinLonMax = new GeoCoordinate(latMin, lonMax);
            latMaxLonMin = new GeoCoordinate(latMax, lonMin);
            latMaxLonMax = new GeoCoordinate(latMax, lonMax);
            Map.SetView(LocationRect.CreateLocationRect(latMaxLonMin, latMinLonMax, latMaxLonMin, latMaxLonMax));
        }

        private void AddStop(GeoCoordinate location, string stopTag)
        {
            Pushpin pushpin = new Pushpin();
            pushpin.Location = location;
            // make the current stop (and/or end stop) red
            if (StateMachine.CurrentState.Stop != null && stopTag.Equals(StateMachine.CurrentState.Stop.Tag))
            {
                pushpin.Style = (Style)(Application.Current.Resources["PushpinStyleStopRed"]);
            }
            else if (StateMachine.CurrentState.EndStop != null && stopTag.Equals(StateMachine.CurrentState.EndStop.Tag))
            {
                pushpin.Style = (Style)(Application.Current.Resources["PushpinStyleStopRed"]);
            }
            else
            {
                pushpin.Style = (Style)(Application.Current.Resources["PushpinStyleStopWhite"]);
            }
            pushpin.Tag = stopTag;
            double zoomFactor = Math.Max(1.0, Math.Pow(Map.TargetZoomLevel / 10, 2));
            pushpin.Width = Map.TargetZoomLevel * zoomFactor;
            pushpin.Height = Map.TargetZoomLevel * zoomFactor;
            pushpin.MouseLeftButtonDown += new MouseButtonEventHandler(pushpin_MouseLeftButtonDown);
            Map.Children.Add(pushpin);
        }

        private void AddStop(Stop stop)
        {
            this.AddStop(stop.Coordinate, stop.Tag);
        }

        public void AddStops(List<Stop> stops, Boolean showRouteOverlay)
        {
            foreach (Stop stop in stops)
            {
                this.AddStop(stop);
            }
            if (showRouteOverlay)
            {
                this.SetRouteOverlay(stops);
            }
            else
            {
                this.SetRouteOverlay(null);
            }
        }

        public void SetRouteOverlay(List<Stop> stopsOrNull)
        {
            // add map layer on which to draw the route
            if (this.RouteLayer == null)
            {
                this.RouteLayer = new MapLayer();
            }
            if (!this.Map.Children.Contains(this.RouteLayer))
            {
                this.Map.Children.Insert(0, this.RouteLayer);
            }

            this.RouteLayer.Children.Clear();
            this.RouteLine = null;

            if (stopsOrNull == null || stopsOrNull.Count() < 2)
            {
                return;
            }

            this.RouteLine = new ArrowMapPolyline();
            double zoomFactor = Math.Max(1.0, Math.Pow(Map.TargetZoomLevel / 10, 2));
            this.RouteLine.StrokeThickness = (Map.TargetZoomLevel * zoomFactor) / 4.0;
            this.RouteLine.ArrowLength = (Map.TargetZoomLevel * zoomFactor) / 3.0;

            var locations = new LocationCollection();
            foreach (var coordinate in from stop in stopsOrNull select stop.Coordinate)
            {
                locations.Add(coordinate);
            }
            this.RouteLine.Locations = locations;
            this.RouteLayer.Children.Add(this.RouteLine);
        }

        public void AddStops(List<string> stopTags, bool showRouteOverlay)
        {
            this.AddStops(StopStore.StopsWithTags(stopTags), showRouteOverlay);
        }

        public void UpdateCurrentLocationPin()
        {
            if (StateMachine.CurrentLocation == StateMachine.DEFAULT_LOCATION)
            {
                return; // nothing to do
            }

            if (CurrentLocationPin == null)
            {
                CurrentLocationPin = new Pushpin();
                CurrentLocationPin.Style = (Style)(Application.Current.Resources["PushpinStyleCurrentLocation"]);
            }

            CurrentLocationPin.Location = StateMachine.CurrentLocation;

            double zoomFactor = Math.Max(1.0, Math.Pow(Map.ZoomLevel / 10, 2));
            CurrentLocationPin.Width = Map.ZoomLevel * zoomFactor;
            CurrentLocationPin.Height = Map.ZoomLevel * zoomFactor;

            if (!Map.Children.Contains(CurrentLocationPin))
            {
                Map.Children.Add(CurrentLocationPin);
            } else {
                Map.UpdateLayout();
            }
        }

        void pushpin_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            string stopTag = (sender as Pushpin).Tag as string;
            if (stopTag != null)
            {
                Stop stop = StopStore.Stops[stopTag];
                StopPushpinClicked(stop);
            }         
        }
    }
}
