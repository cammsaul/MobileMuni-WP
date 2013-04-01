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
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Runtime.Serialization.Json;
using System.IO;

namespace MobileMuni
{
    public static class RouteStore
    {
        public static List<Route> Routes = new List<Route>();

        public static Route RouteForTag(String routeTag)
        {
            if (routeTag == null)
            {
                return null;
            }

            foreach (Route route in Routes) {
                if (route.Tag.Equals(routeTag))
                {
                    return route;
                }
            }
            return null;
        }

        public static void Save()
        {
            using (var store = IsolatedStorageFile.GetUserStoreForApplication())
            using (var file = store.CreateFile("RouteStore.json"))
            {
                // create the serializer for the class
                var serializer = new DataContractJsonSerializer(typeof(List<Route>));

                // save the object as json
                serializer.WriteObject(file, Routes);
            }
        }

        public static bool Restore()
        {
            using (var store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (store.FileExists("RouteStore.json"))
                {
                    using (var file = store.OpenFile("RouteStore.json", FileMode.Open))
                    {
                        // create the serializer
                        var serializer = new DataContractJsonSerializer(typeof(List<Route>));

                        // load the object from JSON
                        Routes = (List<Route>)serializer.ReadObject(file);

                        return true;
                    }
                }
            }
            return false;
        }
    }
}
