using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Device.Location;

namespace MobileMuni
{
    public static class Settings
    {
        public static GeoCoordinate DefaultLocation     = new GeoCoordinate(37.7793, -122.4192);
        public static string[]      DefaultBookmarks    = {"Current Location", "Union Square", "Fisherman's Wharf", "Coit Tower", "Delores Park", "Financial District", "Powell St. BART Station", "Montgomery BART Station", "San Francisco Zoo", "AT&T Park", "UCSF Parnassas", "Stonestown Galleria"};
        public static string        FoursquareCityName  = "San Francisco, CA, US";
        public static string        AdMobID             = "a150ecac8c68530";
        public static string        MapCredentials      = "Anrc1E2dHh1G0KXEM4f8cK_KwKx5E7_OYSY8bc7xsimCcreH52nc5mTXXO-Ihz8w";
        public static string        NextBusCityCode     = "sf-muni";
        public static string        FlurryKey           = "SJR7HKW2VCQAWVKN88A5";
    }
}
