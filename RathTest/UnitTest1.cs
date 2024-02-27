using RaftElection;

namespace RathTest
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Leader_Elected_If_Two_Of_Three_Nodes_Are_Healthy()
        {
            var nodes = CreateNodes(3);
            nodes[0].CurrentState = State.Candidate;
            nodes[0].StartAnElection();
            //WaitForLeaderElection(nodes);
            Assert.AreEqual(State.Leader, nodes[0].CurrentState);
        }

        [Test]
        public void Leader_Elected_If_Three_Of_Five_Nodes_Are_Healthy()
        {
            Election.ClearListForTestingPurpose();
            var nodes = CreateNodes(5);
            nodes[0].CurrentState = State.Candidate;
            nodes[0].StartAnElection();
            //WaitForLeaderElection(nodes);
            Assert.AreEqual(State.Leader, nodes[0].CurrentState);
        }

        [Test]
        public void Leader_Not_Elected_If_Two_Of_Five_Nodes_Are_Unhealthy()
        {
            Election.ClearListForTestingPurpose();
            var nodes = CreateNodes(5);
            nodes[0].CurrentState = State.Candidate;
            Election.MarkNodesUnhealthy(2);
            nodes[0].StartAnElection();

            Assert.AreEqual(State.Leader, nodes[0].CurrentState);
        }

        [Test]
        public void Leader_Not_Elected_If_Three_Of_Five_Nodes_Are_Unhealthy()
        {
            Election.ClearListForTestingPurpose();
            var nodes = CreateNodes(5);
            nodes[0].CurrentState = State.Candidate;
            Election.MarkNodesUnhealthy(3);
            nodes[0].StartAnElection();

            Assert.AreEqual(State.Candidate, nodes[0].CurrentState);
        }

        [Test]
        public void Node_Continues_As_Leader_If_All_Nodes_Remain_Healthy()
        {
            Election.ClearListForTestingPurpose();
            var nodes = CreateNodes(5);
            nodes[0].CurrentState = State.Candidate;
            nodes[0].StartAnElection();
            //WaitForLeaderElection(nodes);
            Assert.AreEqual(State.Leader, nodes[0].CurrentState);

            // Simulate all nodes remaining healthy
            nodes[1].CurrentState = State.Follower;
            nodes[2].CurrentState = State.Follower;
            nodes[3].CurrentState = State.Follower;
            nodes[4].CurrentState = State.Follower;

            // Wait for a while to see if the leader state changes
            Thread.Sleep(500);
            Assert.AreEqual(State.Leader, nodes[0].CurrentState);
        }


        private static Election[] CreateNodes(int count)
        {
            var nodes = new Election[count];
            for (int i = 0; i < count; i++)
            {
                nodes[i] = new Election();
            }

            //foreach (var node in nodes)
            //{
            //    var thread = new Thread(new ThreadStart(node.CheckState));
            //    thread.Start();
            //}

            return nodes;
        }



        private static void WaitForLeaderElection(Election[] nodes)
        {
            // Wait for all nodes to finish their elections
            while (Array.Exists(nodes, node => node.CurrentState != State.Leader))
            {
                Thread.Sleep(10);
            }
        }
    }

}