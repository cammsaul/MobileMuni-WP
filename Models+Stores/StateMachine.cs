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
using System.Device.Location;
using System.Diagnostics;

namespace MobileMuni
{

    public static class StateMachine
    {
        private static List<State> States = new List<State>();

        public static bool IsBaseState { get { return States.Count == 1 && CurrentState.Layout == StateMachine.State.LayoutState.None; } } 

        public delegate void StateChangedDelegate(State newState);
        public static StateChangedDelegate StateChanged;

        public static GeoCoordinate DEFAULT_LOCATION = new GeoCoordinate(37.7793, -122.4192);

        private static GeoCoordinate CurrentLocation_;
        public static GeoCoordinate CurrentLocation
        {
            get
            {
                return (CurrentLocation_ == null) ? DEFAULT_LOCATION : CurrentLocation_;
            }
            set
            {
                CurrentLocation_ = value;
            }
        }

        public static State CurrentState
        {
            get
            {
                if (States.Count == 0)
                {
                    return null;
                }

                return StateMachine.States[States.Count - 1];
            }
        }

        public static State PreviousState { get; private set; }

        public static int StateCount
        {
            get
            {
                return States.Count;
            }
        }

        public static bool RouteChanged()
        {
            return PreviousState == null || CurrentState.Route != PreviousState.Route;
        }

        public static bool StopsChanged()
        {
            return PreviousState == null || CurrentState.Stop != PreviousState.Stop;
        }

        public static bool DirectionChanged()
        {
            return PreviousState == null || CurrentState.Direction != PreviousState.Direction;
        }

        public static bool LayoutChanged()
        {
            return PreviousState == null || CurrentState.Layout != PreviousState.Layout;
        }

        public static State PopState()
        {
            if (States.Count == 1) {
                Debug.WriteLine("Currently on first state, nothing to pop!");
                return CurrentState;
            }

            PreviousState = CurrentState;
            States.RemoveAt(States.Count - 1);
            if (StateChanged != null)
            {
                Debug.WriteLine("Popped to State {0}: {1}", (States.Count - 1), CurrentState);
                StateChanged(CurrentState);
            }
            return CurrentState;
        }

        public static State PushState(State state)
        {
            if (CurrentState == null || state.Layout != CurrentState.Layout || state.Route != CurrentState.Route || state.Stop != CurrentState.Stop || state.Direction != CurrentState.Direction)
            {
                PreviousState = CurrentState;
                States.Add(state);
                if (StateChanged != null)
                {
                    if (IsBaseState)
                    {
                        Debug.WriteLine("Pushed BASE STATE {0}: {1}", (States.Count - 1), CurrentState);
                    }
                    else
                    {
                        StateChanged(CurrentState);
                        Debug.WriteLine("Pushed State {0}: {1}", (States.Count - 1), CurrentState);
                    }
                }
            }
            return state;
        }

        public class State
        {
            public enum LayoutState
            {
                None,
                Favorites,
                RoutePicker,
                RouteOverview,
                StopOverview,
                NearbyStops,
                RouteFinder
            }

            public Route Route { get; private set; }
            public Route.Direction Direction { get; private set; }
            public Stop Stop { get; private set; }
            public Stop EndStop { get; set; }
            public LayoutState Layout { get; private set; }

            public State()
            {
                this.Direction = Route.Direction.None;
                this.Layout = LayoutState.None;
            }

            public State(Route route, Route.Direction direction, Stop stop, LayoutState topView)
            {
                this.Route = route;
                this.Direction = direction;
                this.Stop = stop;
                this.Layout = topView;
            }

            public State WithRoute(Route route)
            {
                return new State(route, this.Direction, this.Stop, this.Layout);
            }

            public State WithDirection(Route.Direction routeDirection)
            {
                return new State(this.Route, routeDirection, this.Stop, this.Layout);
            }

            public State WithStop(Stop stop)
            {
                return new State(this.Route, this.Direction, stop, this.Layout);
            }

            public State WithLayout(LayoutState layout)
            {
                return new State(this.Route, this.Direction, this.Stop, layout);
            }

            public override string ToString()
            {
                string routeChanged = RouteChanged() ? "*" : "";
                string stopChanged = StopsChanged() ? "*" : "";
                string layoutChanged = LayoutChanged() ? "*" : "";
                string directionChanged = DirectionChanged() ? "*" : "";
                return String.Format("Route{0}: {1}, Direction{2}: {3}, Stop{4}: {5}, Layout{6}: {7}", routeChanged, Route != null ? Route.Tag : "none", directionChanged, Direction, stopChanged,
                    Stop != null ? Stop.Title : "none", layoutChanged, Layout);
            }
        }
    }
}
