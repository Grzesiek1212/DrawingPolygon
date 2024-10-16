using System.Windows.Forms;
using System.Drawing;
using System.Numerics;
namespace Gk1
{
    public partial class PolygonEditor : Form
    {
        private Polygon polygon;              // Instancja klasy Polygon
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
                        if (polygon.IsVertexPartOfBezierEdge(i))
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

            if (polygon.isclosed)
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


                if (polygon.IsPointInsidePolygon(new Point(e.X, e.Y)))
                {
                    draggingPolygon = true;
                    lastMousePosition = e.Location;
                    return;
                }
                return;
            }

            // Sprawdzamy, czy klikni�to blisko pierwszego punktu (zamkni�cie wielok�ta)
            if (!polygon.isclosed && polygon.Vertices.Count > 2 && IsCloseToFirstVertex(new Point(e.X, e.Y)))
            {
                polygon.isclosed = true;
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
                    edge.DrawBresenhamLine(g, edge.Start, edge.ControlPoint1, Color.Gray);
                    edge.DrawBresenhamLine(g, edge.ControlPoint1, edge.ControlPoint2, Color.Gray);
                    edge.DrawBresenhamLine(g, edge.ControlPoint2, edge.End, Color.Gray);
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
                    polygon.ApplyContinuityConstraints(draggedVertexIndex, e.Location);
                    polygon.UpdateEdges(-1);
                    polygon.ApplyConstraints((draggedVertexIndex-1+polygon.Vertices.Count)%polygon.Vertices.Count);
                }
                else
                {
                    // Standardowe przesuwanie
                    draggedVertex.X = e.X;
                    draggedVertex.Y = e.Y;
                    polygon.UpdateEdges(-1);  // Aktualizacja kraw�dzi
                    polygon.ApplyConstraints(draggedVertexIndex);
                }


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
                    polygon.RemoveVertexAt(i);
                    drawingPanel.Invalidate();
                    return true;
                }
            }
            return false;
        }
        private void addVertexOnTheHalf(int i)
        {
            Edge edge = polygon.Edges[i];
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
        private Edge? WhichEdgeisnear(MouseEventArgs e)
        {
            for (int i = 0; i < polygon.Edges.Count; i++)
            {
                Edge edge = polygon.Edges[i];
                if (edge.IsPointNearEdge(new Point(e.X, e.Y),10))
                {
                    return edge;
                }
            }
            return null;
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
            contextMenu.Items.Add("Add vertex", null, (sender, args) => addVertexOnTheHalf(polygon.Edges.IndexOf(edge)));
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
        private void ShowVertexContextMenu(MouseEventArgs e, int vertexIndex)
        {
            ContextMenuStrip contextMenu = new ContextMenuStrip();

            contextMenu.Items.Add("Set Continuity G0", null, (sender, args) => SetVertexContinuity(vertexIndex, ContinuityType.G0));
            contextMenu.Items.Add("Set Continuity G1", null, (sender, args) => SetVertexContinuity(vertexIndex, ContinuityType.G1));
            contextMenu.Items.Add("Set Continuity C1", null, (sender, args) => SetVertexContinuity(vertexIndex, ContinuityType.C1));
            contextMenu.Items.Add("Delete vertex", null, (sender, args) => deleteVertex(e));
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
                polygon.ApplyConstraints(edgeIndex); // Stosujemy ograniczenia na wszystkie kraw�dzie
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
                    polygon.ApplyConstraints(edgeIndex); // Zastosuj ograniczenie
                    drawingPanel.Invalidate(); // Od�wie� panel rysowania
                }
            }
        }
        private void RemoveEdgeConstraint(int edgeIndex)
        {
            polygon.RemoveEdgeConstraint(edgeIndex);
            polygon.ApplyConstraints(edgeIndex); // Stosujemy zmiany
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
        
        private void SetVertexContinuity(int vertexIndex, ContinuityType continuity)
        {
            // Sprawdzenie, czy wierzcho�ek nale�y do kraw�dzi B�ziera
            if (!polygon.IsVertexPartOfBezierEdge(vertexIndex))
            {
                MessageBox.Show("The vertex is not part of a B�zier edge.", "Constraint Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Ustawienie ci�g�o�ci dla wierzcho�ka
            var vertex = polygon.Vertices[vertexIndex];
            vertex.Continuity = continuity; // Zak�adam, �e wierzcho�ek ma w�a�ciwo�� Continuity

            // Przywracanie ci�g�o�ci, je�li wierzcho�ek by� przemieszczany
            Point newLocation = vertex.ToPoint(); // Zak�adam, �e wierzcho�ek ma w�a�ciwo�� Position

            polygon.ApplyContinuityConstraints(vertexIndex, newLocation);
            polygon.UpdateEdges(-1);  // Aktualizacja kraw�dzi
            polygon.ApplyConstraints(vertexIndex);
            drawingPanel.Invalidate();
        }
        

    }


}
