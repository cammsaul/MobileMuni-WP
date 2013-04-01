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
    public partial class FoursquareGeocoderResultsPage : PhoneApplicationPage
    {
        private bool FieldIsStartField;
        private List<FoursquareFetcher.Result> Results;
        private FoursquareFetcher.Result GeocodingResult;

        private bool FoursquareFinished;
        private bool GeocoderFinished;

        public FoursquareGeocoderResultsPage()
        {
            InitializeComponent();

            ListBox.ItemsSource = null; // forces update this way (?)
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            FoursquareFinished = false;
            GeocoderFinished = false;

            string field = NavigationContext.QueryString["field"];
            FieldIsStartField = field == "start";
            TitleTextBlock.Text = FieldIsStartField ? "start" : "destination";

            // show the loading bar blocking the screen
            ProgressBar.Visibility = Visibility.Visible;

            string query = NavigationContext.QueryString["query"];

            // fetch the results from foursqare
            FoursquareFetcher.ResultsFetched += FoursquareFetcher_ResultsFetched;
            FoursquareFetcher.Fetch(query);

            // fetch the geocoder results
            GeocodingResult = null;
            Geocoder.GeocodingFinished += Geocoder_GeocodingFinished;
            Geocoder.GeocodeAddress(query);
        }

        private void Geocoder_GeocodingFinished(FoursquareFetcher.Result result)
        {
            GeocoderFinished = true;
            GeocodingResult = result;

            if (FoursquareFinished && GeocoderFinished)
            {
                FoursquareAndGeocoderFinished();
            }
        }

        private void FoursquareFetcher_ResultsFetched(List<FoursquareFetcher.Result> results)
        {
            FoursquareFinished = true;
            Results = results;

            if (FoursquareFinished && GeocoderFinished)
            {
                FoursquareAndGeocoderFinished();
            }
        }

        private void FoursquareAndGeocoderFinished()
        {
            // remove the loading bar blocking the screen
            ProgressBar.Visibility = Visibility.Collapsed;

            // load the results
            if (GeocodingResult != null && !Results.Contains(GeocodingResult))
            {
                Results.Insert(0, GeocodingResult); // address result should be first
            }
            ListBox.ItemsSource = Results;
        }

        private void StackPanel_Tap_1(object sender, System.Windows.Input.GestureEventArgs e)
        {
            FoursquareFetcher.Result result = (sender as StackPanel).DataContext as FoursquareFetcher.Result;
            double latitude = result.Latitude;
            double longitude = result.Longitude;

            if (FieldIsStartField)
            {
                RouteFinderResultsManager.StartLatitude = latitude;
                RouteFinderResultsManager.StartLongitude = longitude;
            }
            else
            {
                RouteFinderResultsManager.EndLatitude = latitude;
                RouteFinderResultsManager.EndLongitude = longitude;
            }

            // go ahead and geocode the end field if it hasn't been done already (e.g. end field is not current location)
            // otherwise navigate to the results page
            if (FieldIsStartField && RouteFinderResultsManager.EndLatitude == null)
            {
                MainPage.Current.NavigationService.Navigate(new Uri("/ViewModels+Views/FoursquareGeocoderResultsPage.xaml?field=end&query=" + RouteFinderResultsManager.EndName, UriKind.Relative));
            }
            else
            {
                MainPage.Current.NavigationService.Navigate(new Uri("/ViewModels+Views/RouteFinderResultsPage.xaml", UriKind.Relative));
            }   
        }
    }
}