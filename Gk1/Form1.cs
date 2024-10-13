using System.Windows.Forms;
using System.Drawing;
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
                if(deleteVertex(e))return; // tu trzeba dorobi� ze jak skasuje to dalej nie idzie
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
                polygon.Vertices[draggedVertexIndex].X = e.X;
                polygon.Vertices[draggedVertexIndex].Y = e.Y;

                polygon.UpdateEdges(-1);  // Aktualizacja kraw�dzi
                polygon.ApplyConstraints();
                drawingPanel.Invalidate();
                return;
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
            if (!isPolygonClosed) return false;
            int j = polygon.Vertices.Count - 1;
            bool inside = false;

            for (int i = 0; i < polygon.Vertices.Count; i++)
            {
                if (polygon.Vertices[i].Y < point.Y && polygon.Vertices[j].Y >= point.Y ||
                    polygon.Vertices[j].Y < point.Y && polygon.Vertices[i].Y >= point.Y)
                {
                    if (polygon.Vertices[i].X + (point.Y - polygon.Vertices[i].Y) / (polygon.Vertices[j].Y - polygon.Vertices[i].Y) *
                        (polygon.Vertices[j].X - polygon.Vertices[i].X) < point.X)
                    {
                        inside = !inside;
                    }
                }
                j = i;
            }

            return inside;
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


    }
}