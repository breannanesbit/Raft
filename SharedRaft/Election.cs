using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Timers;

namespace RaftElection;

public enum State { Follower, Candidate, Leader, Unhealthy }

public class Election
{
    private readonly HttpClient httpClient;

    public Guid NodeId { get; set; }
    public State CurrentState { get; set; }
    public int CurrentTerm { get; set; }
    public int LogIndex { get; set; }
    public Guid CurrentLeader { get; set; }
    public List<string> Urls { get; set; }

    public int timer;
    private readonly Random random = new();
    private readonly static List<Election> ListOfAllNodes = [];
    private readonly static Dictionary<Guid, (int, Guid)> Votes = [];
    private readonly object lockObject = new object();
    private readonly ILogger<Election> logger;
    private Dictionary<string, (string, int)> logDict = [];
    private readonly System.Timers.Timer startTimer;


    public Election(List<string> urls, ILogger<Election> logger)
    {
        this.httpClient = new HttpClient();

        //set id
        NodeId = Guid.NewGuid();
        //everyone starts as a follower
        CurrentState = State.Follower;
        //starting term 0
        CurrentTerm = 0;
        LogIndex = 0;
        //add the current node to the static list
        ListOfAllNodes.Add(this);
        //set timers
        ResetTimers();
        Urls = urls;
        this.logger = logger;

        startTimer = new System.Timers.Timer(timer);
        startTimer.Elapsed += CheckState;
        startTimer.Start();
    }

    public Election(ILogger<Election> logger)
    {
        this.httpClient = new HttpClient();
        //set id
        NodeId = Guid.NewGuid();
        //everyone starts as a follower
        CurrentState = State.Follower;
        //starting term 0
        CurrentTerm = 0;
        LogIndex = 0;
        //add the current node to the static list
        ListOfAllNodes.Add(this);
        //set timers
        ResetTimers();
        this.logger = logger;
    }

    public bool LogToFile(string key, string value)
    {
        //lock (lockObject)
        //{
        //    string fileName = $"{NodeId}.log";
        //    System.IO.File.AppendAllText(fileName, $"{DateTime.Now}: {message}\n");
        //}
        try
        {
            LogIndex = LogIndex + 1;
            logDict[key] = (value, LogIndex);
            logger.LogInformation($"{DateTime.Now}: value:{value}, logIndex: {LogIndex}");
            return true;
        }
        catch { return false; }
    }

    public void ResetTimers()
    {
        timer = random.Next(150, 300);
    }
    public void CheckState(object? sender, ElapsedEventArgs e)
    {
        while (true)
        {
            CheckWhatToDoWithTheStateAsync();
        }
    }

    public async Task CheckWhatToDoWithTheStateAsync()
    {
        Thread.Sleep(timer);
        switch (CurrentState)
        {
            case State.Follower:
                CurrentState = State.Candidate;
                break;
            case State.Candidate:
                StartAnElectionAsync();
                break;
            case State.Leader:
                await SendOutHeartbeatAsync("regular heartbeat", "-1", NodeId);
                break;
        }
    }

    public async Task StartAnElectionAsync()
    {
        //increase the term 
        CurrentTerm++;
        //current node votes for themself
        int voteCount = 0;
        //record the votes

        foreach (var nodes in Urls)
        {
            var Voted = await httpClient.GetFromJsonAsync<bool>($"{nodes}/getVotes/{NodeId}");

            if (Voted)
            {
                voteCount++;
            }
            if (voteCount >= Urls.Count() / 2 + 1)
            {
                CurrentState = State.Leader;
                logger.LogInformation($"{NodeId} is the leader for term {CurrentTerm}");
                //LogToFile($"{NodeId} is the leader for term {CurrentTerm}");
                await SendOutHeartbeatAsync("election ended", "-1", NodeId);
                return;
            }

        }
    }

    public async Task<int> SendOutHeartbeatAsync(string key, string value, Guid CurrentLeader)
    {
        int success = 0;
        foreach (var nodes in Urls)
        {
            if (nodes != null)
            {
                var beat = new HeartbeatInfo()
                {
                    CurrentTerm = CurrentTerm,
                    LeaderId = CurrentLeader,
                    Value = value,
                    key = key,
                };

                var response = await httpClient.GetFromJsonAsync<bool>($"{nodes}/heartbeat/from/{beat}");

                if (response)
                {
                    success++;
                }

                ResetTimers();


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
                    logger.LogInformation($"{NodeId} voted for {CandidateId} on term {term}");
                    //LogToFile($"{NodeId} voted for {CandidateId} on term {term}");
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
                logger.LogInformation($"{NodeId} voted for {CandidateId} on term {term}");
                //LogToFile($"{NodeId} voted for {CandidateId} on term {term}");
                return true;
            }
        }
    }

    public (string?, int?) EventualGet(string key)
    {
        if (logDict.TryGetValue(key, out var value))
        {
            return value;
        }
        return (null, null);
    }

    public async Task<(string?, int?)> StrongGetAsync(string key)
    {
        int leaderInt = 0;
        foreach (var node in Urls)
        {
            var SameLeader = await httpClient.GetFromJsonAsync<bool>($"{node}/compareleader/{NodeId}");
            if (SameLeader)
            {
                leaderInt++;
            }
        }
        if (leaderInt >= (Urls.Count() / 2 + 1))
        {
            if (logDict.TryGetValue(key, out var value))
            {
                return value;
            }
            else
                return (null, null);
        }
        else
        {
            return (null, null);
        }

    }

    public bool CompareVersionAndSwap(string key, int expectedIndex, string newValue)
    {
        if (logDict.TryGetValue(key, out var value) && value.Item2 <= expectedIndex)
        {
            logDict[key] = (newValue, expectedIndex);
            return true;
        }
        else { return false; }

        //if (CurrentState != State.Leader)
        //{
        //    return false;
        //}

        //LogToFile($"term:{CurrentTerm} command:{value}");

        //var nodesResponseCount = SendOutHeartbeat(value.ToString(), NodeId);

        //if (nodesResponseCount + 1 >= ListOfAllNodes.Count() / 2 + 1)
        //{
        //    logDict[key] = (value, CurrentTerm);
        //    return true;
        //}
        //else { return false; }
    }

    public async Task<bool> WriteAsync(string key, string value)
    {
        if (CurrentState != State.Leader)
        {
            return false;
        }

        //LogToFile($"term:{CurrentTerm} command:{value}");
        logger.LogInformation($"term:{CurrentTerm} command:{value}");

        var nodesResponseCount = await SendOutHeartbeatAsync(key, value, NodeId);

        if (nodesResponseCount + 1 >= Urls.Count() / 2 + 1)
        {
            LogIndex = LogIndex + 1;
            logDict[key] = (value, LogIndex);
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

    public void SetUrlsForTests()
    {
        Urls = new List<string>();
        foreach (var n in ListOfAllNodes)
        {
            Urls.Add(n.NodeId.ToString());
        }
    }


}

public class HeartbeatInfo
{
    public Guid LeaderId { get; set; }
    public string Value { get; set; }
    public string key { get; set; }
    public int CurrentTerm { get; set; }
}

public class SwapInfo
{
    public string Key { get; set; }
    public int ExpectedIndex { get; set; }
    public string NewValue { get; set; }
}

public class KeyValue
{
    public string key { get; set; }
    public string value { get; set; }
}

public class Product
{
    public string ProductItem { get; set; }
    public int Quanity { get; set; }
    public double Cost { get; set; }
}

public class Cart
{
    public Guid OrderId { get; set; }
    public string Username { get; set; }
    public List<Product> ShoppingItems { get; set; } = new();
}

public class LogInfo
{
    public string Value { get; set; }
    public int LogIndex { get; set; }
}

public class PendingOrders
{
    public Guid ProcessorId { get; set; }
    public Guid OrderId { get; set; }
    public string Status { get; set; }
    public int StatusIndex { get; set; }
    public Cart? OrderInfo { get; set; }
    public int OrderInfoIndex { get; set; }
    public string username { get; set; }
}




