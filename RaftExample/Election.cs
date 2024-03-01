namespace RaftElection;

public enum State { Follower, Candidate, Leader, Unhealthy }

public class Election
{
    public Guid NodeId { get; set; }
    public State CurrentState { get; set; }
    public int CurrentTerm { get; set; }
    public Guid CurrentLeader { get; set; }

    public int timer;
    private readonly Random random = new();
    private readonly static List<Election> ListOfAllNodes = [];
    private readonly static Dictionary<Guid, (int, Guid)> Votes = [];
    private readonly object lockObject = new object();
    private Dictionary<string, (int, int)> logDict = [];


    public Election()
    {
        //set id
        NodeId = Guid.NewGuid();
        //everyone starts as a follower
        CurrentState = State.Follower;
        //starting term 0
        CurrentTerm = 0;
        //add the current node to the static list
        ListOfAllNodes.Add(this);
        //set timers
        ResetTimers();
    }

    public void LogToFile(string message)
    {
        lock (lockObject)
        {
            string fileName = $"{NodeId}.log";
            System.IO.File.AppendAllText(fileName, $"{DateTime.Now}: {message}\n");
        }
    }

    public void ResetTimers()
    {
        timer = random.Next(150, 300);
    }
    public void CheckState()
    {
        while (true)
        {
            CheckWhatToDoWithTheState();
        }
    }

    public void CheckWhatToDoWithTheState()
    {
        LogToFile($"timer: {timer}");
        Thread.Sleep(timer);
        switch (CurrentState)
        {
            case State.Follower:
                CurrentState = State.Candidate;
                break;
            case State.Candidate:
                StartAnElection();
                break;
            case State.Leader:
                SendOutHeartbeat("regular heartbeat", NodeId);
                break;
        }
    }

    public void StartAnElection()
    {
        //increase the term 
        CurrentTerm++;
        //current node votes for themself
        int voteCount = 0;
        //record the votes
        lock (lockObject)
        {
            foreach (var nodes in ListOfAllNodes)
            {
                if (nodes.CurrentState != State.Unhealthy && nodes.CurrentTerm <= CurrentTerm)
                {
                    var Voted = nodes.VoteForTheCurrentTerm(CurrentTerm, NodeId);

                    if (Voted)
                    {
                        voteCount++;
                    }
                    if (voteCount >= ListOfAllNodes.Count() / 2 + 1)
                    {
                        CurrentState = State.Leader;
                        LogToFile($"{NodeId} is the leader for term {CurrentTerm}");
                        SendOutHeartbeat("election ended", NodeId);
                        return;
                    }

                }

            }
        }
    }

    public int SendOutHeartbeat(string value, Guid CurrentLeader)
    {
        int success = 0;
        foreach (var nodes in ListOfAllNodes)
        {
            if (nodes != null)
            {
                if (nodes.NodeId != NodeId)
                {
                    lock (lockObject)
                    {
                        nodes.CurrentTerm = CurrentTerm;
                        nodes.CurrentState = State.Follower;
                        nodes.LogToFile($"term:{CurrentTerm} command:{value}");
                        success++;

                        ResetTimers();
                    }
                }

                nodes.CurrentLeader = CurrentLeader;
            }
        }
        return success;
    }

    public bool VoteForTheCurrentTerm(int term, Guid CandidateId)
    {
        lock (lockObject)
        {
            try
            {
                var checkVote = Votes[NodeId];
                if (term > checkVote.Item1)
                {
                    Votes[NodeId] = (term, CandidateId);
                    LogToFile($"{NodeId} voted for {CandidateId} on term {term}");
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                Votes[NodeId] = (term, CandidateId);
                LogToFile($"{NodeId} voted for {CandidateId} on term {term}");
                return true;
            }
        }
    }

    public static Guid EventualGet()
    {
        var foundAHealthyNode = ListOfAllNodes.Find(n => n.CurrentState != State.Unhealthy);
        return foundAHealthyNode.CurrentLeader;
    }

    public static Election StrongGet()
    {
        int leaderInt = 0;
        var foundLeader = ListOfAllNodes.Find(n => n.CurrentState == State.Leader);
        if (foundLeader != null)
        {
            foreach (var node in ListOfAllNodes)
            {
                if (node.CurrentLeader == foundLeader.NodeId)
                {
                    leaderInt++;
                }
            }
            if (leaderInt >= (ListOfAllNodes.Count() / 2 + 1))
            {
                return foundLeader;
            }
            else
            {
                throw new Exception("Couldn't find a leader");
            }

        }
        else
        {
            throw new Exception("Couldn't find a leader");
        }

    }

    public bool CompareVersionAndSwap(string key, int value)
    {
        if (CurrentState != State.Leader)
        {
            return false;
        }

        LogToFile($"term:{CurrentTerm} command:{value}");

        var nodesResponseCount = SendOutHeartbeat(value.ToString(), NodeId);

        if (nodesResponseCount + 1 >= ListOfAllNodes.Count() / 2 + 1)
        {
            logDict[key] = (value, CurrentTerm);
            return true;
        }
        else { return false; }
    }



    public static void MarkNodesUnhealthy(int count)
    {
        for (int i = 1; i <= count; i++)
        {
            ListOfAllNodes[i].CurrentState = State.Unhealthy;
        }
    }

    public static void MarkNodesHealthy(int count)
    {
        for (int i = 1; i <= count; i++)
        {
            ListOfAllNodes[i].CurrentState = State.Follower;
        }
    }

    public static void ClearListForTestingPurpose()
    {
        ListOfAllNodes.Clear();
    }

    public static List<Election> GetTheListofnodes()
    {
        return ListOfAllNodes;
    }

    public static Dictionary<Guid, (int, Guid)> GetTheListofVotes()
    {
        return Votes;
    }
}