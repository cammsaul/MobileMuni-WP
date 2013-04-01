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
    public partial class RoutePicker : UserControl, IEntranceExitAnimation
    {
        public Storyboard EntranceAnimation { get { return EntranceAnim; } } 

        public delegate void RouteSelectedDelegate(Route route);
        public RouteSelectedDelegate RouteSelected;

        public RoutePicker()
        {
            InitializeComponent();

            this.SetupRouteButtons();
        }

        private void SetupRouteButtons()
        {
            int routesCount = RouteStore.Routes.Count;

            Brush mobileMuniRedBrush = (SolidColorBrush)(Application.Current.Resources["MobileMuniRedBrush"]);
            
            // every 5 routes needs to be a new row
            for (int i = 0; i < routesCount; i += 4)
            {
                StackPanel horizontalStackPanel = new StackPanel()
                {
                    Height = 80,
                    Orientation = Orientation.Horizontal
                };
                VerticalStackPanel.Children.Add(horizontalStackPanel);

                // add the button for each route
                for (int button = i; button < i + 4 && button < routesCount; button++) {
                    Route route = RouteStore.Routes[button];
                    Button routeButton = new Button()
                    {
                        Content = route.Tag,
                        Foreground = mobileMuniRedBrush,
                        BorderBrush = mobileMuniRedBrush,
                        Height = 72,
                        Margin = new Thickness(0, 5, 0, 0),
                        Width = 120,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        FontSize = 21,
                        VerticalAlignment = VerticalAlignment.Top
                    };
                    routeButton.Tag = route;
                    routeButton.Click += new RoutedEventHandler(routeButton_Click);
                    horizontalStackPanel.Children.Add(routeButton);
                }
            }

            // add one extra empty stack panel so it is possible to scroll all the way to the bottom without it being covered by the app bar
            StackPanel emptyStackPanel = new StackPanel()
            {
                Height = 80
            };
            VerticalStackPanel.Children.Add(emptyStackPanel);
        }

        void routeButton_Click(object sender, RoutedEventArgs e)
        {
            Route route = (sender as Button).Tag as Route;            
            RouteSelected(route);
        }
    }
}
