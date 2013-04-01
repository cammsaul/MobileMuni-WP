using System;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.IsolatedStorage;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Diagnostics;

namespace MobileMuni
{
    public static class FavoriteStore
    {
        public static ObservableCollection<Favorite> Favorites = new ObservableCollection<Favorite>();

        public static Favorite Favorite(Stop stop, Route route, Route.Direction direction)
        {
            if (stop == null || route == null || direction == Route.Direction.None)
            {
                return null;
            }

            var favorites = from favorite in Favorites where favorite.StopTag.Equals(stop.Tag) && favorite.RouteTag.Equals(route.Tag) && favorite.Direction.Equals(direction) select favorite;
            if (favorites.Count() == 0)
            {
                return null;
            }
            return favorites.First();
        }

        public static void Save()
        {
            using (var store = IsolatedStorageFile.GetUserStoreForApplication())
            using (var file = store.CreateFile("FavoriteStore.json"))
            {
                // create the serializer for the class
                var serializer = new DataContractJsonSerializer(typeof(ObservableCollection<Favorite>));

                // save the object as json
                serializer.WriteObject(file, Favorites);
            }

            Debug.WriteLine("FavoriteStore saved.");
        }

        public static bool Restore()
        {
            using (var store = IsolatedStorageFile.GetUserStoreForApplication()) 
            {
                if (store.FileExists("FavoriteStore.json")) 
                {
                    using (var file = store.OpenFile("FavoriteStore.json", FileMode.Open))
                    {
                        // create the serializer
                        var serializer = new DataContractJsonSerializer(typeof(ObservableCollection<Favorite>));

                        // load the object from JSON
                        Favorites = (ObservableCollection<Favorite>)serializer.ReadObject(file);

                        Debug.WriteLine("FavoriteStore restored.");

                        return true;
                    }
                }
            }
            return false;
        }
    }
}
