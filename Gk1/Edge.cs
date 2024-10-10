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
        FixedLength
    }
    public class Edge
    {
        public Vertex Start {  get; set; }
        public Vertex End { get; set; }
        public EdgeConstraint Constraint { get; set; }
        public float? FixedLength { get; set; } // optional for fixed edge

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

    }
}
