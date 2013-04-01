using System;
using System.Collections.Generic;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Xml.Linq;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace MobileMuni
{
    public class Route : IComparable<Route>
    {
        public enum Direction
        {
            Outbound,
            Inbound,
            None
        };

        public string Tag { get; set; }
        public string Title { get; set; }

        public string OutboundTitle { get; set; }
        public string InboundTitle { get; set; }
        public string CurrentDirectionTitle
        {
            get
            {
                switch (StateMachine.CurrentState.Direction)
                {
                    case Route.Direction.Inbound: return "Inbound";
                    case Route.Direction.Outbound: return "Outbound";
                    default: return null;
                }
            }
        }

        public List<string> OutboundStopTags { get; set; }
        public List<string> InboundStopTags { get; set; }

        // list vehicles

        public float LatMin { get; set; }
        public float LatMax { get; set; }
        public float LonMin { get; set; }
        public float LonMax { get; set; }

        public Route(XElement xmlElement)
        {
            this.Tag = (string)xmlElement.Attribute("tag");
            this.Title = (string)xmlElement.Attribute("title");

            this.LatMin = (float)xmlElement.Attribute("latMin");
            this.LatMax = (float)xmlElement.Attribute("latMax");
            this.LonMin = (float)xmlElement.Attribute("lonMin");
            this.LonMax = (float)xmlElement.Attribute("lonMax");

            this.OutboundStopTags = new List<string>();
            this.InboundStopTags = new List<string>();
        }

        public Route() { }

        public int CompareTo(Route other)
        {

            // try to grab just the numbers using regex matching :) 
            Match myMatch = System.Text.RegularExpressions.Regex.Match(this.Tag, "([0-9]+)");
            String otherMatch = System.Text.RegularExpressions.Regex.Match(other.Tag, "([0-9]+)").ToString();
            
            int myInt = -1;
            int otherInt = -1;

            if (myMatch != null)
            {
                try
                {
                    myInt = Convert.ToInt16(myMatch.ToString());
                }
                catch {}
            }
            if (otherMatch != null)
            {
                try
                {
                    otherInt = Convert.ToInt16(otherMatch.ToString());
                }
                catch {}
            }
            if (myInt != -1 && otherInt != -1)
            {
                if (myInt == otherInt)
                {
                    return this.Tag.CompareTo(other.Tag);
                } else
                {
                    return myInt.CompareTo(otherInt);
                }
            }
            if (myInt != -1)
            {
                return 1;
            }
            if (otherInt != -1)
            {
                return -1;
            }
            return this.Tag.CompareTo(other.Tag);
        }
    }
}
