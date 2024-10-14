using System.Windows.Forms;
using System.Drawing;
using System.Numerics;
namespace Gk1
{
    public partial class PolygonEditor : Form
    {
        private Polygon polygon;              // Instancja klasy Polygon
        private bool isPolygonClosed;
        private const int closeDistance = 10;

        private bool draggingVertex;
        private bool draggingPolygon;
        private int draggedVertexIndex = -1;
        private const int vertexRadius = 6;
        private Point lastMousePosition;
        private bool draggingControlPoint;
        private int draggedControlPointIndex = -1;
        private Edge? draggedBezierEdge = null; // KrawêdŸ, której punkty kontrolne s¹ przeci¹gane

        public PolygonEditor()
        {
            InitializeComponent();
            polygon = new Polygon();          // Inicjalizacja obiektu Polygon
            isPolygonClosed = false;
            draggingVertex = false;
            draggingPolygon = false;
            this.DoubleBuffered = true;
        }


        // funkcje drawingPanel 
        private void drawingPanel_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {

                // Sprawdzamy, czy klikniêto na wierzcho³ek i czy jest to wierzcho³ek zwi¹zany z krzyw¹ Béziera
                for (int i = 0; i < polygon.Vertices.Count; i++)
                {
                    if (IsPointNearVertex(new Point(e.X, e.Y), polygon.Vertices[i].ToPoint()))
                    {
                        // Sprawdzamy, czy wierzcho³ek nale¿y do krawêdzi Béziera
                        if (IsVertexPartOfBezierEdge(i))
                        {
                            ShowVertexContextMenu(e,i); // Wyœwietlamy menu ustawienia ci¹g³oœci
                            return;
                        }
                        else
                        {
                            if (deleteVertex(e)) return; // Usuwamy wierzcho³ek, jeœli nie nale¿y do Béziera
                        }
                    }
                }

                Edge edge = WhichEdgeisnear(e);
                if (edge != null) ShowEdgeContextMenu(e);
                return;
            }

            if (isPolygonClosed)
            {
                // Sprawdzamy, czy klikniêto w pobli¿u jakiegoœ wierzcho³ka
                for (int i = 0; i < polygon.Vertices.Count; i++)
                {
                    if (IsPointNearVertex(new Point(e.X, e.Y), polygon.Vertices[i].ToPoint()))
                    {
                        draggingVertex = true;
                        draggedVertexIndex = i;
                        return;
                    }
                }

                addVertexOnTheHalf(e);

                // sprawdzamy czy klilknelismy na wiercho³ek kontrolny
                foreach (var edge in polygon.Edges)
                {
                    if (edge.Constraint == EdgeConstraint.Bezier)
                    {
                        if (IsPointNearVertex(e.Location, edge.ControlPoint1.ToPoint()))
                        {
                            draggingControlPoint = true;
                            draggedControlPointIndex = 1;
                            draggedBezierEdge = edge;
                            return;
                        }

                        if (IsPointNearVertex(e.Location, edge.ControlPoint2.ToPoint()))
                        {
                            draggingControlPoint = true;
                            draggedControlPointIndex = 2;
                            draggedBezierEdge = edge;
                            return;
                        }
                    }
                }


                if (IsPointInsidePolygon(new Point(e.X, e.Y)))
                {
                    draggingPolygon = true;
                    lastMousePosition = e.Location;
                    return;
                }
                return;
            }

            // Sprawdzamy, czy klikniêto blisko pierwszego punktu (zamkniêcie wielok¹ta)
            if (!isPolygonClosed && polygon.Vertices.Count > 2 && IsCloseToFirstVertex(new Point(e.X, e.Y)))
            {
                isPolygonClosed = true;
                polygon.ClosePolygon();
                polygon.UpdateEdges(-1);  // Aktualizujemy krawêdzie po zamkniêciu
                drawingPanel.Invalidate();
                return;
            }

            // Dodajemy nowy wierzcho³ek
            polygon.AddVertex(new Vertex(e.X, e.Y));
            drawingPanel.Invalidate();
        }
        private void drawingPanel_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            // Rysujemy krawêdzie
            foreach (var edge in polygon.Edges)
            {
                edge.Draw(g);

                // Rysowanie ikon ograniczeñ
                if (edge.Constraint != EdgeConstraint.None)
                {
                    Point midPoint = edge.MidPoint();
                    string icon = "";
                    switch (edge.Constraint)
                    {
                        case EdgeConstraint.Horizontal:
                            icon = "---"; // Ikona pozioma
                            break;
                        case EdgeConstraint.Vertical:
                            icon = "^^^"; // Ikona pionowa
                            break;
                        case EdgeConstraint.FixedLength:
                            icon = "[]"; // Ikona sta³ej d³ugoœci
                            break;
                        case EdgeConstraint.Bezier:
                            icon = "<>"; // Ikonka Beziera
                            break;
                    }
                    g.DrawString(icon, this.Font, Brushes.Blue, midPoint);
                }
                if (edge.Constraint == EdgeConstraint.Bezier)
                {
                    g.FillEllipse(Brushes.Blue, edge.ControlPoint1.X - 3, edge.ControlPoint1.Y - 3, 6, 6); // Punkt kontrolny 1
                    g.FillEllipse(Brushes.Blue, edge.ControlPoint2.X - 3, edge.ControlPoint2.Y - 3, 6, 6); // Punkt kontrolny 2

                    // Rysowanie linii przerywanych miêdzy wierzcho³kami a punktami kontrolnymi
                    using (Pen dashedPen = new Pen(Color.Gray, 2)) 
                    {
                        dashedPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Custom;
                        dashedPen.DashPattern = new float[] { 8, 4 };
                        g.DrawLine(dashedPen, edge.Start.ToPoint(), edge.ControlPoint1.ToPoint());
                        g.DrawLine(dashedPen, edge.End.ToPoint(), edge.ControlPoint2.ToPoint());
                        g.DrawLine(dashedPen, edge.ControlPoint1.ToPoint(), edge.ControlPoint2.ToPoint());
                    }
                }
            }

            // Rysujemy wierzcho³ki
            foreach (var vertex in polygon.Vertices)
            {
                g.FillEllipse(Brushes.Red, vertex.X - 3, vertex.Y - 3, 6, 6);
            }
        }
        private void drawingPanel_MouseMove(object sender, MouseEventArgs e)
        {
            // Jeœli przeci¹gamy wierzcho³ek
            if (draggingVertex && draggedVertexIndex != -1)
            {
                var draggedVertex = polygon.Vertices[draggedVertexIndex];

                // SprawdŸ ci¹g³oœæ wierzcho³ka
                if (draggedVertex.Continuity == ContinuityType.G1 || draggedVertex.Continuity == ContinuityType.C1)
                {
                    // Przesuwaj wierzcho³ek z ograniczeniem ci¹g³oœci
                    ApplyContinuityConstraints(draggedVertexIndex, e.Location);
                }
                else
                {
                    // Standardowe przesuwanie
                    draggedVertex.X = e.X;
                    draggedVertex.Y = e.Y;
                }

                polygon.UpdateEdges(-1);  // Aktualizacja krawêdzi
                polygon.ApplyConstraints();
                drawingPanel.Invalidate();
            }

            // Jeœli przeci¹gamy ca³y wielok¹t
            if (draggingPolygon)
            {
                int dx = e.X - lastMousePosition.X;
                int dy = e.Y - lastMousePosition.Y;

                // Przesuwamy wszystkie wierzcho³ki
                foreach (var vertex in polygon.Vertices)
                {
                    vertex.X += dx;
                    vertex.Y += dy;
                }

                foreach (var edge in polygon.Edges)
                {
                    if (edge.Constraint == EdgeConstraint.Bezier)
                    {
                        // Przesuwamy punkty kontrolne krzywej Béziera
                        edge.ControlPoint1.X += dx;
                        edge.ControlPoint1.Y += dy;
                        edge.ControlPoint2.X += dx;
                        edge.ControlPoint2.Y += dy;
                    }
                }

                lastMousePosition = e.Location;
                polygon.UpdateEdges(-1);
                polygon.ApplyConstraints();
                drawingPanel.Invalidate();
                return;
            }

            // jeœli przeci¹gmy punkt kontrolny dla krzywej Béziera
            if (draggingControlPoint && draggedBezierEdge != null)
            {
                if (draggedControlPointIndex == 1)
                {
                    draggedBezierEdge.ControlPoint1.X = e.X;
                    draggedBezierEdge.ControlPoint1.Y = e.Y;
                }
                else if (draggedControlPointIndex == 2)
                {
                    draggedBezierEdge.ControlPoint2.X = e.X;
                    draggedBezierEdge.ControlPoint2.Y = e.Y;
                }

                drawingPanel.Invalidate();
                return;
            }
        }
        private void drawingPanel_MouseUp(object sender, MouseEventArgs e)
        {
            draggingVertex = false;
            draggedVertexIndex = -1;

            draggingPolygon = false;

            // Zakoñczenie przeci¹gania punktu kontrolnego Béziera
            draggingControlPoint = false;
            draggedControlPointIndex = -1;
            draggedBezierEdge = null;
        }


        // Dodawanie i odejmowanie Vertexów
        private bool deleteVertex(MouseEventArgs e)
        {
            for (int i = 0; i < polygon.Vertices.Count; i++)
            {
                if (IsPointNearVertex(new Point(e.X, e.Y), polygon.Vertices[i].ToPoint()))
                {
                    // usuwanie relacji
                    int poprzedni = i - 1;
                    if (poprzedni < 0) poprzedni = polygon.Edges.Count - 1;
                    polygon.Edges[poprzedni].RemoveConstraint();
                    polygon.Edges[i].RemoveConstraint();

                    polygon.RemoveVertexAt(i);

                    if (polygon.Vertices.Count < 3)
                    {
                        isPolygonClosed = false;
                    }
                    
                    drawingPanel.Invalidate();
                    return true;
                }
            }
            return false;
        }
        private void addVertexOnTheHalf(MouseEventArgs e)
        {
            for (int i = 0; i < polygon.Edges.Count; i++)
            {
                Edge edge = polygon.Edges[i];
                if (IsPointNearEdge(new Point(e.X, e.Y), edge.Start.ToPoint(), edge.End.ToPoint()))
                {
                    polygon.Edges[i].RemoveConstraint();
                    Point mid = edge.MidPoint();
                    Vertex newVertex = new Vertex(mid.X, mid.Y);
                    polygon.Vertices.Insert(i + 1, newVertex); // Wstawiamy nowy wierzcho³ek miêdzy dwa istniej¹ce
                    polygon.Edges.Insert(i + 1, new Edge(newVertex, edge.End));
                    polygon.Edges[i].End = newVertex;
                    polygon.UpdateEdges(-2); // Aktualizujemy krawêdzie
                    drawingPanel.Invalidate();
                    return;
                }
            }
        }


        // funkcje sprawdzaj¹ce czy po kliku jesteœmy czegoœ blisko
        private bool IsCloseToFirstVertex(Point currentPoint)
        {
            if (polygon.Vertices.Count == 0) return false;

            Vertex firstVertex = polygon.Vertices[0];
            double distance = Vertex.Distance(new Vertex(currentPoint.X, currentPoint.Y), firstVertex);

            return distance < closeDistance;
        }
        private bool IsPointNearVertex(Point clickPoint, Point vertex)
        {
            double distance = Math.Sqrt(Math.Pow(clickPoint.X - vertex.X, 2) + Math.Pow(clickPoint.Y - vertex.Y, 2));
            return distance < vertexRadius;
        }
        private bool IsPointNearEdge(Point clickPoint, Point start, Point end)
        {
            double edgeLength = Math.Sqrt(Math.Pow(end.X - start.X, 2) + Math.Pow(end.Y - start.Y, 2));
            if (edgeLength == 0) return false;

            double edgeX = end.X - start.X;
            double edgeY = end.Y - start.Y;

            double normalX = -edgeY;
            double normalY = edgeX;

            double normalLength = Math.Sqrt(normalX * normalX + normalY * normalY);
            normalX /= normalLength;
            normalY /= normalLength;

            double t = (clickPoint.X - start.X) * edgeX + (clickPoint.Y - start.Y) * edgeY;
            t /= edgeLength * edgeLength;

            Point closestPoint = new Point((int)(start.X + t * edgeX), (int)(start.Y + t * edgeY));

            double distanceToEdge = Math.Sqrt(Math.Pow(clickPoint.X - closestPoint.X, 2) +
                                               Math.Pow(clickPoint.Y - closestPoint.Y, 2));

            return distanceToEdge < closeDistance;
        }
        private Edge? WhichEdgeisnear(MouseEventArgs e)
        {
            for (int i = 0; i < polygon.Edges.Count; i++)
            {
                Edge edge = polygon.Edges[i];
                if (IsPointNearEdge(new Point(e.X, e.Y), edge.Start.ToPoint(), edge.End.ToPoint()))
                {
                    return edge;
                }
            }
            return null;
        }
        private bool IsPointInsidePolygon(Point point)
        {
            // Sprawdzenie, czy wielok¹t jest zamkniêty
            if (!isPolygonClosed) return false;

            // Liczba wierzcho³ków wielok¹ta
            int vertexCount = polygon.Vertices.Count;

            // Zmienna do przechowywania liczby przeciêæ
            int intersections = 0;

            // Przechodzimy przez ka¿d¹ krawêdŸ wielok¹ta
            for (int i = 0, j = vertexCount - 1; i < vertexCount; j = i++)
            {
                Point vertex1 = new Point(polygon.Vertices[i].X, polygon.Vertices[i].Y); // Bie¿¹cy wierzcho³ek
                Point vertex2 = new Point(polygon.Vertices[j].X, polygon.Vertices[j].Y); // Poprzedni wierzcho³ek (zamykaj¹cy krawêdŸ)

                // Sprawdzamy, czy promieñ przecina krawêdŸ
                if ((vertex1.Y > point.Y) != (vertex2.Y > point.Y)) // Jeden wierzcho³ek nad punktem, drugi pod nim
                {
                    // Obliczamy punkt przeciêcia promienia z krawêdzi¹
                    double intersectionX = vertex1.X + (point.Y - vertex1.Y) * (vertex2.X - vertex1.X) / (vertex2.Y - vertex1.Y);

                    // Sprawdzamy, czy punkt przeciêcia znajduje siê po prawej stronie punktu
                    if (intersectionX > point.X)
                    {
                        // Zliczamy przeciêcie
                        intersections++;
                    }
                }
            }

            // Jeœli liczba przeciêæ jest nieparzysta, punkt jest wewn¹trz
            return (intersections % 2 != 0);
        }



        // pokazywanie menu
        private void ShowEdgeContextMenu(MouseEventArgs e)
        {
            Edge edge = WhichEdgeisnear(e);
            if (edge == null) return;

            ContextMenuStrip contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Set Horizontal Constraint", null, (sender, args) => SetEdgeConstraint(polygon.Edges.IndexOf(edge), EdgeConstraint.Horizontal));
            contextMenu.Items.Add("Set Vertical Constraint", null, (sender, args) => SetEdgeConstraint(polygon.Edges.IndexOf(edge), EdgeConstraint.Vertical));
            contextMenu.Items.Add("Set Fixed Length Constraint", null, (sender, args) => SetFixedLengthConstraint(polygon.Edges.IndexOf(edge)));
            contextMenu.Items.Add("Remove Constraint", null, (sender, args) => RemoveEdgeConstraint(polygon.Edges.IndexOf(edge)));

            if (edge.Constraint != EdgeConstraint.Bezier)
            {
                contextMenu.Items.Add("Set Bezier", null, (sender, args) => SetBezierEdge(polygon.Edges.IndexOf(edge)));
            }
            else
            {
                contextMenu.Items.Add("Unset Bezier", null, (sender, args) => UnsetBezierEdge(polygon.Edges.IndexOf(edge)));
            }

            contextMenu.Show(drawingPanel, new Point(e.X, e.Y));
        }


        // ustawianie relacji na krawêdziach
        private void SetEdgeConstraint(int edgeIndex, EdgeConstraint constraint)
        {
            if (polygon.Edges[edgeIndex].Constraint != EdgeConstraint.None)
            {
                MessageBox.Show("Cannot set this constraint because this edge has constraint.", "Constraint Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!polygon.SetEdgeConstraint(edgeIndex, constraint))
            {
                MessageBox.Show("Cannot set this constraint because adjacent edges have the same constraint.", "Constraint Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                polygon.ApplyConstraints(); // Stosujemy ograniczenia na wszystkie krawêdzie
                drawingPanel.Invalidate(); // Odœwie¿amy panel rysowania
            }
        }
        private void SetFixedLengthConstraint(int edgeIndex)
        {
            if (polygon.Edges[edgeIndex].Constraint != EdgeConstraint.None)
            {
                MessageBox.Show("Cannot set this constraint because this edge has constraint.", "Constraint Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var prompt = new PromptForm("Enter fixed length:", (float)Vertex.Distance(polygon.Edges[edgeIndex].Start, polygon.Edges[edgeIndex].End)); // Przyk³adowy formularz do wprowadzania d³ugoœci
            if (prompt.ShowDialog() == DialogResult.OK)
            {
                float length;
                if (float.TryParse(prompt.InputValue, out length))
                {
                    polygon.Edges[edgeIndex].FixedLength = length;
                    polygon.SetEdgeConstraint(edgeIndex, EdgeConstraint.FixedLength);
                    polygon.ApplyConstraints(); // Zastosuj ograniczenie
                    drawingPanel.Invalidate(); // Odœwie¿ panel rysowania
                }
            }
        }
        private void RemoveEdgeConstraint(int edgeIndex)
        {
            polygon.RemoveEdgeConstraint(edgeIndex);
            polygon.ApplyConstraints(); // Stosujemy zmiany
            drawingPanel.Invalidate(); // Odœwie¿amy panel rysowania
        }
        private void SetBezierEdge(int edgeIndex)
        {
            if (polygon.Edges[edgeIndex].Constraint != EdgeConstraint.None) 
            {
                MessageBox.Show("Cannot set this constraint because this edge has constraint.", "Constraint Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var edge = polygon.Edges[edgeIndex];
            edge.Constraint = EdgeConstraint.Bezier;

            // Ustaw domyœlne punkty kontrolne na œrodku krawêdzi
            Point mid = edge.MidPoint();
            edge.ControlPoint1 = new Vertex(mid.X - 30, mid.Y - 30);
            edge.ControlPoint2 = new Vertex(mid.X + 30, mid.Y + 30);
            edge.StartContinuity = ContinuityType.C1;
            edge.EndContinuity = ContinuityType.C1;

            drawingPanel.Invalidate();
        }
        private void UnsetBezierEdge(int edgeIndex)
        {
            if (polygon.Edges[edgeIndex].Constraint != EdgeConstraint.Bezier)
            {
                MessageBox.Show("Cannot set this constraint because this edge has not be Bezier line.", "Constraint Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            polygon.Edges[edgeIndex].RemoveConstraint();
            drawingPanel.Invalidate();
        }

        
        // ustawianie relacji na wierzcho³kach
        private void ShowVertexContextMenu(MouseEventArgs e, int vertexIndex)
        {
            ContextMenuStrip contextMenu = new ContextMenuStrip();

            contextMenu.Items.Add("Set Continuity G0", null, (sender, args) => SetVertexContinuity(vertexIndex, ContinuityType.G0));
            contextMenu.Items.Add("Set Continuity G1", null, (sender, args) => SetVertexContinuity(vertexIndex, ContinuityType.G1));
            contextMenu.Items.Add("Set Continuity C1", null, (sender, args) => SetVertexContinuity(vertexIndex, ContinuityType.C1));
            contextMenu.Items.Add("Delete vertex", null, (sender, args) => deleteVertex(e));
            contextMenu.Show(drawingPanel, new Point(e.X, e.Y));
        }
        private void SetVertexContinuity(int vertexIndex, ContinuityType continuity)
        {
            // Sprawdzenie, czy wierzcho³ek nale¿y do krawêdzi Béziera
            if (!IsVertexPartOfBezierEdge(vertexIndex))
            {
                MessageBox.Show("The vertex is not part of a Bézier edge.", "Constraint Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Ustawienie ci¹g³oœci dla wierzcho³ka
            var vertex = polygon.Vertices[vertexIndex];
            vertex.Continuity = continuity; // Zak³adam, ¿e wierzcho³ek ma w³aœciwoœæ Continuity

            // Przywracanie ci¹g³oœci, jeœli wierzcho³ek by³ przemieszczany
            Point newLocation = vertex.ToPoint(); // Zak³adam, ¿e wierzcho³ek ma w³aœciwoœæ Position

            ApplyContinuityConstraints(vertexIndex, newLocation);

            // Opcjonalnie: informowanie u¿ytkownika o sukcesie
            MessageBox.Show($"Continuity set to {continuity} for vertex {vertexIndex}.", "Continuity Set", MessageBoxButtons.OK, MessageBoxIcon.Information);
            polygon.UpdateEdges(-1);  // Aktualizacja krawêdzi
            polygon.ApplyConstraints();
            drawingPanel.Invalidate();
        }
        private bool IsVertexPartOfBezierEdge(int vertexIndex)
        {
            foreach (var edge in polygon.Edges)
            {
                if (edge.Constraint == EdgeConstraint.Bezier)
                {
                    if (edge.Start == polygon.Vertices[vertexIndex] || edge.End == polygon.Vertices[vertexIndex])
                    {
                        return true; // Wierzcho³ek jest czêœci¹ krzywej Béziera
                    }
                }
            }
            return false;
        }

        private void ApplyContinuityConstraints(int vertexIndex, Point newLocation)
        {
            var vertex = polygon.Vertices[vertexIndex];
            var incomingEdge = polygon.GetIncomingEdge(vertexIndex);
            var outgoingEdge = polygon.GetOutgoingEdge(vertexIndex);
            if (incomingEdge == null || outgoingEdge == null) return;
            
            if (vertex.Continuity == ContinuityType.G1)
            {
                // Przesuwamy wierzcho³ek tak, aby zachowaæ stycznoœæ jednostkow¹
                PreserveG1Continuity(incomingEdge, outgoingEdge, newLocation);
            }
            else if (vertex.Continuity == ContinuityType.C1)
            {
                // Przesuwamy wierzcho³ek tak, aby zachowaæ ci¹g³oœæ wektorow¹
                PreserveC1Continuity(incomingEdge, outgoingEdge, newLocation);
            }

            vertex.X = newLocation.X;
            vertex.Y = newLocation.Y;
        }

        private void PreserveG1Continuity(Edge incomingEdge, Edge outgoingEdge, Point newLocation)
        {
            // Przypisz now¹ lokalizacjê do wierzcho³ka, gdzie styka siê incomingEdge i outgoingEdge
            incomingEdge.End = ToVertex(newLocation);
            outgoingEdge.Start = ToVertex(newLocation);

            // Oblicz wektor styczny dla incomingEdge
            Point incomingTangent;

            if (incomingEdge.Constraint == EdgeConstraint.Bezier)
            {
                // Jeœli incomingEdge jest krzyw¹ Béziera, oblicz wektor styczny z ostatniego punktu kontrolnego
                Point p1 = incomingEdge.End.ToPoint(); // Punkt koñcowy incomingEdge
                Point p2 = incomingEdge.ControlPoint2.ToPoint(); // Ostatni punkt kontrolny incomingEdge
                incomingTangent = new Point(p1.X - p2.X, p1.Y - p2.Y); // Wektor styczny
            }
            else
            {
                // Jeœli incomingEdge jest prost¹, oblicz wektor styczny bezpoœrednio z wierzcho³ków
                Point p1 = incomingEdge.Start.ToPoint(); // Punkt pocz¹tkowy incomingEdge
                Point p2 = incomingEdge.End.ToPoint(); // Punkt koñcowy incomingEdge
                incomingTangent = new Point(p2.X - p1.X, p2.Y - p1.Y); // Wektor styczny
            }

            // Oblicz nowy punkt kontrolny dla outgoingEdge, jeœli jest Bézierem
            if (outgoingEdge.Constraint == EdgeConstraint.Bezier)
            {
                Point p3 = outgoingEdge.Start.ToPoint(); // Pocz¹tek outgoingEdge (który jest newLocation)

                // D³ugoœæ wektora stycznego outgoingEdge powinna odpowiadaæ incomingEdge
                outgoingEdge.ControlPoint1 = new Vertex(p3.X + incomingTangent.X, p3.Y + incomingTangent.Y);
            }
            else
            {
                // Jeœli outgoingEdge jest prost¹, upewnij siê, ¿e kierunek outgoingEdge jest zgodny z incomingEdge
                Point p3 = outgoingEdge.End.ToPoint(); // Koniec outgoingEdge

                // Upewnij siê, ¿e wektor outgoingEdge ma ten sam kierunek co incomingTangent
                Point outgoingTangent = new Point(p3.X - newLocation.X, p3.Y - newLocation.Y);

                // Skaluje outgoingTangent do d³ugoœci incomingTangent
                double lengthRatio = CalculateVectorLength(incomingTangent) / CalculateVectorLength(outgoingTangent);
                outgoingEdge.End = new Vertex((int)(newLocation.X + outgoingTangent.X * lengthRatio), (int)(newLocation.Y + outgoingTangent.Y * lengthRatio));
            }
        }


        private void PreserveC1Continuity(Edge incomingEdge, Edge outgoingEdge, Point newLocation)
        {
            // Aktualizujemy pocz¹tek outgoingEdge na now¹ lokalizacjê
            outgoingEdge.Start = ToVertex(newLocation);

            // Sprawdzenie, czy incomingEdge jest krzyw¹ Béziera
            if (incomingEdge.Constraint == EdgeConstraint.Bezier)
            {
                if (outgoingEdge.Constraint == EdgeConstraint.Bezier)
                {
                    // Obie krawêdzie s¹ krzywymi Béziera: P4 - P3 = P2 - P1
                    Point p1 = incomingEdge.End.ToPoint();  // Punkt koñcowy incomingEdge (P1)
                    Point p2 = incomingEdge.ControlPoint2.ToPoint();  // Ostatni punkt kontrolny incomingEdge (P2)
                    Point p3 = outgoingEdge.ControlPoint1.ToPoint();  // Pierwszy punkt kontrolny outgoingEdge (P3)
                    Point p4 = outgoingEdge.Start.ToPoint();  // Pocz¹tek outgoingEdge (P4)

                    // Oblicz ró¿nicê P2 - P1
                    Point delta = new Point(p2.X - p1.X, p2.Y - p1.Y);

                    // Aktualizujemy punkt kontrolny outgoingEdge (P3)
                    Point p = new Point(p4.X - delta.X, p4.Y - delta.Y);
                    outgoingEdge.ControlPoint1 = ToVertex(p);
                }
                else
                {
                    // incomingEdge jest krzyw¹ Béziera, a outgoingEdge jest lini¹ prost¹
                    Point a = outgoingEdge.Start.ToPoint();  // Pocz¹tek outgoingEdge (A)
                    Point b = outgoingEdge.End.ToPoint();  // Koniec outgoingEdge (B)
                    Point p1 = incomingEdge.End.ToPoint();  // Punkt koñcowy incomingEdge (P1)
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
                // incomingEdge jest lini¹ prost¹, a outgoingEdge jest krzyw¹ Béziera
                Point a = incomingEdge.Start.ToPoint();  // Pocz¹tek incomingEdge (A)
                Point b = incomingEdge.End.ToPoint();  // Koniec incomingEdge (B)
                Point p1 = outgoingEdge.Start.ToPoint();  // Pocz¹tek outgoingEdge (P1)
                Point p2 = outgoingEdge.ControlPoint1.ToPoint();  // Pierwszy punkt kontrolny outgoingEdge (P2)

                // Obliczamy 1/3 odcinka (B - A)
                Point delta = new Point((b.X - a.X) / 3, (b.Y - a.Y) / 3);

                // Aktualizujemy punkt kontrolny outgoingEdge (P2)
                Point p = new Point(p1.X + delta.X, p1.Y + delta.Y);
                outgoingEdge.ControlPoint1 = ToVertex(p);
            }
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
