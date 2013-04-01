using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace MobileMuni
{
    public partial class RouteFinderResultsPage : PhoneApplicationPage
    {
        private List<RouteFinderResult> Results;

        public RouteFinderResultsPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            ListBox.DataContext = null; // reset the data context

            ProgressBar.Visibility = Visibility.Visible;

            TitleTextBlock.Text = RouteFinderResultsManager.StartName + " to " + RouteFinderResultsManager.EndName;

            // listen for updates to the results (predictions fetched etc)
            RouteFinderResultsManager.ResultsUpdated += RouteFinderResultsManager_ResultsUpdated;

            // calculate the results 
            RouteFinderResultsManager.CalculateResultsFinished += RouteFinderResultsManager_CalculateResultsFinished;
            RouteFinderResultsManager.CalculateResuts();
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);

            // remove the extra predictions from the PredictionsManager
            PredictionFetcher.AdditionalPredictionsToFetch.Clear();

            RouteFinderResultsManager.Results.Clear();
        }

        private void RouteFinderResultsManager_CalculateResultsFinished()
        {
            // load the results
            Results = RouteFinderResultsManager.Results;
            ListBox.ItemsSource = Results;

            // hide the progress bar
            ProgressBar.Visibility = Visibility.Collapsed;
        }

        private void RouteFinderResultsManager_ResultsUpdated()
        {
            // refresh the results.
            Results = RouteFinderResultsManager.Results;
            ListBox.ItemsSource = null;
            ListBox.ItemsSource = Results;
        }

        private void StackPanel_Tap_1(object sender, System.Windows.Input.GestureEventArgs e)
        {
            var result = (sender as StackPanel).DataContext as RouteFinderResult;

            while (NavigationService.BackStack.Count() > 1)
            {
                NavigationService.RemoveBackEntry();
            }
            NavigationService.GoBack();

            var newState = new StateMachine.State(result.Route, result.Direction, result.StartStop, StateMachine.State.LayoutState.StopOverview);
            newState.EndStop = result.EndStop;
            StateMachine.PushState(newState);

            //NavigationService.Navigate(new Uri("/MainPage.xaml?stopTag=" + result.StartStop.Tag + "&routeTag=" + result.Route.Tag + "&direction=" 
            //    + (result.Direction == Route.Direction.Inbound ? "inbound" : "outbound") + "&endStopTag=" + result.EndStop.Tag, UriKind.Relative));            
        }
    }
}