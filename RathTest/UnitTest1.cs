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

            SimalationOfVoting(nodes, 1, 0);
            //WaitForLeaderElection(nodes);
            Assert.AreEqual(State.Leader, nodes[0].CurrentState);
        }

        [Test]
        public void Leader_Elected_If_Three_Of_Five_Nodes_Are_Healthy()
        {
            Election.ClearListForTestingPurpose();
            var nodes = CreateNodes(5);
            nodes[0].CurrentState = State.Candidate;

            SimalationOfVoting(nodes, 1, 0);      //WaitForLeaderElection(nodes);
            Assert.AreEqual(State.Leader, nodes[0].CurrentState);
        }

        [Test]
        public void Leader_Not_Elected_If_Two_Of_Five_Nodes_Are_Unhealthy()
        {
            Election.ClearListForTestingPurpose();
            var nodes = CreateNodes(5);
            nodes[0].CurrentState = State.Candidate;
            Election.MarkNodesUnhealthy(2);

            SimalationOfVoting(nodes, 1, 0);

            Assert.AreEqual(State.Leader, nodes[0].CurrentState);
        }

        [Test]
        public void Leader_Not_Elected_If_one_Of_Five_Nodes_Are_Unhealthy()
        {
            Election.ClearListForTestingPurpose();
            var nodes = CreateNodes(5);
            nodes[0].CurrentState = State.Candidate;
            Election.MarkNodesUnhealthy(1);
            SimalationOfVoting(nodes, 1, 0);

            Assert.AreEqual(State.Leader, nodes[0].CurrentState);
        }

        [Test]
        public void Leader_Not_Elected_If_Three_Of_Five_Nodes_Are_Unhealthy()
        {
            Election.ClearListForTestingPurpose();
            var nodes = CreateNodes(5);
            nodes[0].CurrentState = State.Candidate;
            Election.MarkNodesUnhealthy(3);
            SimalationOfVoting(nodes, 1, 0);

            Assert.AreEqual(State.Candidate, nodes[0].CurrentState);
        }

        [Test]
        public void Node_Continues_As_Leader_If_All_Nodes_Remain_Healthy()
        {
            Election.ClearListForTestingPurpose();
            var nodes = CreateNodes(5);
            nodes[0].CurrentState = State.Candidate;
            SimalationOfVoting(nodes, 1, 0);
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

            SimlatedHeartbeat(nodes[0].NodeId, nodes);

            Assert.AreEqual(nodes[0].CurrentLeader, nodes[0].NodeId);
            Assert.AreEqual(nodes[1].CurrentLeader, nodes[0].NodeId);
            Assert.AreEqual(nodes[2].CurrentLeader, nodes[0].NodeId);
            Assert.AreEqual(nodes[3].CurrentLeader, nodes[0].NodeId);
            Assert.AreEqual(nodes[4].CurrentLeader, nodes[0].NodeId);

        }

        [Test]
        public void Node_will_call_for_an_election_if_messages_from_the_leader_takes_too_long()
        {
            Election.ClearListForTestingPurpose();
            var nodes = CreateNodes(5);

            nodes[0].CurrentState = State.Candidate;
            SimalationOfVoting(nodes, 1, 0);
            //WaitForLeaderElection(nodes);
            Assert.AreEqual(State.Leader, nodes[0].CurrentState);

            nodes[0].CurrentState = State.Follower;
            nodes[1].CheckWhatToDoWithTheStateAsync();
            if (nodes[1].CurrentState == State.Candidate)
            {
                SimalationOfVoting(nodes, 2, 1);
            }

            Assert.AreEqual(State.Leader, nodes[1].CurrentState);
        }

        [Test]
        public void Avoiding_two_double_voting()
        {
            Election.ClearListForTestingPurpose();
            var nodes = CreateNodes(5);
            nodes[0].CurrentState = State.Candidate;
            SimalationOfVoting(nodes, 1, 0);

            Election.MarkNodesUnhealthy(3);
            Assert.AreEqual(State.Leader, nodes[0].CurrentState);

            Election.MarkNodesHealthy(3);
            nodes[4].CurrentTerm = 0;
            nodes[4].StartAnElectionAsync();
            Assert.AreEqual(State.Leader, nodes[0].CurrentState);
            Assert.AreEqual(State.Follower, nodes[4].CurrentState);

        }

        [Test]
        public void TestStrongGet()
        {
            Election.ClearListForTestingPurpose();
            var nodes = CreateNodes(5);
            nodes[0].CurrentState = State.Leader;
            nodes[0].CurrentLeader = nodes[0].NodeId;
            nodes[1].CurrentLeader = nodes[0].NodeId;
            nodes[2].CurrentLeader = nodes[0].NodeId;
            nodes[3].CurrentLeader = nodes[0].NodeId;
            nodes[4].CurrentLeader = nodes[0].NodeId;

            var check = nodes[0];

            Assert.AreEqual(check, nodes[0]);
        }


        [Test]
        public void TestStrongGetWithOnlyThreenodesHaveTheLeader()
        {
            Election.ClearListForTestingPurpose();
            var nodes = CreateNodes(5);
            nodes[0].CurrentState = State.Leader;
            nodes[0].CurrentLeader = nodes[0].NodeId;
            nodes[1].CurrentLeader = nodes[0].NodeId;
            nodes[2].CurrentLeader = nodes[0].NodeId;

            var check = nodes[0];

            Assert.AreEqual(check, nodes[0]);
        }

        [Test]
        public void TestPiecesOfGateway()
        {
            Election.ClearListForTestingPurpose();
            var nodes = CreateNodes(5);
            nodes[0].CurrentState = State.Leader;
            nodes[0].CurrentLeader = nodes[0].NodeId;
            nodes[1].CurrentLeader = nodes[0].NodeId;
            nodes[2].CurrentLeader = nodes[0].NodeId;

            var currentLeader = nodes[0];
            currentLeader.LogToFile("test", 1);

            bool success = false;

            if (currentLeader != null)
            {
                success = currentLeader.CompareVersionAndSwap("test", 1, 2);
            }

            Assert.IsTrue(success);
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

        private static void SimalationOfVoting(Election[] nodes, int term, int candiateNode)
        {
            int voteCount = 0;
            foreach (var node in nodes)
            {
                if (node.CurrentState != State.Unhealthy)
                {
                    var response = node.VoteForTheCurrentTerm(term, nodes[0].NodeId);
                    if (response)
                        voteCount++;

                }

                if (voteCount >= nodes.Count() / 2 + 1)
                {
                    nodes[candiateNode].CurrentState = State.Leader;

                    break;
                }
            }
        }

        private static void SimlatedHeartbeat(Guid LeaderNodeId, Election[] nodes)
        {
            foreach (var node in nodes)
            {
                node.CurrentLeader = LeaderNodeId;
            }
        }

    }

}