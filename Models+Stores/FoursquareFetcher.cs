

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Xml;
namespace MobileMuni
{
    public static class FoursquareFetcher
    {
        public class Result
        {
            public string Name { get; set; }
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public string Address { get; set; }
            public string IconFilename { get; set; }
        }

        public const string API_KEY =  "CSF0GAEIXL2MSZIHTASSTYED2LF24TAUT2V44ZXGKJLVHITJ";

        public delegate void FoursquareResultsFetched(List<Result> results);
        public static FoursquareResultsFetched ResultsFetched;

        private static string CurrentQuery;

        public static void Fetch(string locationName)
        {
            WebClient webClient = new WebClient();
            CurrentQuery = locationName;
            string request = "https://api.foursquare.com/v2/venues/search?near=" + Uri.EscapeDataString(Settings.FoursquareCityName) + "&query=" + Uri.EscapeDataString(CurrentQuery)
                + "&oauth_token=" + API_KEY + "&v=20121008";
            Debug.WriteLine("Foursquare request: " + request);
            webClient.DownloadStringAsync(new Uri(request, UriKind.Absolute));
            webClient.DownloadStringCompleted += webClient_DownloadStringCompleted;
        }

        static void webClient_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            try
            {
                JObject jsonObject = JObject.Parse(e.Result);
                var venues = jsonObject["response"]["venues"];
                var results = new List<Result>();                
                foreach (var venueJson in venues)
                {
                    var result = new Result();
                    result.Latitude = venueJson["location"]["lat"].Value<float>();
                    result.Longitude = venueJson["location"]["lng"].Value<float>();
                    result.Name = venueJson["name"].Value<string>();
                    if (venueJson["location"]["address"] != null)
                    {
                        result.Address = venueJson["location"]["address"].Value<string>();
                    }
                    else
                    {
                        if (venueJson["location"]["city"] != null)
                        {
                            result.Address = venueJson["location"]["city"].Value<string>();
                        }
                    }

                    // figure out what the foursquare icon should be
                    try
                    {
                        var iconString = venueJson["categories"][0]["icon"]["prefix"].Value<string>();
                        var filename = Regex.Replace(iconString, "(?m).*categories_v2(.*)", "$1");
                        filename = Regex.Replace(filename, "(?m)/", "_");
                        filename = Regex.Replace(filename, "(?m)_$", ".png");
                        result.IconFilename = "/Images/FoursquareIcons/fs" + filename;
                    }
                    catch { }

                    results.Add(result);
                }
                if (results.Count == 0)
                {
                    MessageBox.Show("Sorry, we couldn't find any results for '" + CurrentQuery + "'. Please tap back and try a diffent search :)");
                    return;
                }
                if (ResultsFetched != null)
                {
                    ResultsFetched(results);
                }
            }
            catch 
            {
                MessageBox.Show("There was a problem getting Foursquare results. Please tap back and try again :)");
            }
        }
    }
}