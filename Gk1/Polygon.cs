using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.VisualStyles;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

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
                    copyRelationBeetweenEdges(Edges[i], OldEdges[i]);
                }
            }
            else if(OldEdges.Count -1 == Edges.Count)
            {
                int j = index + 1;
                for(int i = 0;i < Edges.Count;i++) 
                {
                    if (i < index - 1)
                    {
                        copyRelationBeetweenEdges(Edges[i], OldEdges[i]);
                    }
                    else if (i == index - 1) continue;
                    else
                    {
                        copyRelationBeetweenEdges(Edges[i], OldEdges[j]);
                        j++;
                    }
                }
               
                
            }
           
        }
        private void copyRelationBeetweenEdges(Edge edgenew, Edge edgeold)
        {
            edgenew.Constraint = edgeold.Constraint;
            edgenew.FixedLength = edgeold.FixedLength;
            edgenew.ControlPoint1 = edgeold.ControlPoint1;
            edgenew.ControlPoint2 = edgeold.ControlPoint2;
            edgenew.StartContinuity = edgeold.StartContinuity;
            edgenew.EndContinuity = edgeold.EndContinuity;
        }
        public bool SetEdgeConstraint(int edgeIndex, EdgeConstraint constraint)
        {
            if (edgeIndex < 0 || edgeIndex >= Edges.Count) return false;

            Edge previousEdge = edgeIndex > 0 ? Edges[edgeIndex - 1] : Edges[Edges.Count-1];
            Edge nextEdge = edgeIndex < Edges.Count - 1 ? Edges[edgeIndex + 1] : Edges[0];

            return Edges[edgeIndex].SetConstraint(constraint, previousEdge, nextEdge);
        }
        public void RemoveEdgeConstraint(int edgeIndex)
        {
            if (edgeIndex >= 0 && edgeIndex < Edges.Count)
            {
                Edges[edgeIndex].RemoveConstraint();
            }
        }
        public void ApplyConstraints(int index)
        {
            bool[] edgevisited = new bool[Edges.Count];
            ApplyConstraint(Edges[index], false);
            edgevisited[index] = true;
            int i = Countposition(index-1);
            int j = Countposition(index + 1);
            while (edgevisited[i]==false || edgevisited[j] == false)
            {
                if (edgevisited[i] == false)
                {
                    edgevisited[i] = true;
                    ApplyConstraint(Edges[i], true);
                }
                if (edgevisited[j] == false) 
                {
                    edgevisited[j] = true;
                    ApplyConstraint(Edges[j], false);
                }
                i = Countposition(i - 1);
                j = Countposition(j + 1);
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
            if (incomingEdge == null && outgoingEdge == null) return;
            if(incomingEdge == null && outgoingEdge != null) incomingEdge = Edges[Countposition(Edges.IndexOf(outgoingEdge)-1)];
            if (incomingEdge != null && outgoingEdge == null) outgoingEdge = Edges[Countposition(Edges.IndexOf(incomingEdge) + 1)];

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
            if (incomingEdge.Constraint == EdgeConstraint.Bezier && outgoingEdge.Constraint != EdgeConstraint.Bezier)
            {
                Point P3 = incomingEdge.ControlPoint2.ToPoint();
                Point P4 = incomingEdge.End.ToPoint();
                Point A = outgoingEdge.End.ToPoint();

                // Odległość między P3 a P4
                double distance = Vertex.Distance(incomingEdge.ControlPoint2, incomingEdge.End);

                // Sprawdzenie, czy punkty P4 i A mają tę samą współrzędną X (pionowa linia)
                if (A.X == P4.X)
                {
                    // W tym przypadku prosta jest pionowa, więc nowy punkt przesuwamy tylko wzdłuż osi Y
                    if (A.Y > P4.Y)
                    {
                        incomingEdge.ControlPoint2.X = P4.X; // współrzędna X się nie zmienia
                        incomingEdge.ControlPoint2.Y = (int)(P4.Y - distance); // przesuwamy w dół
                    }
                    else
                    {
                        incomingEdge.ControlPoint2.X = P4.X;
                        incomingEdge.ControlPoint2.Y = (int)(P4.Y + distance); // przesuwamy w górę
                    }
                }
                // Sprawdzenie, czy punkty P4 i A mają tę samą współrzędną Y (pozioma linia)
                else if (A.Y == P4.Y)
                {
                    // W tym przypadku prosta jest pozioma, więc nowy punkt przesuwamy tylko wzdłuż osi X
                    if (A.X > P4.X)
                    {
                        incomingEdge.ControlPoint2.X = (int)(P4.X - distance); // przesuwamy w lewo
                        incomingEdge.ControlPoint2.Y = P4.Y; // współrzędna Y się nie zmienia
                    }
                    else
                    {
                        incomingEdge.ControlPoint2.X = (int)(P4.X + distance); // przesuwamy w prawo
                        incomingEdge.ControlPoint2.Y = P4.Y;
                    }
                }
                else
                {
                    // Wyznaczenie współczynników a i b prostej y = ax + b przechodzącej przez punkty P4 i A
                    double a = (A.Y - P4.Y) / (A.X - P4.X);
                    double b = P4.Y - a * P4.X;

                    // Wektor kierunkowy między P4 i A
                    double directionX = A.X - P4.X;
                    double directionY = A.Y - P4.Y;

                    // Normalizacja wektora (wektor jednostkowy)
                    double length = Math.Sqrt(directionX * directionX + directionY * directionY);
                    double unitDirectionX = directionX / length;
                    double unitDirectionY = directionY / length;

                    // Znalezienie punktu po przeciwnej stronie niż A, w odległości "distance" od P4
                    // Wektory po przeciwnej stronie, zmieniamy znaki
                    double newX = P4.X - unitDirectionX * distance;
                    double newY = P4.Y - unitDirectionY * distance;

                    // Ustawienie nowej lokalizacji dla newLocation (który może być np. nowym punktem kontrolnym)
                    incomingEdge.ControlPoint2.X = (int)newX;
                    incomingEdge.ControlPoint2.Y = (int)newY;
                }
            }

            else if (incomingEdge.Constraint != EdgeConstraint.Bezier && outgoingEdge.Constraint == EdgeConstraint.Bezier)
            {
                Point P3 = outgoingEdge.ControlPoint1.ToPoint();
                Point P4 = incomingEdge.End.ToPoint();
                Point A = incomingEdge.Start.ToPoint();

                // Odległość między P3 a P4
                double distance = Vertex.Distance(incomingEdge.End, outgoingEdge.ControlPoint1);

                // Sprawdzenie, czy punkty P4 i A mają tę samą współrzędną X (pionowa linia)
                if (A.X == P4.X)
                {
                    // W tym przypadku prosta jest pionowa, więc nowy punkt przesuwamy tylko wzdłuż osi Y
                    if (A.Y > P4.Y)
                    {
                        outgoingEdge.ControlPoint1.X = P4.X; // współrzędna X się nie zmienia
                        outgoingEdge.ControlPoint1.Y = (int)(P4.Y - distance); // przesuwamy w dół
                    }
                    else
                    {
                        outgoingEdge.ControlPoint1.X = P4.X;
                        outgoingEdge.ControlPoint1.Y = (int)(P4.Y + distance); // przesuwamy w górę
                    }
                }
                // Sprawdzenie, czy punkty P4 i A mają tę samą współrzędną Y (pozioma linia)
                else if (A.Y == P4.Y)
                {
                    // W tym przypadku prosta jest pozioma, więc nowy punkt przesuwamy tylko wzdłuż osi X
                    if (A.X > P4.X)
                    {
                        outgoingEdge.ControlPoint1.X = (int)(P4.X - distance); // przesuwamy w lewo
                        outgoingEdge.ControlPoint1.Y = P4.Y; // współrzędna Y się nie zmienia
                    }
                    else
                    {
                        outgoingEdge.ControlPoint1.X = (int)(P4.X + distance); // przesuwamy w prawo
                        outgoingEdge.ControlPoint1.Y = P4.Y;
                    }
                }
                else
                {
                    // Wyznaczenie współczynników a i b prostej y = ax + b przechodzącej przez punkty P4 i A
                    double a = (A.Y - P4.Y) / (A.X - P4.X);
                    double b = P4.Y - a * P4.X;

                    // Wektor kierunkowy między P4 i A
                    double directionX = A.X - P4.X;
                    double directionY = A.Y - P4.Y;

                    // Normalizacja wektora (wektor jednostkowy)
                    double length = Math.Sqrt(directionX * directionX + directionY * directionY);
                    double unitDirectionX = directionX / length;
                    double unitDirectionY = directionY / length;

                    // Znalezienie punktu po przeciwnej stronie niż A, w odległości "distance" od P4
                    // Wektory po przeciwnej stronie, zmieniamy znaki
                    double newX = P4.X - unitDirectionX * distance;
                    double newY = P4.Y - unitDirectionY * distance;

                    // Ustawienie nowej lokalizacji dla newLocation (który może być np. nowym punktem kontrolnym)
                    outgoingEdge.ControlPoint1.X = (int)newX;
                    outgoingEdge.ControlPoint1.Y = (int)newY;
                }
            }

            else if (incomingEdge.Constraint == EdgeConstraint.Bezier && outgoingEdge.Constraint == EdgeConstraint.Bezier)
            {
                Point P3 = incomingEdge.ControlPoint2.ToPoint();
                Point P4 = incomingEdge.End.ToPoint();
                Point A = outgoingEdge.ControlPoint1.ToPoint();

                double distance = Vertex.Distance(incomingEdge.ControlPoint2, incomingEdge.End);

                if (A.X == P4.X)
                {
                    if (A.Y > P4.Y)
                    {
                        incomingEdge.ControlPoint2.X = P4.X;
                        incomingEdge.ControlPoint2.Y = (int)(P4.Y - distance);
                    }
                    else
                    {
                        incomingEdge.ControlPoint2.X = P4.X;
                        incomingEdge.ControlPoint2.Y = (int)(P4.Y + distance);
                    }
                }
                else if (A.Y == P4.Y)
                {
                    if (A.X > P4.X)
                    {
                        incomingEdge.ControlPoint2.X = (int)(P4.X - distance);
                        incomingEdge.ControlPoint2.Y = P4.Y;
                    }
                    else
                    {
                        incomingEdge.ControlPoint2.X = (int)(P4.X + distance);
                        incomingEdge.ControlPoint2.Y = P4.Y;
                    }
                }
                else
                {
                    double a = (A.Y - P4.Y) / (A.X - P4.X);
                    double b = P4.Y - a * P4.X;

                    double directionX = A.X - P4.X;
                    double directionY = A.Y - P4.Y;

                    double length = Math.Sqrt(directionX * directionX + directionY * directionY);
                    double unitDirectionX = directionX / length;
                    double unitDirectionY = directionY / length;

                    double newX = P4.X - unitDirectionX * distance;
                    double newY = P4.Y - unitDirectionY * distance;

                    incomingEdge.ControlPoint2.X = (int)newX;
                    incomingEdge.ControlPoint2.Y = (int)newY;
                }
            }
        }
        private void PreserveC1Continuity(Edge incomingEdge, Edge outgoingEdge, Point newLocation)
        {
            // Aktualizujemy początek outgoingEdge na nową lokalizację
            outgoingEdge.Start = ToVertex(newLocation);
            outgoingEdge.Start.Continuity = ContinuityType.C1;

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
                    Point p = new Point(p1.X - delta.X, p1.Y - delta.Y);
                    incomingEdge.ControlPoint2 = ToVertex(p);
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
        public void ApplyConstraint(Edge edge,bool isStart)
        {
            if (edge.Constraint == EdgeConstraint.None) return;
            if (edge.Constraint == EdgeConstraint.Horizontal)
            {
                _ = isStart ? edge.Start.Y = edge.End.Y : edge.End.Y = edge.Start.Y;
            }
            if (edge.Constraint == EdgeConstraint.Vertical)
            {
                _ = isStart ? edge.Start.X = edge.End.X : edge.End.X = edge.Start.X;
            }
            if (edge.Constraint == EdgeConstraint.FixedLength && edge.FixedLength.HasValue)
            {
                edge.ScaleToFixedLength(edge.FixedLength.Value, isStart);
            }
            if (edge.Constraint == EdgeConstraint.Bezier)
            {
                int index = Edges.IndexOf(edge);
                if (!isStart) ApplyContinuityConstraints(index, Edges[Countposition(index-1)].End.ToPoint());
                else ApplyContinuityConstraints(Countposition(index+1), Edges[Countposition(index + 1)].Start.ToPoint());
            }
        }
        public int Countposition(int index)
        {
            if(index >= Edges.Count) return index%Edges.Count;
            if(index < 0) return (index + Edges.Count)%Edges.Count;
            return index;
        }
        public void UpdateEdgesControlPoint(Edge draggedBezierEdge,int ind)
        {
            int index = 0;
            while (Edges[index] != draggedBezierEdge) { index++; }

            if(ind == 1 && draggedBezierEdge.Start.Continuity != ContinuityType.G1 && draggedBezierEdge.Start.Continuity != ContinuityType.C1)
            {
                ApplyConstraints(index);
                return;
            }
            if (ind == 2 && draggedBezierEdge.End.Continuity != ContinuityType.G1 && draggedBezierEdge.End.Continuity != ContinuityType.C1)
            {
                ApplyConstraints(index);
                return;
            }
            bool[] edgevisited = new bool[Edges.Count];
            edgevisited[index] = true;
            int i = Countposition(index - 1);
            int j = Countposition(index + 1);

            if (ind == 1)
            {
                upgradebycontrol(Edges[i], draggedBezierEdge);
                edgevisited[i] = true;
            }
            else
            {
                upgradebycontrol(Edges[j], draggedBezierEdge);
                edgevisited[j] = true;
            }

            
            while (edgevisited[i] == false || edgevisited[j] == false)
            {
                if (edgevisited[i] == false)
                {
                    edgevisited[i] = true;
                    ApplyConstraint(Edges[i], true);
                }
                if (edgevisited[j] == false)
                {
                    edgevisited[j] = true;
                    ApplyConstraint(Edges[j], false);
                }
                i = Countposition(i - 1);
                j = Countposition(j + 1); 
            }
        }
        private void upgradebycontrol(Edge edge, Edge edgeWithControl)
        {
            Point controlpoint,point,opposedpoint;
            ContinuityType remberpoint, remberopposedpoint;

            if (edge.End.X == edgeWithControl.Start.X && edge.End.Y == edgeWithControl.Start.Y)
            {
                controlpoint = edgeWithControl.ControlPoint1.ToPoint();
                point = edge.End.ToPoint();
                remberpoint = edge.End.Continuity;
                opposedpoint = edge.Start.ToPoint();
                remberopposedpoint = edge.Start.Continuity;
                
            }
            else
            {
                controlpoint = edgeWithControl.ControlPoint2.ToPoint();
                point = edge.Start.ToPoint();
                remberpoint = edge.Start.Continuity;
                opposedpoint = edge.End.ToPoint();
                remberopposedpoint = edge.End.Continuity;
            }

            if(edge.Constraint == EdgeConstraint.None)
            {
                Point P3 = opposedpoint;
                Point P4 = point;
                Point A = controlpoint;

                // Odległość między P3 a P4
                
                double distance = (double)Vertex.Distance(ToVertex(P3),ToVertex(P4));
                if (remberpoint == ContinuityType.C1)
                {
                    distance = 3 * (double)Vertex.Distance(ToVertex(A), ToVertex(P4));
                }

                    // Sprawdzenie, czy punkty P4 i A mają tę samą współrzędną X (pionowa linia)
                    if (A.X == P4.X)
                {
                    // W tym przypadku prosta jest pionowa, więc nowy punkt przesuwamy tylko wzdłuż osi Y
                    if (A.Y > P4.Y)
                    {
                        opposedpoint.X = P4.X; // współrzędna X się nie zmienia
                        opposedpoint.Y = (int)(P4.Y - distance); // przesuwamy w dół
                    }
                    else
                    {
                        opposedpoint.X = P4.X;
                        opposedpoint.Y = (int)(P4.Y + distance); // przesuwamy w górę
                    }
                }
                // Sprawdzenie, czy punkty P4 i A mają tę samą współrzędną Y (pozioma linia)
                else if (A.Y == P4.Y)
                {
                    // W tym przypadku prosta jest pozioma, więc nowy punkt przesuwamy tylko wzdłuż osi X
                    if (A.X > P4.X)
                    {
                        opposedpoint.X = (int)(P4.X - distance); // przesuwamy w lewo
                        opposedpoint.Y = P4.Y; // współrzędna Y się nie zmienia
                    }
                    else
                    {
                        opposedpoint.X = (int)(P4.X + distance); // przesuwamy w prawo
                        opposedpoint.Y = P4.Y;
                    }
                }
                else
                {
                    // Wyznaczenie współczynników a i b prostej y = ax + b przechodzącej przez punkty P4 i A
                    double a = (A.Y - P4.Y) / (A.X - P4.X);
                    double b = P4.Y - a * P4.X;

                    // Wektor kierunkowy między P4 i A
                    double directionX = A.X - P4.X;
                    double directionY = A.Y - P4.Y;

                    // Normalizacja wektora (wektor jednostkowy)
                    double length = Math.Sqrt(directionX * directionX + directionY * directionY);
                    double unitDirectionX = directionX / length;
                    double unitDirectionY = directionY / length;

                    // Znalezienie punktu po przeciwnej stronie niż A, w odległości "distance" od P4
                    double newX = P4.X - unitDirectionX * distance;
                    double newY = P4.Y - unitDirectionY * distance;

                    // Ustawienie nowej lokalizacji dla opposedpoint
                    opposedpoint.X = (int)newX;
                    opposedpoint.Y = (int)newY;

                   
                }
            }

            if (edge.Constraint == EdgeConstraint.Horizontal)
            {
                controlpoint.Y = point.Y; 
            }
            if (edge.Constraint == EdgeConstraint.Vertical)
            {
                controlpoint.X = point.X;
            }
            if (edge.Constraint == EdgeConstraint.FixedLength)
            {
                Point P3 = opposedpoint;
                Point P4 = point;
                Point A = controlpoint;

                // Odległość między P3 a P4
                double distance = (double)edge.FixedLength;

                // Sprawdzenie, czy punkty P4 i A mają tę samą współrzędną X (pionowa linia)
                if (A.X == P4.X)
                {
                    // W tym przypadku prosta jest pionowa, więc nowy punkt przesuwamy tylko wzdłuż osi Y
                    if (A.Y > P4.Y)
                    {
                        opposedpoint.X = P4.X; // współrzędna X się nie zmienia
                        opposedpoint.Y = (int)(P4.Y - distance); // przesuwamy w dół
                    }
                    else
                    {
                        opposedpoint.X = P4.X;
                        opposedpoint.Y = (int)(P4.Y + distance); // przesuwamy w górę
                    }
                }
                // Sprawdzenie, czy punkty P4 i A mają tę samą współrzędną Y (pozioma linia)
                else if (A.Y == P4.Y)
                {
                    // W tym przypadku prosta jest pozioma, więc nowy punkt przesuwamy tylko wzdłuż osi X
                    if (A.X > P4.X)
                    {
                        opposedpoint.X = (int)(P4.X - distance); // przesuwamy w lewo
                        opposedpoint.Y = P4.Y; // współrzędna Y się nie zmienia
                    }
                    else
                    {
                        opposedpoint.X = (int)(P4.X + distance); // przesuwamy w prawo
                        opposedpoint.Y = P4.Y;
                    }
                }
                else
                {
                    // Wyznaczenie współczynników a i b prostej y = ax + b przechodzącej przez punkty P4 i A
                    double a = (A.Y - P4.Y) / (A.X - P4.X);
                    double b = P4.Y - a * P4.X;

                    // Wektor kierunkowy między P4 i A
                    double directionX = A.X - P4.X;
                    double directionY = A.Y - P4.Y;

                    // Normalizacja wektora (wektor jednostkowy)
                    double length = Math.Sqrt(directionX * directionX + directionY * directionY);
                    double unitDirectionX = directionX / length;
                    double unitDirectionY = directionY / length;

                    // Znalezienie punktu po przeciwnej stronie niż A, w odległości "distance" od P4
                    double newX = P4.X - unitDirectionX * distance;
                    double newY = P4.Y - unitDirectionY * distance;

                    // Ustawienie nowej lokalizacji dla opposedpoint
                    opposedpoint.X = (int)newX;
                    opposedpoint.Y = (int)newY;

                    if (remberpoint == ContinuityType.C1)
                    {
                        // Wektor kierunkowy między P4 (point) i opposedpoint
                        double directionX_C1 = opposedpoint.X - P4.X;
                        double directionY_C1 = opposedpoint.Y - P4.Y;

                        // Normalizacja wektora (wektor jednostkowy)
                        double length_C1 = Math.Sqrt(directionX_C1 * directionX_C1 + directionY_C1 * directionY_C1);
                        double unitDirectionX_C1 = directionX_C1 / length_C1;
                        double unitDirectionY_C1 = directionY_C1 / length_C1;

                        // Odległość między P4 a controlpoint musi być 1/3 długości distance
                        double controlDistance = distance / 3.0;

                        // Nowe współrzędne controlPoint (wzdłuż tego samego wektora kierunkowego)
                        double controlX = P4.X - unitDirectionX_C1 * controlDistance;
                        double controlY = P4.Y - unitDirectionY_C1 * controlDistance;

                        // Ustawienie nowej lokalizacji dla controlPoint
                        controlpoint.X = (int)controlX;
                        controlpoint.Y = (int)controlY;
                    }
                }
            }

            if (edge.Constraint == EdgeConstraint.Bezier)
            {
                // Wyznaczenie opposedpoint, w zależności od położenia kontrolnych punktów krzywej
                _ = edge.End == edgeWithControl.Start ? opposedpoint = edge.ControlPoint2.ToPoint() : opposedpoint = edge.ControlPoint1.ToPoint();

                Point P3 = opposedpoint;
                Point P4 = point;
                Point A = controlpoint;

                // Odległość między P3 a P4
                double distance = Vertex.Distance(ToVertex(P3), ToVertex(P4));

                // Sprawdzenie, czy punkty P4 i A mają tę samą współrzędną X (pionowa linia)
                if (A.X == P4.X)
                {
                    // Prosta pionowa, przesunięcie wzdłuż osi Y
                    if (A.Y > P4.Y)
                    {
                        opposedpoint.X = P4.X; // współrzędna X się nie zmienia
                        opposedpoint.Y = (int)(P4.Y - distance); // przesuwamy w dół
                    }
                    else
                    {
                        opposedpoint.X = P4.X;
                        opposedpoint.Y = (int)(P4.Y + distance); // przesuwamy w górę
                    }
                }
                // Sprawdzenie, czy punkty P4 i A mają tę samą współrzędną Y (pozioma linia)
                else if (A.Y == P4.Y)
                {
                    // Prosta pozioma, przesunięcie wzdłuż osi X
                    if (A.X > P4.X)
                    {
                        opposedpoint.X = (int)(P4.X - distance); // przesuwamy w lewo
                        opposedpoint.Y = P4.Y; // współrzędna Y się nie zmienia
                    }
                    else
                    {
                        opposedpoint.X = (int)(P4.X + distance); // przesuwamy w prawo
                        opposedpoint.Y = P4.Y;
                    }
                }
                else
                {
                    // Wyznaczenie współczynników a i b prostej y = ax + b przechodzącej przez punkty P4 i A
                    double a = (A.Y - P4.Y) / (A.X - P4.X);
                    double b = P4.Y - a * P4.X;

                    // Wektor kierunkowy między P4 i A
                    double directionX = A.X - P4.X;
                    double directionY = A.Y - P4.Y;

                    // Normalizacja wektora (wektor jednostkowy)
                    double length = Math.Sqrt(directionX * directionX + directionY * directionY);
                    double unitDirectionX = directionX / length;
                    double unitDirectionY = directionY / length;

                    // Znalezienie punktu po przeciwnej stronie niż A, w odległości "distance" od P4
                    double newX = P4.X - unitDirectionX * distance;
                    double newY = P4.Y - unitDirectionY * distance;

                    // Ustawienie nowej lokalizacji dla opposedpoint
                    opposedpoint.X = (int)newX;
                    opposedpoint.Y = (int)newY;
                }
            }



            if (remberpoint == ContinuityType.C1 && edge.Constraint != EdgeConstraint.FixedLength)
            {
                // Obliczenie aktualnej odległości między point a controlpoint
                double currentControlDistanceX = controlpoint.X - point.X;
                double currentControlDistanceY = controlpoint.Y - point.Y;

                // Obliczenie długości wektora point -> controlpoint (odległość między point a controlpoint)
                double controlDistance = Math.Sqrt(currentControlDistanceX * currentControlDistanceX + currentControlDistanceY * currentControlDistanceY);

                // Odległość między point a opposedpoint powinna być 3 razy większa niż odległość point -> controlpoint
                double targetOpposedDistance = controlDistance * 3.0;
                if(edge.Constraint == EdgeConstraint.Bezier) { targetOpposedDistance = controlDistance; }

                // Obliczenie aktualnej odległości między point a opposedpoint
                double currentOpposedDistanceX = opposedpoint.X - point.X;
                double currentOpposedDistanceY = opposedpoint.Y - point.Y;

                // Normalizacja wektora między point a opposedpoint (wektor jednostkowy)
                double lengthOpposed = Math.Sqrt(currentOpposedDistanceX * currentOpposedDistanceX + currentOpposedDistanceY * currentOpposedDistanceY);
                double unitDirectionX_Opposed = currentOpposedDistanceX / lengthOpposed;
                double unitDirectionY_Opposed = currentOpposedDistanceY / lengthOpposed;

                // Nowe współrzędne dla opposedpoint, tak aby odległość była 3 razy większa
                double newOpposedX = point.X + unitDirectionX_Opposed * targetOpposedDistance;
                double newOpposedY = point.Y + unitDirectionY_Opposed * targetOpposedDistance;

                // Ustawienie nowych współrzędnych dla opposedpoint
                opposedpoint.X = (int)newOpposedX;
                opposedpoint.Y = (int)newOpposedY;
            }

            if (edge.End.X == edgeWithControl.Start.X && edge.End.Y == edgeWithControl.Start.Y)
            {
                edgeWithControl.ControlPoint1 = ToVertex(controlpoint);
                edge.End = ToVertex(point);
                edge.End.Continuity = remberpoint;
                if (edge.Constraint != EdgeConstraint.Bezier)
                {
                    edge.Start = ToVertex(opposedpoint);
                    edge.Start.Continuity = remberopposedpoint;
                    Edges[Countposition(Edges.IndexOf(edge) - 1)].End = edge.Start;
                    Vertices[Countposition(Edges.IndexOf(edge))] = edge.Start;
                }

            }
            else
            {
                edgeWithControl.ControlPoint2 = ToVertex(controlpoint);
                edge.Start = ToVertex(point);
                edge.Start.Continuity = remberpoint;
                if (edge.Constraint != EdgeConstraint.Bezier)
                {
                    edge.End = ToVertex(opposedpoint);
                    edge.End.Continuity = remberopposedpoint;
                    Edges[Countposition(Edges.IndexOf(edge) + 1)].Start = edge.End;
                    Vertices[Countposition(Edges.IndexOf(edge) + 1)] = edge.End;
                }
            }

            if (edge.Constraint == EdgeConstraint.Bezier)
            {
                if (edge.End.X == edgeWithControl.Start.X && edge.End.Y == edgeWithControl.Start.Y)
                {
                    edge.ControlPoint2 = ToVertex(opposedpoint);
                }
                else
                {
                    edge.ControlPoint1 = ToVertex(opposedpoint);
                }
            }
            else
            {
                if (edge.End.X == edgeWithControl.Start.X && edge.End.Y == edgeWithControl.Start.Y)
                {
                     edge.Start = ToVertex(opposedpoint);
                     edge.Start.Continuity = remberopposedpoint;
                }
                else
                {
                    edge.End = ToVertex(opposedpoint);
                    edge.End.Continuity = remberopposedpoint;
                }
            }

            

        }
    }
}
