using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gk1
{
    public enum EdgeConstraint
    {
        None,
        Horizontal,
        Vertical,
        FixedLength,
        Bezier,
        HalfCircle
    }
    public class Edge
    {
        public Vertex Start {  get; set; }
        public Vertex End { get; set; }
        public EdgeConstraint Constraint { get; set; }
        public float? FixedLength { get; set; } // optional for fixed edge
        public Vertex ControlPoint1 { get; set; }     
        public Vertex ControlPoint2 { get; set; }
        public ContinuityType StartContinuity { get; set; }
        public ContinuityType EndContinuity { get; set; }


        public Edge(Vertex start, Vertex end)
        {
            Start = start;
            End = end;
            Constraint = EdgeConstraint.None;
            FixedLength = null;
        }
        public double Length()
        {
            return Vertex.Distance(Start, End);
        }
        public bool IsHorizontal()
        {
            return Constraint == EdgeConstraint.Horizontal;
        }
        public bool IsVertical()
        {
            return Constraint == EdgeConstraint.Vertical;
        }
        public Point MidPoint()
        {
            return new Point((Start.X + End.X)/2, (Start.Y + End.Y)/2);
        }


        public bool IsPointNearEdge(Point clickPoint,int closeDistance)
        {
            double edgeLength = Math.Sqrt(Math.Pow(End.X - Start.X, 2) + Math.Pow(End.Y - Start.Y, 2));
            if (edgeLength == 0) return false;

            double edgeX = End.X - Start.X;
            double edgeY = End.Y - Start.Y;

            double normalX = -edgeY;
            double normalY = edgeX;

            double normalLength = Math.Sqrt(normalX * normalX + normalY * normalY);
            normalX /= normalLength;
            normalY /= normalLength;

            double t = (clickPoint.X - Start.X) * edgeX + (clickPoint.Y - Start.Y) * edgeY;
            t /= edgeLength * edgeLength;

            Point closestPoint = new Point((int)(Start.X + t * edgeX), (int)(Start.Y + t * edgeY));

            double distanceToEdge = Math.Sqrt(Math.Pow(clickPoint.X - closestPoint.X, 2) +
                                               Math.Pow(clickPoint.Y - closestPoint.Y, 2));

            return distanceToEdge < closeDistance;
        }

        public bool SetConstraint(EdgeConstraint newConstraint, Edge previousEdge, Edge nextEdge)
        {
            if (newConstraint == EdgeConstraint.Horizontal && (previousEdge?.IsHorizontal() == true || nextEdge?.IsHorizontal() == true))
            {
                return false; // Nie możemy ustawić, jeśli sąsiednia krawędź też jest pozioma
            }
            if (newConstraint == EdgeConstraint.Vertical && (previousEdge?.IsVertical() == true || nextEdge?.IsVertical() == true))
            {
                return false; // Nie możemy ustawić, jeśli sąsiednia krawędź też jest pionowa
            }
            Constraint = newConstraint;
            return true;
        }
        public void RemoveConstraint()
        {
            Constraint = EdgeConstraint.None;
            FixedLength = null;
        }
        public void ScaleToFixedLength(float newlength,bool isStart)
        {
            double currentLength = Length();
            if (currentLength == 0) return;
            
            double scale = newlength / currentLength;
            if (isStart)
            {
                Start.X = (int)(End.X - (End.X - Start.X) * scale);
                Start.Y = (int)(End.Y - (End.Y - Start.Y) * scale);
            }
            else
            {
                End.X = (int)(Start.X + (End.X - Start.X) * scale);
                End.Y = (int)(Start.Y + (End.Y - Start.Y) * scale);
            }
        }

        public void Draw(Graphics g)
        {
            // Ustawienie koloru w zależności od typu ograniczenia
            Color color = Color.Black; // Domyślny kolor
            switch (Constraint)
            {
                case EdgeConstraint.Horizontal:
                    color = Color.Brown; // Kolor dla poziomej
                    break;
                case EdgeConstraint.Vertical:
                    color = Color.Green; // Kolor dla pionowej
                    break;
                case EdgeConstraint.FixedLength:
                    color = Color.Red; // Kolor dla stałej długości
                    break;
                case EdgeConstraint.HalfCircle:
                    DrawHalfCircle(g);
                    return;
                case EdgeConstraint.Bezier:
                    DrawBezier(g); // Rysowanie krzywej Béziera
                    return; // Zakończ metodę, gdy rysujemy Béziera
                
            }

            // Rysowanie linii dla krawędzi
            using (Pen pen = new Pen(color, 2)) // Grubość linii 2
            {
                //g.DrawLine(pen, Start.ToPoint(), End.ToPoint());
                DrawBresenhamLine(g, Start, End, color);
            }

            // Jeśli krawędź ma punkty kontrolne, narysuj je
            if (Constraint == EdgeConstraint.Bezier)
            {
                g.FillEllipse(Brushes.Blue, ControlPoint1.X - 3, ControlPoint1.Y - 3, 6, 6); // Punkt kontrolny 1
                g.FillEllipse(Brushes.Blue, ControlPoint2.X - 3, ControlPoint2.Y - 3, 6, 6); // Punkt kontrolny 2
            }
        }

        private void DrawHalfCircle(Graphics g)
        {
            Point midpoint = MidPoint();
            double radius = Vertex.Distance(Start, End) / 2;

            double startAngle = Math.Atan2(End.Y - Start.Y, End.X - Start.X);                
            double endAngle = Math.Atan2(Start.Y - End.Y, Start.X - End.X);

            int segments = 100;
            for (int i = 0; i <= segments; i++)
            {
                double t = i / (double)segments;
                double currentAngle = startAngle + t * (endAngle - startAngle);

                int x1 = (int)(midpoint.X + radius * Math.Cos(startAngle + (t - 1.0 / segments) * (endAngle - startAngle)));
                int y1 = (int)(midpoint.Y + radius * Math.Sin(startAngle + (t - 1.0 / segments) * (endAngle - startAngle)));
                int x2 = midpoint.X + (int)(radius * Math.Cos(currentAngle));
                int y2 = midpoint.Y + (int)(radius * Math.Sin(currentAngle));

                Vertex prevPoint = new Vertex(x1, y1);
                Vertex currentPoint = new Vertex(x2, y2);

                DrawBresenhamLine(g, prevPoint, currentPoint, Color.Purple);
            }
        }



        private void DrawBezier(Graphics g)
        {
            // Pobranie punktów kontrolnych P1, P2, P3, P4
            Vertex P1 = Start;
            Vertex P2 = ControlPoint1;
            Vertex P3 = ControlPoint2;
            Vertex P4 = End;

            // Obliczenie współczynników A0, A1, A2, A3
            Vertex A0 = P1;
            Vertex A1 = new Vertex(3 * (P2.X - P1.X), 3 * (P2.Y - P1.Y));
            Vertex A2 = new Vertex(3 * (P3.X - 2 * P2.X + P1.X), 3 * (P3.Y - 2 * P2.Y + P1.Y));
            Vertex A3 = new Vertex(P4.X - 3 * P3.X + 3 * P2.X - P1.X, P4.Y - 3 * P3.Y + 3 * P2.Y - P1.Y);

            // Liczba punktów do narysowania na krzywej
            int segments = 100;
            float step = 1.0f / segments;

            Vertex prevPoint = P1; // Zaczynamy od P1

            for (int i = 1; i <= segments; i++)
            {
                // Obliczenie wartości t w zakresie od 0 do 1
                float t = i * step;

                // Obliczenie współrzędnych punktu na krzywej Béziera dla danego t
                float t2 = t * t;
                float t3 = t2 * t;

                float x = A3.X * t3 + A2.X * t2 + A1.X * t + A0.X;
                float y = A3.Y * t3 + A2.Y * t2 + A1.Y * t + A0.Y;

                Vertex currentPoint = new Vertex((int)x, (int)y);

                // Rysowanie linii pomiędzy poprzednim a bieżącym punktem
                DrawBresenhamLine(g, prevPoint, currentPoint, Color.Blue);

                // Ustawienie bieżącego punktu jako poprzedni
                prevPoint = currentPoint;
            }
        }


        public void DrawBresenhamLine(Graphics g, Vertex a, Vertex b,Color color)
        {
            int x1 = a.X;
            int x2 = b.X;
            int y1 = a.Y;
            int y2 = b.Y;

            int dx = Math.Abs(x2 - x1);
            int dy = Math.Abs(y2 - y1);
            int sx = x1 < x2 ? 1 : -1;
            int sy = y1 < y2 ? 1 : -1;
            int err = dx - dy;

            while(true)
            {
                g.FillRectangle(new SolidBrush(color),x1,y1,1,1);
                if (x1 == x2 && y1 == y2) break;
                int e = 2 * err;
                if(e> -dy)
                {
                    err -= dy;
                    x1 += sx;
                }
                if(e < dx)
                {
                    err += dx;
                    y1 += sy;
                }
            }
        }
    }
}
