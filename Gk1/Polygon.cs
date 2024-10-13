using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gk1
{
    public class Polygon
    {
        public List<Vertex> Vertices { get; set; }
        public List<Edge> Edges { get; set;}

        public bool isclosed;

        public Polygon() 
        {
            Vertices = new List<Vertex>();
            Edges = new List<Edge>();
            isclosed = false;
        }

        public void AddVertex(Vertex v)
        {
            Vertices.Add(v);
            UpdateEdges(-1);
        }

        public void RemoveVertexAt(int index)
        {
            if(index >= 0 && index < Vertices.Count)
            {
                Vertices.RemoveAt(index);
                UpdateEdges(index);
                if(Vertices.Count < 3)
                {
                    Vertices.Clear();
                    Edges.Clear();
                    isclosed = false;
                }
            }
        }

        public void UpdateEdges(int index)
        {
            List<Edge> OldEdges = Edges.ToList();
            Edges.Clear();
            for (int i = 0; i < Vertices.Count - 1; i++)
            {
                Edges.Add(new Edge(Vertices[i], Vertices[i + 1]));                      
            }
            if (isclosed && Vertices.Count > 2)
            {
                // Zamykamy wielokąt (dodajemy krawędź między ostatnim i pierwszym wierzchołkiem)
                Edges.Add(new Edge(Vertices[Vertices.Count - 1], Vertices[0]));
            }

            if(OldEdges.Count  == Edges.Count) // tu jeżeli przesuneliśmy cały wielokąt
            {
                for(int i = 0;i < OldEdges.Count;i++)
                {
                    Edges[i].Constraint = OldEdges[i].Constraint;
                    Edges[i].FixedLength = OldEdges[i].FixedLength;
                    Edges[i].ControlPoint1 = OldEdges[i].ControlPoint1;
                    Edges[i].ControlPoint2 = OldEdges[i].ControlPoint2;
                }
            }
            else if(OldEdges.Count -1 == Edges.Count)
            {
                int j = index + 1;
                for(int i = 0;i < Edges.Count;i++) 
                {
                    if (i < index - 1)
                    {
                        Edges[i].Constraint = OldEdges[i].Constraint;
                        Edges[i].FixedLength = OldEdges[i].FixedLength;
                        Edges[i].ControlPoint1 = OldEdges[i].ControlPoint1;
                        Edges[i].ControlPoint2 = OldEdges[i].ControlPoint2;
                    }
                    else if (i == index - 1) continue;
                    else
                    {
                        Edges[i].Constraint = OldEdges[j].Constraint;
                        Edges[i].FixedLength = OldEdges[j].FixedLength;
                        Edges[i].ControlPoint1 = OldEdges[i].ControlPoint1;
                        Edges[i].ControlPoint2 = OldEdges[i].ControlPoint2;
                        j++;
                    }
                }
               
                
            }
           
        }

        public bool SetEdgeConstraint(int edgeIndex, EdgeConstraint constraint)
        {
            if (edgeIndex < 0 || edgeIndex >= Edges.Count) return false;

            Edge previousEdge = edgeIndex > 0 ? Edges[edgeIndex - 1] : Edges[Edges.Count-1];
            Edge nextEdge = edgeIndex < Edges.Count - 1 ? Edges[edgeIndex + 1] : Edges[0];

            return Edges[edgeIndex].SetConstraint(constraint, previousEdge, nextEdge);
        }

        // Usuwanie ograniczenia z wybranej krawędzi
        public void RemoveEdgeConstraint(int edgeIndex)
        {
            if (edgeIndex >= 0 && edgeIndex < Edges.Count)
            {
                Edges[edgeIndex].RemoveConstraint();
            }
        }
        public void ApplyConstraints()
        {
            foreach (var edge in Edges)
            {
                edge.ApplyConstraint();
            }
        }
        public void ClosePolygon()
        {
            if (Vertices.Count > 2)
            {
                isclosed = true;
                UpdateEdges(-1);
            }
        }
    }
}
