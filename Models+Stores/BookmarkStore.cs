using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Runtime.Serialization.Json;

namespace MobileMuni
{
    public static class BookmarkStore
    {
        private const string BOOKMARK_STORE_KEY = "BookmarkStore";

        public static List<string> Bookmarks = new List<string>();

        public delegate void StartBookmarkSelectedDelegate(string startBookmark);
        public static StartBookmarkSelectedDelegate StartBookmarkChanged;
        public delegate void EndBookmarkSelectedDelegate(string endBookmark);
        public static EndBookmarkSelectedDelegate EndBookmarkChanged;

        public static void SetStartBookmark(string startBookmark)
        {
            if (StartBookmarkChanged != null)
            {
                StartBookmarkChanged(startBookmark);
            }
        }

        public static void SetEndBookmark(string endBookmark)
        {
            if (EndBookmarkChanged != null)
            {
                EndBookmarkChanged(endBookmark);
            }
        }

        public static void AddBookmark(string bookmark)
        {
            Bookmarks.Remove(bookmark); // remove if it already exists; we'll bring it to the front.
            Bookmarks.Remove("Current Location");
            Bookmarks.Insert(0, bookmark);
            Bookmarks.Insert(0, "Current Location"); // current location should always be first

            if (Bookmarks.Count > 100)
            {
                Bookmarks.RemoveRange(100, Bookmarks.Count - 100); // don't keep bookmarks older than 100
            }

            Save();
        }

        public static void Save()
        {
            string[] array = new string[Bookmarks.Count];
            int i = 0;
            foreach (string bookmark in Bookmarks) 
            {
                array[i] = bookmark;
                i++;
            }

            using (var store = IsolatedStorageFile.GetUserStoreForApplication())
            using (var file = store.CreateFile("BookmarkStore.json"))
            {
                // create the serializer for the class
                var serializer = new DataContractJsonSerializer(typeof(string[]));

                // save the object as json
                serializer.WriteObject(file, array);
            }
        }

        public static bool Restore()
        {
            using (var store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (store.FileExists("BookmarkStore.json"))
                {
                    using (var file = store.OpenFile("BookmarkStore.json", System.IO.FileMode.Open))
                    {
                        // create the serializer 
                        var serializer = new DataContractJsonSerializer(typeof(string[]));

                        // load the object from the JSON
                        string[] array = (string[])serializer.ReadObject(file);
                        Bookmarks = new List<string>();

                        foreach (string bookmark in array)
                        {
                            Bookmarks.Add(bookmark);
                        }
                        return true;
                    }                    
                }
            }

            // just create some defaults for the bookmarks 
            Bookmarks = new List<string>(Settings.DefaultBookmarks);

            return false;
        }
    }
}