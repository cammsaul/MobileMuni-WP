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
using Microsoft.Phone.Controls;
using Microsoft.Phone.Controls.Maps;
using System.Device.Location;
using System.Diagnostics;
using System.IO.IsolatedStorage;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using Google.AdMob.Ads.WindowsPhone7;

namespace MobileMuni
{
    public partial class MainPage : PhoneApplicationPage
    {
        // various views
        private StopOverview StopOverview;
        private RoutePicker RoutePicker;
        private RouteOverview RouteOverview;
        private FavoritesView FavoritesView;
        private RouteFinderView RouteFinderView;
        private FetchingOverlay FetchingOverlay;

        private GeoCoordinateWatcher GeoCoordinateWatcher;

        private bool HasFetchedCurrentLocation;
        private bool HasLoadedStores;
        private static string ENABLE_CURRENT_LOCATION = "EnableCurrentLocation";
        private bool EnableCurrentLocation;
        private Favorite PendingState; // load this after the stores finish loading if applicable (I know favorite is not a state object but it more convinient)

        private UserControl CurrentTopView;

        public static MainPage Current; 

        // Constructor
        public MainPage()
        {
            InitializeComponent();

             if (IsolatedStorageSettings.ApplicationSettings.Contains(ENABLE_CURRENT_LOCATION))
             {
                EnableCurrentLocation = (bool)IsolatedStorageSettings.ApplicationSettings[ENABLE_CURRENT_LOCATION];
                if (!EnableCurrentLocation)
                {
                    LocationServicesBarMenuItem = this.ApplicationBar.MenuItems[0] as ApplicationBarMenuItem;
                    LocationServicesBarMenuItem.Text = "enable current location";
                }
             } else {
                 EnableCurrentLocation = true;
             }


            MapControl.StopPushpinClicked += new MobileMuni.MapControl.StopPushpinClickedListener(MapControl_StopPushpinClicked);

            Fetcher.FetchingBegan += new Fetcher.FetchingBeganDelegate(Fetcher_FetchingBegan);
            Fetcher.FetchingComplete += new Fetcher.FetchingCompleteDelegate(Fetcher_FetchingComplete);
            Fetcher.RestoringBegan += new Fetcher.RestoringBeganDelegate(Fetcher_RestoringBegan);
            Fetcher.RestoringComplete += new Fetcher.RestoringCompleteDelegate(Fetcher_RestoringComplete);
            Fetcher.fetch(forced: false);

            // listen to state machine and push initial state
            StateMachine.PushState(new StateMachine.State());
            StateMachine.StateChanged += new StateMachine.StateChangedDelegate(StateMachine_StateChanged);

            Current = this;

            AdControl.AdUnitID = Settings.AdMobID;
        }

        public void StateMachine_StateChanged(StateMachine.State newState)
        {
            if (StateMachine.LayoutChanged())
            {
                this.RemoveAllTopViews();

                if (newState.Layout == StateMachine.State.LayoutState.RouteOverview)
                {
                    // show the Route Overview
                    if (this.RouteOverview == null)
                    {
                        this.RouteOverview = new RouteOverview();
                    }
                    this.AddTopView(this.RouteOverview);
                }
                else if (newState.Layout == StateMachine.State.LayoutState.RoutePicker)
                {
                    if (this.RoutePicker == null)
                    {
                        this.RoutePicker = new RoutePicker();
                        this.RoutePicker.RouteSelected += new RoutePicker.RouteSelectedDelegate(RoutePicker_RouteSelected);
                    }
                    this.AddTopView(this.RoutePicker);
                }
                else if (newState.Layout == StateMachine.State.LayoutState.StopOverview)
                {
                    // show the stop overview
                    if (this.StopOverview == null)
                    {
                        this.StopOverview = new StopOverview();
                    }
                    this.AddTopView(this.StopOverview);
                }
                else if (newState.Layout == StateMachine.State.LayoutState.Favorites)
                {
                    if (this.FavoritesView == null)
                    {
                        this.FavoritesView = new FavoritesView();
                    }
                    this.AddTopView(this.FavoritesView);
                }
                else if (newState.Layout == StateMachine.State.LayoutState.RouteFinder)
                {
                    if (this.RouteFinderView == null)
                    {
                        this.RouteFinderView = new RouteFinderView();
                    }
                    this.AddTopView(this.RouteFinderView);
                }
            }
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            string stopTag = null;
            string routeTag = null;
            Route.Direction direction = Route.Direction.None;

            if (!NavigationContext.QueryString.ContainsKey("stopTag")) {
                return; // DONE!
            }

            if (NavigationContext.QueryString.ContainsKey("stopTag"))
            {
                stopTag = NavigationContext.QueryString["stopTag"];
                HasFetchedCurrentLocation = true; // not true but we don't want to jump away from the stop when it's fetched
            }
            if (NavigationContext.QueryString.ContainsKey("routeTag"))
            {
                routeTag = NavigationContext.QueryString["routeTag"];
            }
            if (NavigationContext.QueryString.ContainsKey("direction"))
            {
                string d = NavigationContext.QueryString["direction"];
                direction = d.Equals("inbound") ? Route.Direction.Inbound : Route.Direction.Outbound;
            }

            Favorite pendingState = new Favorite() {
                RouteTag = routeTag,
                StopTag = stopTag,
                Direction = direction
            };
            if (!HasLoadedStores)
            {
                PendingState = pendingState;
            }
            else 
            {               
                StateMachine.PushState(new StateMachine.State(RouteStore.RouteForTag(pendingState.RouteTag), PendingState.Direction, StopStore.Stops[pendingState.StopTag], StateMachine.State.LayoutState.StopOverview));
            }
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            if (StateMachine.StateCount > 1)
            {
                StateMachine.PopState();
                e.Cancel = true;
            }
            else
            {
                base.OnBackKeyPress(e);
            }
        }

        private void Fetcher_RestoringBegan()
        {
            this.ProgressBar.Visibility = Visibility.Visible;
        }

        private void Fetcher_RestoringComplete()
        {
            this.ProgressBar.Visibility = Visibility.Collapsed;

            if (EnableCurrentLocation)
            {
                GeoCoordinateWatcher = new GeoCoordinateWatcher(GeoPositionAccuracy.High);
                GeoCoordinateWatcher.PositionChanged += new EventHandler<GeoPositionChangedEventArgs<GeoCoordinate>>(GeoCoordinateWatcher_PositionChanged);
                GeoCoordinateWatcher.Start();
            }

            // start the prediction fetcher
            PredictionFetcher.StartFecthing();

            HasLoadedStores = true;
            if (PendingState != null)
            {
                StateMachine.State state = new StateMachine.State(RouteStore.RouteForTag(PendingState.RouteTag), PendingState.Direction, StopStore.Stops[PendingState.StopTag],         StateMachine.State.LayoutState.StopOverview);
                StateMachine.PushState(state);
            }
        }

        private void Fetcher_FetchingBegan()
        {
            this.ProgressBar.Visibility = Visibility.Collapsed;
            this.FetchingOverlay = new FetchingOverlay();
            this.ApplicationBar.IsVisible = false;
            LayoutRoot.Children.Add(this.FetchingOverlay);

        }

        private void Fetcher_FetchingComplete()
        {
            LayoutRoot.Children.Remove(this.FetchingOverlay);
            this.ApplicationBar.IsVisible = true;
            this.FetchingOverlay = null;

            if (EnableCurrentLocation) {
                GeoCoordinateWatcher = new GeoCoordinateWatcher(GeoPositionAccuracy.High);
                GeoCoordinateWatcher.PositionChanged += new EventHandler<GeoPositionChangedEventArgs<GeoCoordinate>>(GeoCoordinateWatcher_PositionChanged);
                GeoCoordinateWatcher.Start();
            }

            // start the prediction fetcher
            PredictionFetcher.StartFecthing();

            HasLoadedStores = true;
            if (PendingState != null)
            {
                StateMachine.State state = new StateMachine.State(RouteStore.RouteForTag(PendingState.RouteTag), PendingState.Direction, StopStore.Stops[PendingState.StopTag], StateMachine.State.LayoutState.StopOverview);
                StateMachine.PushState(state);
            }
        }

        private void MapControl_StopPushpinClicked(Stop stop)
        {
            if (stop != StateMachine.CurrentState.Stop)
            {
                StateMachine.PushState(StateMachine.CurrentState.WithLayout(StateMachine.State.LayoutState.StopOverview).WithStop(stop));
            }
        }

        void GeoCoordinateWatcher_PositionChanged(object sender, GeoPositionChangedEventArgs<GeoCoordinate> e)
        {
            if (!e.Position.Location.IsUnknown)
            {
                double latitude = e.Position.Location.Latitude;
                double longitude = e.Position.Location.Longitude;

                StateMachine.CurrentLocation = new GeoCoordinate(latitude, longitude);
                MapControl.UpdateCurrentLocationPin();

                if (!HasFetchedCurrentLocation)
                {
                    HasFetchedCurrentLocation = true;
                    MapControl.SetCenter(center: StateMachine.CurrentLocation, resize: true);
                }
            }
        }

        void FavoritesButton_Click(object sender, EventArgs e)
        {
            if (!HasLoadedStores)
            {
                return;
            }

            if (StateMachine.CurrentState.Layout != StateMachine.State.LayoutState.Favorites)
            {
                StateMachine.PushState(new StateMachine.State().WithLayout(StateMachine.State.LayoutState.Favorites));
            }
        }

        private void RouteFinderButton_Click(object sender, EventArgs e)
        {
            if (!HasLoadedStores)
            {
                return;
            }

            if (StateMachine.CurrentState.Layout != StateMachine.State.LayoutState.RouteFinder)
            {
                StateMachine.PushState(new StateMachine.State().WithLayout(StateMachine.State.LayoutState.RouteFinder));
            }
        }

        void RoutesButton_Click(object sender, EventArgs e)
        {
            if (!HasLoadedStores)
            {
                return;
            }

            if (StateMachine.CurrentState.Layout != StateMachine.State.LayoutState.RoutePicker)
            {
                StateMachine.PushState(new StateMachine.State().WithLayout(StateMachine.State.LayoutState.RoutePicker));
            }
        }

        void RoutePicker_RouteSelected(Route route)
        {
            // default to outbound direction if we currently don't have one
            Route.Direction direction = StateMachine.CurrentState.Direction == Route.Direction.None ? Route.Direction.Outbound : StateMachine.CurrentState.Direction;
            StateMachine.PushState(new StateMachine.State().WithLayout(StateMachine.State.LayoutState.RouteOverview).WithRoute(route).WithDirection(direction));
        }

        void PrivacyPolicy_Clicked(object sender, EventArgs e)
        {
            MessageBox.Show("MobileMuni can optionally use your current location to find routes and stops nearby. Your location is used locally only on the phone itself -- it is never sent to our servers or any other parties.\n\nPlease email cameron@getluckybird.com with any questions or concerns regarding the privacy policy. ");
        }

        void ToggleLocationServices_Clicked(object sender, EventArgs e)
        {
            const string disableStr = "disable location services";
            const string enableStr = "enable location services";


            ApplicationBarMenuItem barMenuItem = (ApplicationBarMenuItem)sender;
            if (barMenuItem.Text.Equals(disableStr))
            {
                EnableCurrentLocation = false;
                StateMachine.CurrentLocation = StateMachine.DEFAULT_LOCATION;
                barMenuItem.Text = enableStr;
            }
            else
            {
                EnableCurrentLocation = true;
                barMenuItem.Text = disableStr;
            }

            if (EnableCurrentLocation)
            {
                HasFetchedCurrentLocation = false;
                GeoCoordinateWatcher = new GeoCoordinateWatcher(GeoPositionAccuracy.High);
                GeoCoordinateWatcher.PositionChanged += new EventHandler<GeoPositionChangedEventArgs<GeoCoordinate>>(GeoCoordinateWatcher_PositionChanged);
                GeoCoordinateWatcher.Start();
            } 
            else
            {
                GeoCoordinateWatcher.Stop();
            }

            IsolatedStorageSettings.ApplicationSettings[ENABLE_CURRENT_LOCATION] = EnableCurrentLocation;
            IsolatedStorageSettings.ApplicationSettings.Save();
        }

        void NearbyStopsButton_Click(object sender, EventArgs e)
        {
            if (!HasLoadedStores)
            {
                return;
            }

            if (StateMachine.CurrentState.Layout != StateMachine.State.LayoutState.NearbyStops)
            {
                if (StateMachine.CurrentLocation == StateMachine.DEFAULT_LOCATION)
                {
                    MessageBox.Show("Current Location is currently disabled :/\nWe picked a good one for you though!");
                }

                StateMachine.PushState(new StateMachine.State().WithLayout(StateMachine.State.LayoutState.NearbyStops));
            }
        }

        private void AddTopView(UserControl topView)
        {
            UserControl oldTopView = this.CurrentTopView;
            this.CurrentTopView = topView;

            IEntranceExitAnimation animatedExitView = oldTopView as IEntranceExitAnimation;
            if (animatedExitView != null)
            {
                // TODO
            }

            IEntranceExitAnimation animatedEnterView = topView as IEntranceExitAnimation;
            if (animatedEnterView != null)
            {
                animatedEnterView.EntranceAnimation.Begin();
            }

            LayoutRoot.Children.Add(topView);
        }

        private void RemoveAllTopViews()
        {
            UserControl[] userControls = { this.StopOverview, this.RoutePicker, this.RouteOverview, this.FavoritesView, this.RouteFinderView };
            var controls = from userControl in userControls where userControl != null && LayoutRoot.Children.Contains(userControl) select userControl;
            foreach (var control in controls)
            {
                LayoutRoot.Children.Remove(control);
            }
            this.CurrentTopView = null;
        }

        private void AppSupportFeedbackButton_Click_1(object sender, EventArgs e)
        {
            EmailComposeTask emailComposer = new EmailComposeTask();
            emailComposer.To = "cameron@getluckybird.com";
            emailComposer.Subject = "MobileMuni Feedback";
            emailComposer.Body = "loving the app!";
            emailComposer.Show();
        }
    }
}