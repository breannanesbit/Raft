namespace RaftElection;

public enum State { Follower, Candidate, Leader }

public class Election
{
    public Guid NodeId { get; set; }
    public State CurrentState { get; set; }
    public int CurrentTerm { get; set; }
    public int timer;
    private readonly Random random = new();
    private readonly static List<Election> ListOfAllNodes = [];
    private readonly static Dictionary<Guid, (int, Guid)> Votes = [];
    private readonly object lockObject = new object();


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
                    SendOutHeartbeat();
                    break;
            }
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
                if (nodes != null && nodes.CurrentTerm <= CurrentTerm)
                {
                    nodes.VoteForTheCurrentTerm(CurrentTerm, NodeId);
                    voteCount++;
                    if (voteCount >= ListOfAllNodes.Count() / 2 + 1)
                    {
                        CurrentState = State.Leader;
                        LogToFile($"{NodeId} is the leader for term {CurrentTerm}");
                        SendOutHeartbeat();
                        return;
                    }
                }
            }
        }
    }

    public void SendOutHeartbeat()
    {
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
                        LogToFile($"Got a heartbeat for term {CurrentTerm}");
                        ResetTimers();
                    }
                }
            }
        }
    }

    public void VoteForTheCurrentTerm(int term, Guid CandidateId)
    {
        lock (lockObject)
        {
            Votes[NodeId] = (term, CandidateId);
            LogToFile($"{NodeId} voted for {CandidateId} on term {term}");
        }
    }

    public static void MarkNodesUnhealthy(int count)
    {
        for (int i = 1; i <= count; i++)
        {
            ListOfAllNodes[i] = null;
        }
    }

    public static void ClearListForTestingPurpose()
    {
        ListOfAllNodes.Clear();
    }
}