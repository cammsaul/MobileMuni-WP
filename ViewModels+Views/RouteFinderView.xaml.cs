using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Media.Animation;
using System.Windows.Media;

namespace MobileMuni
{
    public partial class RouteFinderView : UserControl, IEntranceExitAnimation
    {
        public Storyboard EntranceAnimation { get { return EntranceAnim; } }

        private bool FindRoutesButtonEnabled = false;

        public RouteFinderView()
        {
            InitializeComponent();

            BookmarkStore.StartBookmarkChanged += new BookmarkStore.StartBookmarkSelectedDelegate(BookmarkStore_StartBookmarkSelected);
            BookmarkStore.EndBookmarkChanged += new BookmarkStore.EndBookmarkSelectedDelegate(BookmarkStore_EndBookmarkSelected);
        }

        protected void BookmarkStore_StartBookmarkSelected(string startBookmark)
        {
            StartTextBox.Text = startBookmark;
            StartTextBox_TextChanged(StartTextBox, null);
        }

        protected void BookmarkStore_EndBookmarkSelected(string endBookmark)
        {
            EndTextBox.Text = endBookmark;
            EndTextBox_TextChanged(EndTextBox, null); 
        }

        private void StartBookmarkButton_Click(object sender, RoutedEventArgs e)
        {
            MainPage.Current.NavigationService.Navigate(new Uri("/ViewModels+Views/BookmarksPage.xaml?field=start", UriKind.Relative));
        }

        private void EndBookmarkButton_Click(object sender, RoutedEventArgs e)
        {
            MainPage.Current.NavigationService.Navigate(new Uri("/ViewModels+Views/BookmarksPage.xaml?field=end", UriKind.Relative));
        }

        private void FlipStartDestButton_Click(object sender, RoutedEventArgs e)
        {
            string temp = StartTextBox.Text;
            StartTextBox.Text = EndTextBox.Text;
            EndTextBox.Text = temp;
        }

        private void FindRoutesButton_Click(object sender, RoutedEventArgs e)
        {
            if (!FindRoutesButtonEnabled)
            {
                return;
            }

            // reset the values we have saved in the RouteFinderResultManager
            RouteFinderResultsManager.ResetAllValues();

            // possible navigation patterns are:
            //  + Start is 'Current Location'
            //      + End is 'Current Location'
            //  *       navigate to FoursquareGeocoderResults page to geocode end. Then navigate to route results
            //      *   navigate to FoursquareGeocoderResults page to geocode start. Then navigate to route results
            //  *   *   tell user that they are being silly
            //          navigate to FoursquareGeocoderResults page to geocode start. Then navigate to FoursquareGeocoderResults page to geocode end. Then navigate to route results

            var startText = StartTextBox.Text;
            var endText = EndTextBox.Text;

            bool startIsCurrentLocation = startText.Equals("Current Location", StringComparison.OrdinalIgnoreCase);
            bool endIsCurrentLocation = endText.Equals("Current Location", StringComparison.OrdinalIgnoreCase);

            RouteFinderResultsManager.StartName = startText;
            RouteFinderResultsManager.EndName = endText;

            // save bookmarks
            BookmarkStore.AddBookmark(startText);
            BookmarkStore.AddBookmark(endText);

            // escape strings so they can be passed correctly in navigationb
            startText = Uri.EscapeDataString(startText);
            endText = Uri.EscapeDataString(endText);

            if (startIsCurrentLocation)
            {
                if (endIsCurrentLocation)
                {
                    // warn user they are being silly
                }

                RouteFinderResultsManager.StartLatitude = StateMachine.CurrentLocation.Latitude;
                RouteFinderResultsManager.StartLongitude = StateMachine.CurrentLocation.Longitude;
                MainPage.Current.NavigationService.Navigate(new Uri("/ViewModels+Views/FoursquareGeocoderResultsPage.xaml?field=end&query=" + endText, UriKind.Relative));
            }
            else if (endIsCurrentLocation)
            {
                RouteFinderResultsManager.EndLatitude = StateMachine.CurrentLocation.Latitude;
                RouteFinderResultsManager.EndLongitude = StateMachine.CurrentLocation.Longitude;
                MainPage.Current.NavigationService.Navigate(new Uri("/ViewModels+Views/FoursquareGeocoderResultsPage.xaml?field=start&query=" + startText, UriKind.Relative));
            }
            else
            {
                MainPage.Current.NavigationService.Navigate(new Uri("/ViewModels+Views/FoursquareGeocoderResultsPage.xaml?field=start&query=" + startText, UriKind.Relative));
            }
        }

        private void StartTextBox_GotFocus_1(object sender, RoutedEventArgs e)
        {
            (sender as TextBox).Background = new SolidColorBrush(Color.FromArgb(255, 221, 221, 221));
        }

        private void StartBookmarkButton_GotFocus_1(object sender, RoutedEventArgs e)
        {
            (sender as TextBox).Background = new SolidColorBrush(Color.FromArgb(255, 221, 221, 221));
        }

        private void EndTextBox_GotFocus_1(object sender, RoutedEventArgs e)
        {
            (sender as TextBox).Background = new SolidColorBrush(Color.FromArgb(255, 221, 221, 221));
        }

        private void StartTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            textBox.Foreground = textBox.Text == "Current Location" ? Application.Current.Resources["MobileMuniRedBrush"] as SolidColorBrush : new SolidColorBrush(Colors.Black);
            EnableOrDisableFindRoutesButton();
        }

        private void EndTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            textBox.Foreground = textBox.Text == "Current Location" ? Application.Current.Resources["MobileMuniRedBrush"] as SolidColorBrush : new SolidColorBrush(Colors.Black);
            EnableOrDisableFindRoutesButton();
        }

        private void EnableOrDisableFindRoutesButton()
        {
            FindRoutesButtonEnabled = StartTextBox.Text != null && StartTextBox.Text.Length > 3 && EndTextBox.Text != null && EndTextBox.Text.Length > 3;
            if (FindRoutesButtonEnabled)
            {
                FindRoutesButton.Foreground = Application.Current.Resources["MobileMuniRedBrush"] as SolidColorBrush;
                FindRoutesButton.BorderBrush = Application.Current.Resources["MobileMuniRedBrush"] as SolidColorBrush;
            }
            else
            {
                FindRoutesButton.Foreground = new SolidColorBrush(Colors.Gray);
                FindRoutesButton.BorderBrush = new SolidColorBrush(Colors.Gray);
            }
        }
    }
}
