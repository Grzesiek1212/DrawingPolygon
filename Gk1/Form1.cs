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

        public PolygonEditor()
        {
            InitializeComponent();
            polygon = new Polygon();          // Inicjalizacja obiektu Polygon
            isPolygonClosed = false;
            draggingVertex = false;
            draggingPolygon = false;
        }

        private void drawingPanel_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                deleteVertex(e);
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

                if (IsPointInsidePolygon(new Point(e.X, e.Y)))
                {
                    draggingPolygon = true;
                    lastMousePosition = e.Location;
                    return;
                }

                return;
            }

            // Sprawdzamy, czy klikni�to blisko pierwszego punktu (zamkni�cie wielok�ta)
            if (polygon.Vertices.Count > 2 && IsCloseToFirstVertex(new Point(e.X, e.Y)))
            {
                isPolygonClosed = true;
                polygon.ClosePolygon();
                polygon.UpdateEdges();  // Aktualizujemy kraw�dzie po zamkni�ciu
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
                g.DrawLine(Pens.Black, edge.Start.ToPoint(), edge.End.ToPoint());

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
                    }
                    g.DrawString(icon, this.Font, Brushes.Blue, midPoint);
                }
            }

            // Rysujemy wierzcho�ki
            foreach (var vertex in polygon.Vertices)
            {
                g.FillEllipse(Brushes.Red, vertex.X - 3, vertex.Y - 3, 6, 6);
            }
        }


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

        private void drawingPanel_MouseMove(object sender, MouseEventArgs e)
        {
            // Je�li przeci�gamy wierzcho�ek
            if (draggingVertex && draggedVertexIndex != -1)
            {
                polygon.Vertices[draggedVertexIndex].X = e.X;
                polygon.Vertices[draggedVertexIndex].Y = e.Y;

                polygon.UpdateEdges();  // Aktualizacja kraw�dzi
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

                lastMousePosition = e.Location;
                polygon.UpdateEdges();
                polygon.ApplyConstraints();
                drawingPanel.Invalidate();
            }
        }

        private void drawingPanel_MouseUp(object sender, MouseEventArgs e)
        {
            draggingVertex = false;
            draggedVertexIndex = -1;

            draggingPolygon = false;
        }

        private void deleteVertex(MouseEventArgs e)
        {
            for (int i = 0; i < polygon.Vertices.Count; i++)
            {
                if (IsPointNearVertex(new Point(e.X, e.Y), polygon.Vertices[i].ToPoint()))
                {
                    polygon.RemoveVertexAt(i);

                    if (polygon.Vertices.Count < 3)
                    {
                        isPolygonClosed = false;
                    }

                    drawingPanel.Invalidate();
                    return;
                }
            }
        }

        private void addVertexOnTheHalf(MouseEventArgs e)
        {
            for (int i = 0; i < polygon.Edges.Count; i++)
            {
                Edge edge = polygon.Edges[i];
                if (IsPointNearEdge(new Point(e.X, e.Y), edge.Start.ToPoint(), edge.End.ToPoint()))
                {
                    Point mid = edge.MidPoint();
                    Vertex newVertex = new Vertex(mid.X,mid.Y);
                    polygon.Vertices.Insert(i + 1, newVertex); // Wstawiamy nowy wierzcho�ek mi�dzy dwa istniej�ce
                    polygon.UpdateEdges(); // Aktualizujemy kraw�dzie
                    drawingPanel.Invalidate();
                    return;
                }
            }
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

        private void ShowEdgeContextMenu(MouseEventArgs e)
        {
            // Przegl�damy wszystkie kraw�dzie, aby sprawdzi�, czy klikni�to na jak��
            for (int i = 0; i < polygon.Edges.Count; i++)
            {
                Edge edge = polygon.Edges[i];

                // Sprawdzamy, czy klikni�to blisko tej kraw�dzi
                if (IsPointNearEdge(new Point(e.X, e.Y), edge.Start.ToPoint(), edge.End.ToPoint()))
                {
                    // Je�li tak, tworzymy menu kontekstowe
                    ContextMenuStrip contextMenu = new ContextMenuStrip();
                    contextMenu.Items.Add("Set Horizontal Constraint", null, (sender, args) => SetEdgeConstraint(i, EdgeConstraint.Horizontal));
                    contextMenu.Items.Add("Set Vertical Constraint", null, (sender, args) => SetEdgeConstraint(i, EdgeConstraint.Vertical));
                    contextMenu.Items.Add("Set Fixed Length Constraint", null, (sender, args) => SetFixedLengthConstraint(i));
                    contextMenu.Items.Add("Remove Constraint", null, (sender, args) => RemoveEdgeConstraint(i));

                    // Wy�wietlamy menu kontekstowe w miejscu klikni�cia
                    contextMenu.Show(drawingPanel, new Point(e.X, e.Y));
                    return;
                }
            }
        }
        private void SetEdgeConstraint(int edgeIndex, EdgeConstraint constraint)
        {
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
            var prompt = new PromptForm("Enter fixed length:"); // Przyk�adowy formularz do wprowadzania d�ugo�ci
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



    }
}
