using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace MobileMuni.ViewModels_Views
{
    public partial class BookmarksPage : PhoneApplicationPage
    {
        private bool FieldIsStartField = false; // what field are we looking up a bookmark for? either start field (true) or end field (false);

        public BookmarksPage()
        {
            InitializeComponent();

            ListBox.ItemsSource = null; // forces update this way (?)
            ListBox.ItemsSource = BookmarkStore.Bookmarks;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            string field = NavigationContext.QueryString["field"];
            FieldIsStartField = field == "start";
        }

        private void TextBlock_Tap_1(object sender, System.Windows.Input.GestureEventArgs e)
        {
            string selectedBookmark = (sender as TextBlock).Text;
            if (FieldIsStartField)
            {
                BookmarkStore.SetStartBookmark(selectedBookmark);
            }
            else
            {
                BookmarkStore.SetEndBookmark(selectedBookmark);
            }
            NavigationService.GoBack();
        }
    }
}