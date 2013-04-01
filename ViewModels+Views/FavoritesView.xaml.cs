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
using Microsoft.Phone.Shell;
using System.Collections.Specialized;
using Microsoft.Phone.Controls;
using System.Diagnostics;

namespace MobileMuni
{
    public partial class FavoritesView : UserControl, IEntranceExitAnimation
    {
        public Storyboard EntranceAnimation { get { return EntranceAnim; } } 

        public FavoritesView()
        {
            InitializeComponent();
            FavoritesListBox.ItemsSource = FavoriteStore.Favorites;

            NoFavoritesTextBlock.Visibility = FavoriteStore.Favorites.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            FavoriteStore.Favorites.CollectionChanged += new NotifyCollectionChangedEventHandler(FavoritesChanged);

            PredictionFetcher.PredictionsUpdated += new PredictionFetcher.PredictionsUpdatedDelegate(PredictionFetcher_PredictionsUpdated);
        }

        public void FavoritesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            NoFavoritesTextBlock.Visibility = FavoriteStore.Favorites.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        public void PredictionFetcher_PredictionsUpdated()
        {
            FavoritesListBox.ItemsSource = null; // force reload
            FavoritesListBox.ItemsSource = FavoriteStore.Favorites;
        }

        private void FavoriteClicked(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Favorite favorite = (sender as StackPanel).Tag as Favorite;
            Route route = RouteStore.RouteForTag(favorite.RouteTag);
            Stop stop = StopStore.Stops[favorite.StopTag];
            StateMachine.PushState(new StateMachine.State().WithRoute(route).WithDirection(favorite.Direction).WithStop(stop).WithLayout(StateMachine.State.LayoutState.StopOverview));
        }

        private void ContextMenu_Delete_Click_1(object sender, RoutedEventArgs e)
        {
            var favorite = (sender as MenuItem).DataContext as Favorite;
            FavoriteStore.Favorites.Remove(favorite);
        }

        private void ContextMenu_PinToStart_Click_1(object sender, RoutedEventArgs e)
        {
            var favorite = (sender as MenuItem).DataContext as Favorite;

            string routeWithDirection = String.Format("{0} - {1}", favorite.Route.Tag, favorite.Direction == Route.Direction.Inbound ? "Inbound" : "Outbound");
            var newTile = new StandardTileData()
            {
                Title = routeWithDirection,
                BackgroundImage = new Uri("/Images/Background_Transparent.png", UriKind.Relative),
                BackBackgroundImage = new Uri("/Images/Background_Blank.png", UriKind.Relative),
                BackTitle = routeWithDirection,
                BackContent = favorite.Stop.Title
            };
            var uri = String.Format("/MainPage.xaml?stopTag={0}&routeTag={1}&direction={2}", favorite.Stop.Tag, favorite.Route.Tag,
                favorite.Direction == Route.Direction.Inbound ? "inbound" : "outbound");
            
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
