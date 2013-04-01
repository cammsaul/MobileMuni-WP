using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace MobileMuni
{
    public partial class RouteOverview : UserControl, IEntranceExitAnimation
    {
        public Storyboard EntranceAnimation { get { return EntranceAnim; } } 

        public RouteOverview()
        {
            InitializeComponent();

            StateMachine.StateChanged += new StateMachine.StateChangedDelegate(StateMachine_StateChanged);
            StateMachine_StateChanged(StateMachine.CurrentState); // call once to setup UI
        }

        public void StateMachine_StateChanged(StateMachine.State newState)
        {
            if (newState.Route != null)
            {
                DataContext = null; // basically this will "force" a reload -- if direction changes but not the route itself we need the UI to reflect that
                DataContext = newState.Route; 

                SwitchDirectionButton.Content = newState.Direction == Route.Direction.Inbound ? "switch to outbound" : "switch to inbound";
            }
        }

        public void SwitchDirectionButton_Click(object sender, EventArgs e)
        {
            Route.Direction newDirection = StateMachine.CurrentState.Direction == Route.Direction.Inbound ? Route.Direction.Outbound : Route.Direction.Inbound;
            StateMachine.PushState(StateMachine.CurrentState.WithDirection(newDirection));
        }
    }
}
