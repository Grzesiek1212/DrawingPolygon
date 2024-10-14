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
        private Edge? draggedBezierEdge = null; // Kraw�d�, kt�rej punkty kontrolne s� przeci�gane

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

                // Sprawdzamy, czy klikni�to na wierzcho�ek i czy jest to wierzcho�ek zwi�zany z krzyw� B�ziera
                for (int i = 0; i < polygon.Vertices.Count; i++)
                {
                    if (IsPointNearVertex(new Point(e.X, e.Y), polygon.Vertices[i].ToPoint()))
                    {
                        // Sprawdzamy, czy wierzcho�ek nale�y do kraw�dzi B�ziera
                        if (IsVertexPartOfBezierEdge(i))
                        {
                            ShowVertexContextMenu(e,i); // Wy�wietlamy menu ustawienia ci�g�o�ci
                            return;
                        }
                        else
                        {
                            if (deleteVertex(e)) return; // Usuwamy wierzcho�ek, je�li nie nale�y do B�ziera
                        }
                    }
                }

                Edge edge = WhichEdgeisnear(e);
                if (edge != null) ShowEdgeContextMenu(e);
                return;
            }

            if (isPolygonClosed)
            {
                // Sprawdzamy, czy klikni�to w pobli�u jakiego� wierzcho�ka
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

                // sprawdzamy czy klilknelismy na wiercho�ek kontrolny
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

            // Sprawdzamy, czy klikni�to blisko pierwszego punktu (zamkni�cie wielok�ta)
            if (!isPolygonClosed && polygon.Vertices.Count > 2 && IsCloseToFirstVertex(new Point(e.X, e.Y)))
            {
                isPolygonClosed = true;
                polygon.ClosePolygon();
                polygon.UpdateEdges(-1);  // Aktualizujemy kraw�dzie po zamkni�ciu
                drawingPanel.Invalidate();
                return;
            }

            // Dodajemy nowy wierzcho�ek
            polygon.AddVertex(new Vertex(e.X, e.Y));
            drawingPanel.Invalidate();
        }
        private void drawingPanel_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            // Rysujemy kraw�dzie
            foreach (var edge in polygon.Edges)
            {
                edge.Draw(g);

                // Rysowanie ikon ogranicze�
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
                            icon = "[]"; // Ikona sta�ej d�ugo�ci
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

                    // Rysowanie linii przerywanych mi�dzy wierzcho�kami a punktami kontrolnymi
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

            // Rysujemy wierzcho�ki
            foreach (var vertex in polygon.Vertices)
            {
                g.FillEllipse(Brushes.Red, vertex.X - 3, vertex.Y - 3, 6, 6);
            }
        }
        private void drawingPanel_MouseMove(object sender, MouseEventArgs e)
        {
            // Je�li przeci�gamy wierzcho�ek
            if (draggingVertex && draggedVertexIndex != -1)
            {
                var draggedVertex = polygon.Vertices[draggedVertexIndex];

                // Sprawd� ci�g�o�� wierzcho�ka
                if (draggedVertex.Continuity == ContinuityType.G1 || draggedVertex.Continuity == ContinuityType.C1)
                {
                    // Przesuwaj wierzcho�ek z ograniczeniem ci�g�o�ci
                    ApplyContinuityConstraints(draggedVertexIndex, e.Location);
                }
                else
                {
                    // Standardowe przesuwanie
                    draggedVertex.X = e.X;
                    draggedVertex.Y = e.Y;
                }

                polygon.UpdateEdges(-1);  // Aktualizacja kraw�dzi
                polygon.ApplyConstraints();
                drawingPanel.Invalidate();
            }

            // Je�li przeci�gamy ca�y wielok�t
            if (draggingPolygon)
            {
                int dx = e.X - lastMousePosition.X;
                int dy = e.Y - lastMousePosition.Y;

                // Przesuwamy wszystkie wierzcho�ki
                foreach (var vertex in polygon.Vertices)
                {
                    vertex.X += dx;
                    vertex.Y += dy;
                }

                foreach (var edge in polygon.Edges)
                {
                    if (edge.Constraint == EdgeConstraint.Bezier)
                    {
                        // Przesuwamy punkty kontrolne krzywej B�ziera
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

            // je�li przeci�gmy punkt kontrolny dla krzywej B�ziera
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

            // Zako�czenie przeci�gania punktu kontrolnego B�ziera
            draggingControlPoint = false;
            draggedControlPointIndex = -1;
            draggedBezierEdge = null;
        }


        // Dodawanie i odejmowanie Vertex�w
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
                    polygon.Vertices.Insert(i + 1, newVertex); // Wstawiamy nowy wierzcho�ek mi�dzy dwa istniej�ce
                    polygon.Edges.Insert(i + 1, new Edge(newVertex, edge.End));
                    polygon.Edges[i].End = newVertex;
                    polygon.UpdateEdges(-2); // Aktualizujemy kraw�dzie
                    drawingPanel.Invalidate();
                    return;
                }
            }
        }


        // funkcje sprawdzaj�ce czy po kliku jeste�my czego� blisko
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
            // Sprawdzenie, czy wielok�t jest zamkni�ty
            if (!isPolygonClosed) return false;

            // Liczba wierzcho�k�w wielok�ta
            int vertexCount = polygon.Vertices.Count;

            // Zmienna do przechowywania liczby przeci��
            int intersections = 0;

            // Przechodzimy przez ka�d� kraw�d� wielok�ta
            for (int i = 0, j = vertexCount - 1; i < vertexCount; j = i++)
            {
                Point vertex1 = new Point(polygon.Vertices[i].X, polygon.Vertices[i].Y); // Bie��cy wierzcho�ek
                Point vertex2 = new Point(polygon.Vertices[j].X, polygon.Vertices[j].Y); // Poprzedni wierzcho�ek (zamykaj�cy kraw�d�)

                // Sprawdzamy, czy promie� przecina kraw�d�
                if ((vertex1.Y > point.Y) != (vertex2.Y > point.Y)) // Jeden wierzcho�ek nad punktem, drugi pod nim
                {
                    // Obliczamy punkt przeci�cia promienia z kraw�dzi�
                    double intersectionX = vertex1.X + (point.Y - vertex1.Y) * (vertex2.X - vertex1.X) / (vertex2.Y - vertex1.Y);

                    // Sprawdzamy, czy punkt przeci�cia znajduje si� po prawej stronie punktu
                    if (intersectionX > point.X)
                    {
                        // Zliczamy przeci�cie
                        intersections++;
                    }
                }
            }

            // Je�li liczba przeci�� jest nieparzysta, punkt jest wewn�trz
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


        // ustawianie relacji na kraw�dziach
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
                polygon.ApplyConstraints(); // Stosujemy ograniczenia na wszystkie kraw�dzie
                drawingPanel.Invalidate(); // Od�wie�amy panel rysowania
            }
        }
        private void SetFixedLengthConstraint(int edgeIndex)
        {
            if (polygon.Edges[edgeIndex].Constraint != EdgeConstraint.None)
            {
                MessageBox.Show("Cannot set this constraint because this edge has constraint.", "Constraint Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var prompt = new PromptForm("Enter fixed length:", (float)Vertex.Distance(polygon.Edges[edgeIndex].Start, polygon.Edges[edgeIndex].End)); // Przyk�adowy formularz do wprowadzania d�ugo�ci
            if (prompt.ShowDialog() == DialogResult.OK)
            {
                float length;
                if (float.TryParse(prompt.InputValue, out length))
                {
                    polygon.Edges[edgeIndex].FixedLength = length;
                    polygon.SetEdgeConstraint(edgeIndex, EdgeConstraint.FixedLength);
                    polygon.ApplyConstraints(); // Zastosuj ograniczenie
                    drawingPanel.Invalidate(); // Od�wie� panel rysowania
                }
            }
        }
        private void RemoveEdgeConstraint(int edgeIndex)
        {
            polygon.RemoveEdgeConstraint(edgeIndex);
            polygon.ApplyConstraints(); // Stosujemy zmiany
            drawingPanel.Invalidate(); // Od�wie�amy panel rysowania
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

            // Ustaw domy�lne punkty kontrolne na �rodku kraw�dzi
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

        
        // ustawianie relacji na wierzcho�kach
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
            // Sprawdzenie, czy wierzcho�ek nale�y do kraw�dzi B�ziera
            if (!IsVertexPartOfBezierEdge(vertexIndex))
            {
                MessageBox.Show("The vertex is not part of a B�zier edge.", "Constraint Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Ustawienie ci�g�o�ci dla wierzcho�ka
            var vertex = polygon.Vertices[vertexIndex];
            vertex.Continuity = continuity; // Zak�adam, �e wierzcho�ek ma w�a�ciwo�� Continuity

            // Przywracanie ci�g�o�ci, je�li wierzcho�ek by� przemieszczany
            Point newLocation = vertex.ToPoint(); // Zak�adam, �e wierzcho�ek ma w�a�ciwo�� Position

            ApplyContinuityConstraints(vertexIndex, newLocation);

            // Opcjonalnie: informowanie u�ytkownika o sukcesie
            MessageBox.Show($"Continuity set to {continuity} for vertex {vertexIndex}.", "Continuity Set", MessageBoxButtons.OK, MessageBoxIcon.Information);
            polygon.UpdateEdges(-1);  // Aktualizacja kraw�dzi
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
                        return true; // Wierzcho�ek jest cz�ci� krzywej B�ziera
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
                // Przesuwamy wierzcho�ek tak, aby zachowa� styczno�� jednostkow�
                PreserveG1Continuity(incomingEdge, outgoingEdge, newLocation);
            }
            else if (vertex.Continuity == ContinuityType.C1)
            {
                // Przesuwamy wierzcho�ek tak, aby zachowa� ci�g�o�� wektorow�
                PreserveC1Continuity(incomingEdge, outgoingEdge, newLocation);
            }

            vertex.X = newLocation.X;
            vertex.Y = newLocation.Y;
        }

        private void PreserveG1Continuity(Edge incomingEdge, Edge outgoingEdge, Point newLocation)
        {
            // Przypisz now� lokalizacj� do wierzcho�ka, gdzie styka si� incomingEdge i outgoingEdge
            incomingEdge.End = ToVertex(newLocation);
            outgoingEdge.Start = ToVertex(newLocation);

            // Oblicz wektor styczny dla incomingEdge
            Point incomingTangent;

            if (incomingEdge.Constraint == EdgeConstraint.Bezier)
            {
                // Je�li incomingEdge jest krzyw� B�ziera, oblicz wektor styczny z ostatniego punktu kontrolnego
                Point p1 = incomingEdge.End.ToPoint(); // Punkt ko�cowy incomingEdge
                Point p2 = incomingEdge.ControlPoint2.ToPoint(); // Ostatni punkt kontrolny incomingEdge
                incomingTangent = new Point(p1.X - p2.X, p1.Y - p2.Y); // Wektor styczny
            }
            else
            {
                // Je�li incomingEdge jest prost�, oblicz wektor styczny bezpo�rednio z wierzcho�k�w
                Point p1 = incomingEdge.Start.ToPoint(); // Punkt pocz�tkowy incomingEdge
                Point p2 = incomingEdge.End.ToPoint(); // Punkt ko�cowy incomingEdge
                incomingTangent = new Point(p2.X - p1.X, p2.Y - p1.Y); // Wektor styczny
            }

            // Oblicz nowy punkt kontrolny dla outgoingEdge, je�li jest B�zierem
            if (outgoingEdge.Constraint == EdgeConstraint.Bezier)
            {
                Point p3 = outgoingEdge.Start.ToPoint(); // Pocz�tek outgoingEdge (kt�ry jest newLocation)

                // D�ugo�� wektora stycznego outgoingEdge powinna odpowiada� incomingEdge
                outgoingEdge.ControlPoint1 = new Vertex(p3.X + incomingTangent.X, p3.Y + incomingTangent.Y);
            }
            else
            {
                // Je�li outgoingEdge jest prost�, upewnij si�, �e kierunek outgoingEdge jest zgodny z incomingEdge
                Point p3 = outgoingEdge.End.ToPoint(); // Koniec outgoingEdge

                // Upewnij si�, �e wektor outgoingEdge ma ten sam kierunek co incomingTangent
                Point outgoingTangent = new Point(p3.X - newLocation.X, p3.Y - newLocation.Y);

                // Skaluje outgoingTangent do d�ugo�ci incomingTangent
                double lengthRatio = CalculateVectorLength(incomingTangent) / CalculateVectorLength(outgoingTangent);
                outgoingEdge.End = new Vertex((int)(newLocation.X + outgoingTangent.X * lengthRatio), (int)(newLocation.Y + outgoingTangent.Y * lengthRatio));
            }
        }


        private void PreserveC1Continuity(Edge incomingEdge, Edge outgoingEdge, Point newLocation)
        {
            // Aktualizujemy pocz�tek outgoingEdge na now� lokalizacj�
            outgoingEdge.Start = ToVertex(newLocation);

            // Sprawdzenie, czy incomingEdge jest krzyw� B�ziera
            if (incomingEdge.Constraint == EdgeConstraint.Bezier)
            {
                if (outgoingEdge.Constraint == EdgeConstraint.Bezier)
                {
                    // Obie kraw�dzie s� krzywymi B�ziera: P4 - P3 = P2 - P1
                    Point p1 = incomingEdge.End.ToPoint();  // Punkt ko�cowy incomingEdge (P1)
                    Point p2 = incomingEdge.ControlPoint2.ToPoint();  // Ostatni punkt kontrolny incomingEdge (P2)
                    Point p3 = outgoingEdge.ControlPoint1.ToPoint();  // Pierwszy punkt kontrolny outgoingEdge (P3)
                    Point p4 = outgoingEdge.Start.ToPoint();  // Pocz�tek outgoingEdge (P4)

                    // Oblicz r�nic� P2 - P1
                    Point delta = new Point(p2.X - p1.X, p2.Y - p1.Y);

                    // Aktualizujemy punkt kontrolny outgoingEdge (P3)
                    Point p = new Point(p4.X - delta.X, p4.Y - delta.Y);
                    outgoingEdge.ControlPoint1 = ToVertex(p);
                }
                else
                {
                    // incomingEdge jest krzyw� B�ziera, a outgoingEdge jest lini� prost�
                    Point a = outgoingEdge.Start.ToPoint();  // Pocz�tek outgoingEdge (A)
                    Point b = outgoingEdge.End.ToPoint();  // Koniec outgoingEdge (B)
                    Point p1 = incomingEdge.End.ToPoint();  // Punkt ko�cowy incomingEdge (P1)
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
                // incomingEdge jest lini� prost�, a outgoingEdge jest krzyw� B�ziera
                Point a = incomingEdge.Start.ToPoint();  // Pocz�tek incomingEdge (A)
                Point b = incomingEdge.End.ToPoint();  // Koniec incomingEdge (B)
                Point p1 = outgoingEdge.Start.ToPoint();  // Pocz�tek outgoingEdge (P1)
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
