using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Mazes
{
    public class Maze : MonoBehaviour
    {
        const float X_SCALE = 8.75f;
        const float Z_SCALE = 8.75f;

        public int Rows;
        public int Cols;
        public GameObject CellObject;
        public GameObject Player;

        GraphVertex[] vertices;

        enum Direction
        {
            North, East, West, South
        }

        public void Start()
        {
            vertices = new GraphVertex[Rows * Cols];

            InitializeVertices();
            RecursiveBacktracker();
            MakeMaze();
            SpawnPlayer();
            MovePlayer();
        }

        private void SpawnPlayer()
        {
            float xPos = ((Cols * (Rows - 1) % Cols) - ((float)(Rows - 1) / 2)) * X_SCALE;
            float zPos = ((Cols * (Rows - 1) / Cols) - ((float)(Cols - 1) / 2)) * Z_SCALE;
            Player = Instantiate(Player, new Vector3(xPos, 2.05f, zPos), Quaternion.identity);
        }

        private void MovePlayer()
        {
            float xPos = (((Cols - 1) % Cols) - ((float)(Rows - 1) / 2)) * X_SCALE;
            float zPos = (((Cols - 1) / Cols) - ((float)(Cols - 1) / 2)) * Z_SCALE;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(new Vector3(xPos, 0, zPos), out hit, 1f, NavMesh.AllAreas))
            {
                Player.GetComponent<NavMeshAgent>().SetDestination(hit.position);
            }
        }

        private void InitializeVertices()
        {
            vertices = new GraphVertex[Rows * Cols];

            for (int i = 0; i < Rows * Cols; i++)
            {
                vertices[i] = new GraphVertex(i);
            }
        }

        public void MakeMaze()
        {
            for (int i = 0; i < vertices.Length; i++)
            {
                float xPos = ((i % Cols) - ((float)(Rows-1) / 2)) * X_SCALE;
                float zPos = ((i / Cols) - ((float)(Cols-1) / 2)) * Z_SCALE;
                GameObject cellObject = Instantiate(CellObject, new Vector3(xPos, 0, zPos), Quaternion.identity, gameObject.transform);
                Cell cell = cellObject.GetComponent<Cell>();
                
                foreach (GraphEdge edge in vertices[i].Edges)
                {
                    if (edge.Destination.Value == i - Cols)
                    {
                        cell.BackWall.SetActive(false);
                    }
                    if (edge.Destination.Value == i + Cols)
                    {
                        cell.FrontWall.SetActive(false);
                    }
                    if (edge.Destination.Value == i - 1)
                    {
                        cell.LeftWall.SetActive(false);
                    }
                    if (edge.Destination.Value == i + 1)
                    {
                        cell.RightWall.SetActive(false);
                    }
                }
            }
        }

        private GraphVertex GetVertexInDirection(GraphVertex origin, Direction direction)
        {
            int originRow = origin.Value / Cols;
            int originCol = origin.Value % Cols;
            int newRow = originRow;
            int newCol = originCol;
            switch(direction)
            {
                case Direction.North:
                    newRow -= 1;
                    break;
                case Direction.South:
                    newRow += 1;
                    break;
                case Direction.East:
                    newCol += 1;
                    break;
                case Direction.West:
                    newCol -= 1;
                    break;
            }

            if (newRow < 0 || newRow >= Rows || newCol < 0 || newCol >= Cols)
            {
                return null;
            }
            else
            {
                return vertices[newRow * Cols + newCol];
            }
        }

        public List<GraphVertex> GetAllUnvisitedAdjacentVerts(GraphVertex vertex)
        {
            List<GraphVertex> ret = new List<GraphVertex>();
            ret.Add(GetVertexInDirection(vertex, Direction.North));
            ret.Add(GetVertexInDirection(vertex, Direction.South));
            ret.Add(GetVertexInDirection(vertex, Direction.East));
            ret.Add(GetVertexInDirection(vertex, Direction.West));
            ret = ret.FindAll(x => x != null && x.Visited == false);

            List<GraphVertex> ret2 = new List<GraphVertex>();
            foreach (GraphVertex v in ret)
            {
                if (v != null && v.Visited == false)
                {
                    ret2.Add(v);
                }
            }

            return ret2;
        }

        public List<GraphVertex> GetAllUnvisitedConnectedVerts(GraphVertex vertex)
        {
            return vertex.Edges.Select(x => x.Destination).ToList().FindAll(x => x.Visited == false);
        }

        public void RecursiveBacktracker()
        {
            InitializeVertices();

            Stack<GraphVertex> vertStack = new Stack<GraphVertex>();
            vertStack.Push(vertices[0]);
            vertices[0].Visited = true;
            while (vertStack.Count() > 0)
            {
                // Do a "drunken walk"
                bool loopDrunkenWalk = true;
                do
                {
                    GraphVertex vertex = vertStack.Peek();
                    List<GraphVertex> possibleVerts = GetAllUnvisitedAdjacentVerts(vertex);
                    if (possibleVerts.Count > 0)
                    {
                        GraphVertex randomChoice = possibleVerts[Random.Range(0, possibleVerts.Count)];
                        randomChoice.Visited = true;
                        AddEdge(vertex, randomChoice, 1);
                        vertStack.Push(randomChoice);
                    }
                    else
                    {
                        loopDrunkenWalk = false;
                    }
                } while (loopDrunkenWalk);

                // Recurse backwards
                bool loopBacktracking = true;
                do
                {
                    GraphVertex vertex = vertStack.Peek();
                    List<GraphVertex> possibleVerts = GetAllUnvisitedAdjacentVerts(vertex);
                    if (possibleVerts.Count > 0)
                    {
                        loopBacktracking = false;
                    }
                    else
                    {
                        vertStack.Pop();
                    }
                } while (loopBacktracking && vertStack.Count > 0);
            }

            foreach(GraphVertex vertex in vertices)
            {
                vertex.Visited = false;
            }
        }

        public void AddVertex(int value)
        {
            throw new System.NotImplementedException();
        }

        public void AddEdge(GraphVertex start, GraphVertex end, float weight)
        {
            start.Edges.Add(new GraphEdge(end, weight));
            end.Edges.Add(new GraphEdge(start, weight));
        }

        public class GraphVertex
        {
            public int Value { get; set; }
            public List<GraphEdge> Edges { get; set; }
            public bool Visited;
            public GraphVertex(int value)
            {
                this.Value = value;
                this.Visited = false;
                Edges = new List<GraphEdge>();
            }
        }

        public class GraphEdge
        {
            public float Weight { get; set; }
            public GraphVertex Destination { get; set; }
            public GraphEdge(GraphVertex destination, float weight)
            {
                this.Destination = destination;
                this.Weight = weight;
            }
        }
    }
}
