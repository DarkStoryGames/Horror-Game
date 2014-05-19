//#define ASTARDEBUG
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Pathfinding.RVO {
	/** Exposes properties of an Agent class.
	  * \astarpro */
	public interface IAgent {
		/** Interpolated position of agent.
		 * Will be interpolated if the interpolation setting is enabled on the simulator.
		 */
		Vector3 InterpolatedPosition {get;}
		
		/** Position of the agent.
		 * This can be changed manually if you need to reposition the agent, but if you are reading from InterpolatedPosition, it will not update interpolated position
		 * until the next simulation step.
		 * \see Position
		 */
		Vector3 Position {get; set;}
		
		/** Desired velocity of the agent.
		 * Usually you set this once per frame. The agent will try move as close to the desired velocity as possible.
		 * Will take effect at the next simulation step.
		 */
		Vector3 DesiredVelocity {get; set;}
		/** Velocity of the agent.
		 * Can be used to set the rotation of the rendered agent.
		 * But smoothing is recommended if you do so since it might be a bit unstable when the velocity is very low.
		 * 
		 * You can set this variable manually,but it is not recommended to do so unless
		 * you have good reasons since it might degrade the quality of the simulation.
		 */
		Vector3 Velocity {get; set;}
		
		/** Locked agents will not move */
		bool Locked {get; set;}
		
		/** Radius of the agent.
		 * Agents are modelled as circles/cylinders */
		float Radius {get; set;}
		
		/** Height of the agent */
		float Height {get; set;}
		
		/** Max speed of the agent. In units per second  */
		float MaxSpeed {get; set;}
		
		/** Max distance to other agents to take them into account.
		 * Decreasing this value can lead to better performance, increasing it can lead to better quality of the simulation.
		 */
		float NeighbourDist {get; set;}
		
		/** Max number of estimated seconds to look into the future for collisions with agents.
		  * As it turns out, this variable is also very good for controling agent avoidance priorities.
		  * Agents with lower values will avoid other agents less and thus you can make 'high priority agents' by
		  * giving them a lower value.
		  */
		float AgentTimeHorizon {get; set;}
		/** Max number of estimated seconds to look into the future for collisions with obstacles */
		float ObstacleTimeHorizon {get; set;}
		
		/** Debug drawing */
		bool DebugDraw {get; set;}
		
		/** Max number of agents to take into account.
		 * Decreasing this value can lead to better performance, increasing it can lead to better quality of the simulation.
		 */
		int MaxNeighbours {get; set;}
		
		/** List of obstacle segments which were close to the agent during the last simulation step.
		 * Can be used to apply additional wall avoidance forces for example.
		 * Segments are formed by the obstacle vertex and its .next property.
		 */
		List<ObstacleVertex> NeighbourObstacles {get; }
		
		/** Teleports the agent to a new position.
		 * Just setting the position can cause strange effects when using interpolation.
		 */
		void Teleport (Vector3 pos);
	}
	
	/** RVO Agent.
	 * Handles calculation of an individual agent's velocity.
	 * \astarpro 
	 */
	public class Agent : IAgent {
	
		Vector3 smoothPos;
		
		public Vector3 Position {
			get;
			set;
		}
		
		public Vector3 InterpolatedPosition {
			get { return smoothPos; }
		}
		
		public Vector3 DesiredVelocity { get; set; }
		
		public void Teleport (Vector3 pos) {
			Position = pos;
			smoothPos = pos;
			prevSmoothPos = pos;
		}
		
		//Current values for double buffer calculation
		
		public float radius, height, maxSpeed, neighbourDist, agentTimeHorizon, obstacleTimeHorizon, weight;
		public bool locked = false;
		
		public int maxNeighbours;
		public Vector3 position, desiredVelocity, prevSmoothPos;
		
		public bool Locked {get; set;}
		public float Radius {get; set;}
		public float Height {get; set;}
		public float MaxSpeed {get; set;}
		public float NeighbourDist {get; set;}
		public float AgentTimeHorizon {get; set;}
		public float ObstacleTimeHorizon {get; set;}
		public Vector3 Velocity {get; set;}
		public bool DebugDraw {get; set;}
		
		public int MaxNeighbours {get; set;}
		
		private Vector3 velocity;
		private Vector3 newVelocity;
		
		/** Simulator which handles this agent.
		 * Used by this script as a reference and to prevent
		 * adding this agent to multiple simulations.
		 */
		public Simulator simulator;
		
		List<Agent> neighbours = new List<Agent>();
		List<float> neighbourDists = new List<float>();
		List<ObstacleVertex> obstaclesBuffered = new List<ObstacleVertex>();
		List<ObstacleVertex> obstacles = new List<ObstacleVertex>();
		List<float> obstacleDists = new List<float>();
		
		List<Line> orcaLines = new List<Line>();
		
		List<Line> projLines = new List<Line>();
		
		public List<ObstacleVertex> NeighbourObstacles {
			get {
				return obstaclesBuffered;
			}
		}
		
		public Agent (Vector3 pos) {
			MaxSpeed = 2;
			NeighbourDist = 15;
			AgentTimeHorizon = 2;
			ObstacleTimeHorizon = 2;
			Height = 5;
			Radius = 5;
			MaxNeighbours = 10;
			Locked = false;
			
			position = pos;
			Position = position;
			prevSmoothPos = position;
			smoothPos = position;
		}
		
		public void BufferSwitch () {
			// <==
			radius = Radius;
			height = Height;
			maxSpeed = MaxSpeed;
			neighbourDist = NeighbourDist;
			agentTimeHorizon = AgentTimeHorizon;
			obstacleTimeHorizon = ObstacleTimeHorizon;
			maxNeighbours = MaxNeighbours;
			desiredVelocity = DesiredVelocity;
			locked = Locked;
			
			//position = Position;
			
			// ==>
			Velocity = velocity;
			List<ObstacleVertex> tmp = obstaclesBuffered;
			obstaclesBuffered = obstacles;
			obstacles = tmp;
		}
		
		// Update is called once per frame
		public void Update () {
			velocity = newVelocity;
			
			prevSmoothPos = smoothPos;
			
			//Note the case P/p
			position = Position;
			position = position + velocity * simulator.DeltaTime;
			Position = position;
		}
		
		public void Interpolate (float t) {
			if (t == 1.0f) smoothPos = Position;
			else smoothPos = prevSmoothPos + (Position-prevSmoothPos)*t;
		}
		
		public static System.Diagnostics.Stopwatch watch1 = new System.Diagnostics.Stopwatch();
		public static System.Diagnostics.Stopwatch watch2 = new System.Diagnostics.Stopwatch();
		
		public void CalculateNeighbours () {
			neighbours.Clear ();
			neighbourDists.Clear ();
			float rangeSq;
			
			//watch1.Start ();
			if (MaxNeighbours > 0) {
				rangeSq = neighbourDist*neighbourDist;// 
				
				simulator.KDTree.GetAgentNeighbours (this, rangeSq);
			}
			//watch1.Stop ();
			
			obstacles.Clear ();
			obstacleDists.Clear ();
			
			//watch2.Start ();
			rangeSq = (obstacleTimeHorizon * maxSpeed + radius);
			rangeSq *= rangeSq;
			simulator.KDTree.GetObstacleNeighbours (this, rangeSq);
			//watch2.Stop ();
			
		}
		
		public float det (Vector3 a, Vector3 b) {
			return a.x * b.z - a.z * b.x;
		}
		
		public float det (Vector2 a, Vector2 b) {
			return a.x * b.y - a.y * b.x;
		}
		
		public void CalculateVelocity () {
			orcaLines.Clear ();
			
			if (locked) {
				newVelocity = new Vector3(0,0,0);
				return;
			}
			
			
	
			/*
			 * RVO Local Avoidance is temporarily disabled in the A* Pathfinding Project due to licensing issues.
			 * I am working to get it back as soon as possible. All agents will fall back to not avoiding other agents.
			 * Sorry for the inconvenience.
			 */
			
			// Fallback
			projLines.Clear ();
			newVelocity = Vector3.ClampMagnitude (desiredVelocity, maxSpeed);
		}
		
		private float Sqr (float v) { return v*v; }
		
		public float InsertAgentNeighbour (Agent agent, float rangeSq) {
			return 0;
		}
		
		private static float DistSqPointLineSegment(Vector3 a, Vector3 b, Vector3 c) {
			return DistSqPointLineSegment ( new Vector2(a.x,a.z), new Vector2(b.x,b.z), new Vector2(c.x,c.z));
		}
		
		private static float DistSqPointLineSegment(Vector2 a, Vector2 b, Vector2 c) {
			float r = Vector2.Dot (c - a,  b - a) / (b - a).sqrMagnitude;
			
			if (r < 0.0f)
				return (c - a).sqrMagnitude;
			else if (r > 1.0f)
				return (c - b).sqrMagnitude;
			else
				return (c - (a + r * (b - a))).sqrMagnitude;
		}
		
		public void InsertObstacleNeighbour (ObstacleVertex ob1, float rangeSq) {
		}
		
		bool LinearProgram1(List<Line> lines, int lineNo, float radius, Vector2 optVelocity, bool directionOpt, ref Vector2 result) {
			return true;
		}
		
		int LinearProgram2(List<Line> lines, float radius, Vector2 optVelocity, bool directionOpt, ref Vector2 result) {
			return 0;
		}
		
		void LinearProgram3(List<Line> lines, int numObstLines, int beginLine, float radius, ref Vector2 result) {
		}
	}
}