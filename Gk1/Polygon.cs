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
            UpdateEdges();
        }

        public void RemoveVertexAt(int index)
        {
            if(index >= 0 && index < Vertices.Count)
            {
                Vertices.RemoveAt(index);
                UpdateEdges();
                if(Vertices.Count < 3)
                {
                    Vertices.Clear();
                    Edges.Clear();
                    isclosed = false;
                }
            }
        }

        public void UpdateEdges()
        {
            List<Edge> OldEdges = Edges.ToList();
            Edges.Clear();
            for (int i = 0; i < Vertices.Count - 1; i++)
            {
                Edges.Add(new Edge(Vertices[i], Vertices[i + 1]));
                if (isclosed)
                {
                    Edges[i].Constraint = OldEdges[i].Constraint;
                    Edges[i].FixedLength = OldEdges[i].FixedLength;
                }
                 
            }

            if (isclosed && Vertices.Count > 2)
            {
                // Zamykamy wielokąt (dodajemy krawędź między ostatnim i pierwszym wierzchołkiem)
                Edges.Add(new Edge(Vertices[Vertices.Count - 1], Vertices[0]));
            }

           
        }

        public bool SetEdgeConstraint(int edgeIndex, EdgeConstraint constraint)
        {
            if (edgeIndex < 0 || edgeIndex >= Edges.Count) return false;

            Edge previousEdge = edgeIndex > 0 ? Edges[edgeIndex - 1] : null;
            Edge nextEdge = edgeIndex < Edges.Count - 1 ? Edges[edgeIndex + 1] : null;

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
                UpdateEdges();
            }
        }
    }
}
