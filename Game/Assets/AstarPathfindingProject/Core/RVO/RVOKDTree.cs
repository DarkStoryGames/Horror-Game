using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace Pathfinding.RVO {
	/** KD-Tree implementation for rvo agents.
	  * \see Pathfinding.RVO.Simulator
	  * 
	  * \astarpro 
	  */
	public class KDTree {
		
		const int MAX_LEAF_SIZE = 10;
		ObstacleTreeNode obstacleRoot;
		private AgentTreeNode[] agentTree;
		
		private List<Agent> agents;
		
		private Simulator simulator;
		
		private bool rebuildAgents = false;
		
		public KDTree (Simulator simulator) {
			this.simulator = simulator;
			//agentTree = new List<AgentTreeNode>();
			agentTree = new AgentTreeNode[0];
		}
		
		public void RebuildAgents () {
			rebuildAgents = true;
		}
		
		public void BuildAgentTree () {
			//Dummy to avoid compiler warnings
			obstacleRoot = new ObstacleTreeNode();
			obstacleRoot.right = obstacleRoot;
			obstacleRoot.left = obstacleRoot;
			obstacleRoot = null;
			
			List<Agent> ag = simulator.GetAgents ();
			if (agents == null || agents.Count != ag.Count || rebuildAgents) {
				rebuildAgents = false;
				if (agents == null) agents = new List<Agent>(ag.Count);
				else agents.Clear ();
				
				agents.AddRange (simulator.GetAgents());
			}
			
			if (agentTree.Length != agents.Count*2) {
				agentTree = new AgentTreeNode[agents.Count*2];
				for (int i=0;i<agentTree.Length;i++) agentTree[i] = new AgentTreeNode();
			}
			
			if (agents.Count != 0) {
				BuildAgentTreeRecursive (0,agents.Count, 0);
			}
		}
		
		private void BuildAgentTreeRecursive (int start, int end, int node) {
			
		}
		
		
		public void BuildObstacleTree () {
		}
		
		private int countDepth (ObstacleTreeNode node) {
			if (node == null) return 0;
			return 1 + countDepth (node.left) + countDepth (node.right);
		}
		
		private ObstacleTreeNode BuildObstacleTreeRecursive (List<ObstacleVertex> obstacles) {
			return null;
		}
		
		
		public void GetAgentNeighbours (Agent agent, float rangeSq) {
			QueryAgentTreeRecursive (agent, ref rangeSq, 0);
		}
		
		public void GetObstacleNeighbours (Agent agent, float rangeSq) {
			QueryObstacleTreeRecursive (agent, rangeSq, obstacleRoot);
		}
		
		private float Sqr (float v) { return v*v; }
		
		private void QueryAgentTreeRecursive (Agent agent, ref float rangeSq, int node) {
		}
		
		private void QueryObstacleTreeRecursive (Agent agent, float rangeSq, ObstacleTreeNode node) {
		}
		
		private struct AgentTreeNode {
			public int start, end, left, right;
			public float xmax,ymax, xmin, ymin;
		}
		
		private class ObstacleTreeNode {
			public ObstacleTreeNode left, right;
			public ObstacleVertex obstacle;
		}
		
		private static Stack<ObstacleTreeNode> OTNPool = new Stack<ObstacleTreeNode>();
		private static ObstacleTreeNode GetOTN () {
			if (OTNPool.Count > 0) {
				return OTNPool.Pop ();
			} else {
				return new ObstacleTreeNode ();
			}
		}
		
		private static void RecycleOTN (ObstacleTreeNode node) {
			if (node == null) return;
			OTNPool.Push (node);
			node.obstacle = null;
			RecycleOTN (node.left);
			RecycleOTN (node.right);
		}
	}
}