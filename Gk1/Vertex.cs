using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gk1
{
    public enum ContinuityType
    {
        None,   // Brak ciągłości
        G0,     // Ciągłość geometryczna G0 (ciągłość punktów)
        G1,     // Ciągłość geometryczna G1 (ciągłość jednostkowego wektora stycznego)
        C1      // Ciągłość geometryczna C1 (ciągłość wektora stycznego)
    }
    public class Vertex
    {
        public int X {  get; set; }
        public int Y { get; set; }
        public ContinuityType Continuity { get; set; } = ContinuityType.None;

        public Vertex(int x, int y) 
        {
            X = x;
            Y = y;
        }

        public Point ToPoint()
        {
            return new Point(X, Y);
        }

        public static double Distance(Vertex a, Vertex b)
        {
            return Math.Sqrt(Math.Pow(b.X - a.X, 2) + Math.Pow(b.Y - a.Y, 2));
        }
    }
}
