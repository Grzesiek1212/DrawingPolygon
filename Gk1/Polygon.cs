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
            int poprzedni = index - 1;
            if (poprzedni < 0) poprzedni = Edges.Count - 1;
            Edges[poprzedni].RemoveConstraint();
            Edges[index].RemoveConstraint();

            Vertices.RemoveAt(index);
            UpdateEdges(index);

            if(Vertices.Count < 3)
            {
                Vertices.Clear();
                Edges.Clear();
                isclosed = false;
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
                    Edges[i].StartContinuity = OldEdges[i].StartContinuity;
                    Edges[i].EndContinuity = OldEdges[i].EndContinuity;
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
                        Edges[i].StartContinuity = OldEdges[i].StartContinuity;
                        Edges[i].EndContinuity = OldEdges[i].EndContinuity;
                    }
                    else if (i == index - 1) continue;
                    else
                    {
                        Edges[i].Constraint = OldEdges[j].Constraint;
                        Edges[i].FixedLength = OldEdges[j].FixedLength;
                        Edges[i].ControlPoint1 = OldEdges[j].ControlPoint1;
                        Edges[i].ControlPoint2 = OldEdges[j].ControlPoint2;
                        Edges[i].StartContinuity = OldEdges[j].StartContinuity;
                        Edges[i].EndContinuity = OldEdges[j].EndContinuity;
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


        public bool IsPointInsidePolygon(Point point)
        {
            // Sprawdzenie, czy wielokąt jest zamknięty
            if (!isclosed) return false;

            // Liczba wierzchołków wielokąta
            int vertexCount = Vertices.Count;

            // Zmienna do przechowywania liczby przecięć
            int intersections = 0;

            // Przechodzimy przez każdą krawędź wielokąta
            for (int i = 0, j = vertexCount - 1; i < vertexCount; j = i++)
            {
                Point vertex1 = new Point(Vertices[i].X, Vertices[i].Y); // Bieżący wierzchołek
                Point vertex2 = new Point(Vertices[j].X, Vertices[j].Y); // Poprzedni wierzchołek (zamykający krawędź)

                // Sprawdzamy, czy promień przecina krawędź
                if ((vertex1.Y > point.Y) != (vertex2.Y > point.Y)) // Jeden wierzchołek nad punktem, drugi pod nim
                {
                    // Obliczamy punkt przecięcia promienia z krawędzią
                    double intersectionX = vertex1.X + (point.Y - vertex1.Y) * (vertex2.X - vertex1.X) / (vertex2.Y - vertex1.Y);

                    // Sprawdzamy, czy punkt przecięcia znajduje się po prawej stronie punktu
                    if (intersectionX > point.X)
                    {
                        // Zliczamy przecięcie
                        intersections++;
                    }
                }
            }

            // Jeśli liczba przecięć jest nieparzysta, punkt jest wewnątrz
            return (intersections % 2 != 0);
        }
        public bool IsVertexPartOfBezierEdge(int vertexIndex)
        {
            foreach (var edge in Edges)
            {
                if (edge.Constraint == EdgeConstraint.Bezier)
                {
                    if (edge.Start == Vertices[vertexIndex] || edge.End == Vertices[vertexIndex])
                    {
                        return true; // Wierzchołek jest częścią krzywej Béziera
                    }
                }
            }
            return false;
        }

        
        // funkcje do ustawiania relacji na wierchołku
        public void ApplyContinuityConstraints(int vertexIndex, Point newLocation)
        {
            var vertex = Vertices[vertexIndex];
            var incomingEdge = GetIncomingEdge(vertexIndex);
            var outgoingEdge = GetOutgoingEdge(vertexIndex);
            if (incomingEdge == null || outgoingEdge == null) return;

            if (vertex.Continuity == ContinuityType.G1)
            {
                // Przesuwamy wierzchołek tak, aby zachować styczność jednostkową
                PreserveG1Continuity(incomingEdge, outgoingEdge, newLocation);
            }
            else if (vertex.Continuity == ContinuityType.C1)
            {
                // Przesuwamy wierzchołek tak, aby zachować ciągłość wektorową
                PreserveC1Continuity(incomingEdge, outgoingEdge, newLocation);
            }

            vertex.X = newLocation.X;
            vertex.Y = newLocation.Y;
        }
        private void PreserveG1Continuity(Edge incomingEdge, Edge outgoingEdge, Point newLocation)
        {
            // Przypisz nową lokalizację do wierzchołka, gdzie styka się incomingEdge i outgoingEdge
            incomingEdge.End = ToVertex(newLocation);
            outgoingEdge.Start = ToVertex(newLocation);

            // Oblicz wektor styczny dla incomingEdge
            Point incomingTangent;

            if (incomingEdge.Constraint == EdgeConstraint.Bezier)
            {
                // Jeśli incomingEdge jest krzywą Béziera, oblicz wektor styczny z ostatniego punktu kontrolnego
                Point p1 = incomingEdge.End.ToPoint(); // Punkt końcowy incomingEdge
                Point p2 = incomingEdge.ControlPoint2.ToPoint(); // Ostatni punkt kontrolny incomingEdge
                incomingTangent = new Point(p1.X - p2.X, p1.Y - p2.Y); // Wektor styczny
            }
            else
            {
                // Jeśli incomingEdge jest prostą, oblicz wektor styczny bezpośrednio z wierzchołków
                Point p1 = incomingEdge.Start.ToPoint(); // Punkt początkowy incomingEdge
                Point p2 = incomingEdge.End.ToPoint(); // Punkt końcowy incomingEdge
                incomingTangent = new Point(p2.X - p1.X, p2.Y - p1.Y); // Wektor styczny
            }

            // Oblicz nowy punkt kontrolny dla outgoingEdge, jeśli jest Bézierem
            if (outgoingEdge.Constraint == EdgeConstraint.Bezier)
            {
                Point p3 = outgoingEdge.Start.ToPoint(); // Początek outgoingEdge (który jest newLocation)

                // Długość wektora stycznego outgoingEdge powinna odpowiadać incomingEdge
                outgoingEdge.ControlPoint1 = new Vertex(p3.X + incomingTangent.X, p3.Y + incomingTangent.Y);
            }
            else
            {
                // Jeśli outgoingEdge jest prostą, upewnij się, że kierunek outgoingEdge jest zgodny z incomingEdge
                Point p3 = outgoingEdge.End.ToPoint(); // Koniec outgoingEdge

                // Upewnij się, że wektor outgoingEdge ma ten sam kierunek co incomingTangent
                Point outgoingTangent = new Point(p3.X - newLocation.X, p3.Y - newLocation.Y);

                // Skaluje outgoingTangent do długości incomingTangent
                double lengthRatio = CalculateVectorLength(incomingTangent) / CalculateVectorLength(outgoingTangent);
                outgoingEdge.End = new Vertex((int)(newLocation.X + outgoingTangent.X * lengthRatio), (int)(newLocation.Y + outgoingTangent.Y * lengthRatio));
            }
        }
        private void PreserveC1Continuity(Edge incomingEdge, Edge outgoingEdge, Point newLocation)
        {
            // Aktualizujemy początek outgoingEdge na nową lokalizację
            outgoingEdge.Start = ToVertex(newLocation);

            // Sprawdzenie, czy incomingEdge jest krzywą Béziera
            if (incomingEdge.Constraint == EdgeConstraint.Bezier)
            {
                if (outgoingEdge.Constraint == EdgeConstraint.Bezier)
                {
                    // Obie krawędzie są krzywymi Béziera: P4 - P3 = P2 - P1
                    Point p1 = incomingEdge.End.ToPoint();  // Punkt końcowy incomingEdge (P1)
                    Point p2 = incomingEdge.ControlPoint2.ToPoint();  // Ostatni punkt kontrolny incomingEdge (P2)
                    Point p3 = outgoingEdge.ControlPoint1.ToPoint();  // Pierwszy punkt kontrolny outgoingEdge (P3)
                    Point p4 = outgoingEdge.Start.ToPoint();  // Początek outgoingEdge (P4)

                    // Oblicz różnicę P2 - P1
                    Point delta = new Point(p2.X - p1.X, p2.Y - p1.Y);

                    // Aktualizujemy punkt kontrolny outgoingEdge (P3)
                    Point p = new Point(p4.X - delta.X, p4.Y - delta.Y);
                    outgoingEdge.ControlPoint1 = ToVertex(p);
                }
                else
                {
                    // incomingEdge jest krzywą Béziera, a outgoingEdge jest linią prostą
                    Point a = outgoingEdge.Start.ToPoint();  // Początek outgoingEdge (A)
                    Point b = outgoingEdge.End.ToPoint();  // Koniec outgoingEdge (B)
                    Point p1 = incomingEdge.End.ToPoint();  // Punkt końcowy incomingEdge (P1)
                    Point p2 = incomingEdge.ControlPoint2.ToPoint();  // Ostatni punkt kontrolny incomingEdge (P2)

                    // Obliczamy 1/3 odcinka (B - A)
                    Point delta = new Point((b.X - a.X) / 3, (b.Y - a.Y) / 3);

                    // Aktualizujemy punkt kontrolny incomingEdge (P2)
                    Point p = new Point(p1.X + delta.X, p1.Y + delta.Y);
                    incomingEdge.ControlPoint2 = ToVertex(p1);
                }
            }
            else if (outgoingEdge.Constraint == EdgeConstraint.Bezier)
            {
                // incomingEdge jest linią prostą, a outgoingEdge jest krzywą Béziera
                Point a = incomingEdge.Start.ToPoint();  // Początek incomingEdge (A)
                Point b = incomingEdge.End.ToPoint();  // Koniec incomingEdge (B)
                Point p1 = outgoingEdge.Start.ToPoint();  // Początek outgoingEdge (P1)
                Point p2 = outgoingEdge.ControlPoint1.ToPoint();  // Pierwszy punkt kontrolny outgoingEdge (P2)

                // Obliczamy 1/3 odcinka (B - A)
                Point delta = new Point((b.X - a.X) / 3, (b.Y - a.Y) / 3);

                // Aktualizujemy punkt kontrolny outgoingEdge (P2)
                Point p = new Point(p1.X + delta.X, p1.Y + delta.Y);
                outgoingEdge.ControlPoint1 = ToVertex(p);
            }
        }
        public Edge? GetIncomingEdge(int vertexIndex)
        {
            for (int i = 0; i < Edges.Count; i++)
            {
                if (Edges[i].End == Vertices[vertexIndex])
                {
                    return Edges[i];
                }
            }
            return null;
        }
        public Edge? GetOutgoingEdge(int vertexIndex)
        {
            for (int i = 0; i < Edges.Count; i++)
            {
                if (Edges[i].Start == Vertices[vertexIndex])
                {
                    return Edges[i];
                }
            }
            return null;
        }
        public Vertex ToVertex(Point p)
        {
            return new Vertex(p.X, p.Y);
        }
        private double CalculateVectorLength(Point p)
        {
            return Math.Sqrt(p.X * p.X + p.Y * p.Y);
        }
    }
}
