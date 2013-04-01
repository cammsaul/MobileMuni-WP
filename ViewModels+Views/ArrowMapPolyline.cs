using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Phone.Controls.Maps.Core;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows;
using Microsoft.Phone.Controls.Maps;

namespace MobileMuni
{
    public class ArrowMapPolyline : MapPolyline
    {
        public ArrowMapPolyline()
            : base()
        {
            this.Stroke = (SolidColorBrush)(Application.Current.Resources["MobileMuniRedBrush"]);
            this.Opacity = 0.65;
        }

        const double ANGLE = Math.PI / 6;

        public double ArrowLength { get; set; }

        public new FillRule FillRule
        {
            get
            {
                return (FillRule)base.EmbeddedShape.GetValue(Polygon.FillRuleProperty);
            }
            set
            {
                base.EmbeddedShape.SetValue(Polygon.FillRuleProperty, value);
            }
        }

        protected override PointCollection ProjectedPoints
        {
            get
            {
                return ((Polyline)base.EmbeddedShape).Points;
            }
            set
            {
                if (value.Count > 1)
                {
                    Point[] verticiesArray = new Point[value.Count()];
                    value.CopyTo(verticiesArray, 0);

                    value.Clear();

                    //Point previousPoint = verticiesArray[verticiesArray.Count() - 1];
                    //for (int i = verticiesArray.Count() - 2; i >= 0; i--)
                    Point start = verticiesArray[0];
                    for (int i = 1; i < verticiesArray.Count(); i++)
                    {
                        Point end = verticiesArray[i];
                        double slopeAngle = (start.X > end.X ? 0 : 1) * Math.PI + Math.Atan(-1 * (start.Y - end.Y) / (start.X - end.X));
                        if ((start.X - end.X) == 0.0)
                            slopeAngle = Math.PI * (start.Y - end.Y > 0 ? -0.5 : 0.5);
                        Point mid = CalculateMidpoint(start, end);

                        Point arrow1 = CalulatePoint(mid, slopeAngle + ANGLE);
                        Point arrow2 = CalulatePoint(mid, slopeAngle - ANGLE);
                        value.Add(start);
                        value.Add(mid);
                        value.Add(arrow2);
                        value.Add(arrow1);
                        value.Add(mid);
                        value.Add(end);

                        start = end;
                    }
                }
                ((Polyline)base.EmbeddedShape).Points = value;
            }
        }
        private Point CalulatePoint(Point originalPoint, double angle)
        {
            return new Point(
                  originalPoint.X + ArrowLength * Math.Cos(angle),
                  originalPoint.Y - ArrowLength * Math.Sin(angle));
        }

        private Point CalculateMidpoint(Point firstPoint, Point secondPoint)
        {
            // try to center the arrow in the middle of the line -- move back half of ARROW_LEN towards the firstPoint.
            double shiftAmount = ArrowLength / 2;
            //double shiftX = firstPoint.X < secondPoint.X ? -shiftAmount : shiftAmount; // if first point is less than second move backwards, otherwise move up
            //double shiftY = firstPoint.Y < secondPoint.Y ? -shiftAmount : shiftAmount;
            return new Point(
                ((firstPoint.X + secondPoint.X) / 2)/* + shiftX*/,
                ((firstPoint.Y + secondPoint.Y) / 2)/* + shiftY*/);
        }
    }
}
