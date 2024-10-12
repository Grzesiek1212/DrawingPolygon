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
        Bezier
    }
    public class Edge
    {
        public Vertex Start {  get; set; }
        public Vertex End { get; set; }
        public EdgeConstraint Constraint { get; set; }
        public float? FixedLength { get; set; } // optional for fixed edge
        public Vertex ControlPoint1 { get; set; }     
        public Vertex ControlPoint2 { get; set; }
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

        public void ApplyConstraint()
        {
            if(Constraint == EdgeConstraint.None) return;
            if(Constraint == EdgeConstraint.Horizontal)
            {
                End.Y = Start.Y;
            }
            if(Constraint == EdgeConstraint.Vertical)
            {
                Start.X = End.X;
            }
            if(Constraint == EdgeConstraint.FixedLength && FixedLength.HasValue)
            {
                ScaleToFixedLength(FixedLength.Value);
            }
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
        private void ScaleToFixedLength(float newlength)
        {
            double currentLength = Length();
            if (currentLength == 0) return;
            
            double scale = newlength / currentLength;
            End.X = (int)(Start.X + (End.X - Start.X) * scale);
            End.Y = (int)(Start.Y + (End.Y - Start.Y) * scale);
        }

        public void Draw(Graphics g)
        {
            // Ustawienie koloru w zależności od typu ograniczenia
            Color color = Color.Black; // Domyślny kolor
            switch (Constraint)
            {
                case EdgeConstraint.Horizontal:
                    color = Color.Blue; // Kolor dla poziomej
                    break;
                case EdgeConstraint.Vertical:
                    color = Color.Green; // Kolor dla pionowej
                    break;
                case EdgeConstraint.FixedLength:
                    color = Color.Red; // Kolor dla stałej długości
                    break;
                case EdgeConstraint.Bezier:
                    DrawBezier(g); // Rysowanie krzywej Béziera
                    return; // Zakończ metodę, gdy rysujemy Béziera
            }

            // Rysowanie linii dla krawędzi
            using (Pen pen = new Pen(color, 2)) // Grubość linii 2
            {
                g.DrawLine(pen, Start.ToPoint(), End.ToPoint());
            }

            // Jeśli krawędź ma punkty kontrolne, narysuj je
            if (Constraint == EdgeConstraint.Bezier)
            {
                g.FillEllipse(Brushes.Blue, ControlPoint1.X - 3, ControlPoint1.Y - 3, 6, 6); // Punkt kontrolny 1
                g.FillEllipse(Brushes.Blue, ControlPoint2.X - 3, ControlPoint2.Y - 3, 6, 6); // Punkt kontrolny 2
            }
        }

        private void DrawBezier(Graphics g)
        {
            // Rysowanie krzywej Béziera
            using (Pen pen = new Pen(Color.Blue, 2))
            {
                // Używamy krzywej Béziera z punktów kontrolnych
                Point[] bezierPoints = new Point[]
                {
                    Start.ToPoint(),
                    ControlPoint1.ToPoint(),
                    ControlPoint2.ToPoint(),
                    End.ToPoint()
                };
                g.DrawBezier(pen, Start.ToPoint(), ControlPoint1.ToPoint(), ControlPoint2.ToPoint(), End.ToPoint());
            }
        }

    }
}
