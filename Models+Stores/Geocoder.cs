using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace MobileMuni
{
    class Geocoder
    {
        public delegate void GeocodingFinishedDelegate(FoursquareFetcher.Result result);
        public static GeocodingFinishedDelegate GeocodingFinished;

        public static void GeocodeAddress(string address)
        {
            WebClient webClient = new WebClient();
            var addressWithCity = address + ", " + Settings.FoursquareCityName;
            string request = String.Format("http://maps.googleapis.com/maps/api/geocode/json?address={0}&sensor=true", Uri.EscapeDataString(addressWithCity));
            Debug.WriteLine("Google Maps Geocoding request: " + request);
            webClient.DownloadStringAsync(new Uri(request, UriKind.Absolute));
            webClient.DownloadStringCompleted += webClient_DownloadStringCompleted;
        }

        static void webClient_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            try
            {
                JObject jsonObject = JObject.Parse(e.Result);
                var jsonResult = jsonObject["results"][0];
                var location = jsonResult["geometry"]["location"];
                var lat = location["lat"].Value<double>();
                var lon = location["lng"].Value<double>();
                var address = jsonResult["formatted_address"].Value<string>();
                var addressFirstLine = Regex.Replace(address, "(?m)^([^,]+),.*$", "$1");
                Debug.WriteLine("Geocoder results: {0}, {1}", lat, lon);
                if (GeocodingFinished != null)
                {
                    var result = new FoursquareFetcher.Result()
                    {
                        Latitude = lat,
                        Longitude = lon,
                        Name = addressFirstLine,
                        Address = address,
                        IconFilename = "/Images/appbar.nearby.png"
                    };
                    GeocodingFinished(result);
                }
            }
            catch {
                GeocodingFinished(null);
            }

        }
    }
}
