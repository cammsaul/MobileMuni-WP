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
using System.Diagnostics;
using Microsoft.Phone.Shell;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using System.Device.Location;

namespace MobileMuni
{
    public partial class StopOverview : UserControl, IEntranceExitAnimation
    {

        public Storyboard EntranceAnimation { get { return EntranceAnim; } } 

        public StopOverview()
        {
            InitializeComponent();

            StateMachine.StateChanged += new StateMachine.StateChangedDelegate(StateMachine_StateChanged);
            StateMachine_StateChanged(StateMachine.CurrentState); // call once to setup UI

            PredictionFetcher.PredictionsUpdated += new PredictionFetcher.PredictionsUpdatedDelegate(PredictionFetcher_PredictionsUpdated);
        }

        public void StateMachine_StateChanged(StateMachine.State newState)
        {
            if (newState.Stop == null)
            {
                return; // nothing to do
            }

            this.DataContext = newState.Stop;

            // set the text for "27 - Outbound" and "routes served by this stop" (etc) label programatically
            if (newState.Route == null)
            {
                RoutesServedByThisStopLabel.Text = "routes served by this stop";
                SwitchDirectionButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                RoutesServedByThisStopLabel.Text = "other routes served by this stop";
                SwitchDirectionButton.Visibility = Visibility.Visible;
                SwitchDirectionButton.Content = String.Format("{0} - {1}", newState.Route.Tag, newState.Direction == Route.Direction.Inbound ? "Inbound" : "Outbound");
            }

            // create the "routes served by this stop" grid programatically
            IEnumerable<Route> routes = newState.Stop.RoutesServedByThisStop();

            // remove everything except for the "other routes" label
            for (int i = ButtonsVerticalStackPanel.Children.Count() - 1; i >= 0; i--)
            {
                if (ButtonsVerticalStackPanel.Children[i] is StackPanel)
                {
                    ButtonsVerticalStackPanel.Children.RemoveAt(i);
                }
            }

            // determine if we're a favorite or not
            if (FavoriteStore.Favorite(newState.Stop, newState.Route, newState.Direction) != null)
            {
                FavoriteButton.Content = "remove favorite";
            }
            else
            {
                FavoriteButton.Content = "add favorite";
            }

            // hide the favorite/tile buttons if there's no route
            if (StateMachine.CurrentState.Route == null)
            {
                FavoriteTileButtonsVerticalStackPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                FavoriteTileButtonsVerticalStackPanel.Visibility = Visibility.Visible;
            }

            // if there's routes then show the other routes panel; always show if we're doing a "nearby stops" type of thing
            if (routes == null || (routes.Count() < 2 && newState.Route != null))
            {
                ButtonsVerticalStackPanel.Visibility = Visibility.Collapsed;
                return;
            }
            else
            {
                ButtonsVerticalStackPanel.Visibility = Visibility.Visible;                
            }

            // update the predictions label
            bool canShowPredictions = StateMachine.CurrentState.Route != null && StateMachine.CurrentState.Stop != null;
            PredictionsLabel.Text = canShowPredictions ? PredictionFetcher.GetPrediction(StateMachine.CurrentState.Route.Tag, StateMachine.CurrentState.Stop.Tag, StateMachine.CurrentState.Direction) : null;
            
            Brush mobileMuniRedBrush = (SolidColorBrush)(Application.Current.Resources["MobileMuniRedBrush"]);

            // make a button for all routes that aren't the current route
            routes = from route in routes where route != StateMachine.CurrentState.Route select route;

            for (int i = 0; i < routes.Count(); i += 5)
            {
                StackPanel horizontalStackPanel = new StackPanel()
                {
                    Orientation = Orientation.Horizontal,
                    Height = 60,
                    VerticalAlignment = VerticalAlignment.Top
                };
                ButtonsVerticalStackPanel.Children.Add(horizontalStackPanel);

                for (int j = i; j < i + 5 && j < routes.Count(); j++)
                {
                    Route route = routes.ElementAt(j);

                    Button button = new Button()
                    {
                        Content = route.Tag,
                        Tag = route,
                        Foreground = mobileMuniRedBrush,
                        BorderBrush = mobileMuniRedBrush,
                        Height = 70,
                        Margin = new Thickness(-3, -5, -3, -5),
                        Width = 102,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        FontSize = 15,
                        VerticalAlignment = VerticalAlignment.Top
                    };
                    button.Click += new RoutedEventHandler(otherRouteButton_Click);
                    horizontalStackPanel.Children.Add(button);
                }
            }
        }

        public void PredictionFetcher_PredictionsUpdated()
        {
            // just update the predictions label
            bool canShowPredictions = StateMachine.CurrentState.Route != null && StateMachine.CurrentState.Stop != null;
            PredictionsLabel.Text = canShowPredictions ? PredictionFetcher.GetPrediction(StateMachine.CurrentState.Route.Tag, StateMachine.CurrentState.Stop.Tag, StateMachine.CurrentState.Direction) : null;
        }


        void SwitchDirectionButton_Click(object sender, RoutedEventArgs e)
        {
            Route.Direction newDirection = StateMachine.CurrentState.Direction == Route.Direction.Inbound ? Route.Direction.Outbound : Route.Direction.Inbound;

            // if the stop isn't part of the other direction, then just grab the nearest stop
            Route currentRoute = StateMachine.CurrentState.Route;
            Stop currentStop = StateMachine.CurrentState.Stop;
            List<string> stopTags = newDirection == Route.Direction.Inbound ? currentRoute.InboundStopTags : currentRoute.OutboundStopTags;
            if (!stopTags.Contains(currentStop.Tag))
            {
                // find the next best thing
                Stop newStop = (from stopResult in StopStore.StopsWithTags(stopTags)
                                orderby stopResult.DistanceFromLocation(new GeoCoordinate(currentStop.Latitude, currentStop.Longitude))
                                select stopResult).ThenBy(stop => stop).First();
                if (newStop != null)
                {
                    currentStop = newStop;
                }
            }

            StateMachine.PushState(StateMachine.CurrentState.WithDirection(newDirection).WithStop(currentStop));
        }

        void otherRouteButton_Click(object sender, RoutedEventArgs e)
        {
            Route currentRoute = (sender as Button).Tag as Route;
            Route.Direction currentDirection = StateMachine.CurrentState.Direction == Route.Direction.None ? Route.Direction.Inbound : StateMachine.CurrentState.Direction;
            Stop currentStop = StateMachine.CurrentState.Stop;

            // if the stop isn't part of the other direction, then just grab the nearest stop
            List<string> stopTags = currentDirection == Route.Direction.Inbound ? currentRoute.InboundStopTags : currentRoute.OutboundStopTags;
            if (!stopTags.Contains(currentStop.Tag))
            {
                // attempt to switch direction
                stopTags = currentDirection != Route.Direction.Inbound ? currentRoute.InboundStopTags : currentRoute.OutboundStopTags;
                if (stopTags.Contains(currentStop.Tag))
                {
                    currentDirection = currentDirection == Route.Direction.Inbound ? Route.Direction.Outbound : Route.Direction.Inbound;
                }
                else
                {
                    // find the next best thing
                    Stop newStop = (from stopResult in StopStore.StopsWithTags(stopTags)
                                    orderby stopResult.DistanceFromLocation(new GeoCoordinate(currentStop.Latitude, currentStop.Longitude))
                                    select stopResult).ThenBy(stop => stop).First();
                    if (newStop != null)
                    {
                        currentStop = newStop;
                    }
                }
           
            }
            StateMachine.PushState(new StateMachine.State(currentRoute, currentDirection, currentStop, StateMachine.State.LayoutState.StopOverview));
        }

        void FavoriteButton_Click(object sender, RoutedEventArgs e)
        {
        Favorite exisitingFavorite = FavoriteStore.Favorite(StateMachine.CurrentState.Stop, StateMachine.CurrentState.Route, StateMachine.CurrentState.Direction);
            if (exisitingFavorite != null)
            {
                FavoriteStore.Favorites.Remove(exisitingFavorite);
                FavoriteButton.Content = "add favorite";
            }
            else
            {
                FavoriteStore.Favorites.Add(new Favorite()
                {
                    RouteTag = StateMachine.CurrentState.Route.Tag,
                    Direction = StateMachine.CurrentState.Direction,
                    StopTag = StateMachine.CurrentState.Stop.Tag
                });
                FavoriteButton.Content = "remove favorite";
            }

            // save our changes to disk
            FavoriteStore.Save();
        }

        void AddTileButton_Click(object sender, RoutedEventArgs e)
        {
            if (StateMachine.CurrentState.Stop == null || StateMachine.CurrentState.Route == null || StateMachine.CurrentState.Direction == Route.Direction.None)
            {
                // weird state, just apologize and GTFO
                Debug.WriteLine("Invalid operation exception while attempting to add tile to home screen: Stop, Route, or Direction invalid");
                MessageBox.Show("You've already pinned this stop to your home screen! :-)\n");
                return;
            }

            string routeWithDirection = String.Format("{0} - {1}", StateMachine.CurrentState.Route.Tag, StateMachine.CurrentState.Direction == Route.Direction.Inbound ? "Inbound" : "Outbound"); 
            var newTile = new StandardTileData()
            {
                Title = routeWithDirection,
                BackgroundImage = new Uri("/Images/Background_Transparent.png", UriKind.Relative),
                BackBackgroundImage = new Uri("/Images/Background_Blank.png", UriKind.Relative),
                BackTitle = routeWithDirection,
                BackContent = StateMachine.CurrentState.Stop.Title
            };
            var uri = String.Format("/MainPage.xaml?stopTag={0}&routeTag={1}&direction={2}", StateMachine.CurrentState.Stop.Tag, StateMachine.CurrentState.Route.Tag, 
                StateMachine.CurrentState.Direction == Route.Direction.Inbound ? "inbound" : "outbound");
            
            try
            {
                ShellTile.Create(new Uri(uri, UriKind.Relative), newTile);
            }
            catch (InvalidOperationException ex)
            {
                // no idea what's going on here
                Debug.WriteLine("Invalid operation exception while attempting to add tile to home screen: {0}", ex);
                MessageBox.Show("Sorry, we couldn't add the tile at this time. Maybe you've already added it? :)\n");
            }
        }
    }
}

