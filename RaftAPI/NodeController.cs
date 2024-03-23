using Microsoft.AspNetCore.Mvc;
using RaftElection;

namespace RaftAPI
{
    // NodeController.cs



    [ApiController]
    [Route("[controller]")]
    public class NodeController : ControllerBase
    {
        private readonly Election _nodeService;

        public NodeController(Election nodeService)
        {
            _nodeService = nodeService;
        }

        //[HttpGet("startNode")]
        //public void StartNode()
        //{
        //    _nodeService.CheckState();
        //}

        [HttpGet("leader")]
        public bool FoundLeader()
        {
            if (_nodeService.CurrentState == State.Leader)
            {
                return true;
            }
            else
                return false;
        }

        [HttpGet("compareleader/{leaderguid}")]
        public bool FoundLeader(Guid leaderguid)
        {
            if (leaderguid == _nodeService.CurrentLeader)
            {
                return true;
            }
            else
                return false;
        }

        [HttpPost("write")]
        public async Task<bool> WriteAsync(KeyValue pair)
        {
            var response = await _nodeService.WriteAsync(pair.key, pair.value);
            return response;
        }

        [HttpGet("heartbeat/from/{heartbeat}")]
        public bool HeartbeatReceiving(HeartbeatInfo heartbeat)
        {
            _nodeService.CurrentTerm = heartbeat.CurrentTerm;
            _nodeService.CurrentState = State.Follower;
            _nodeService.CurrentLeader = heartbeat.LeaderId;
            var response = _nodeService.LogToFile(heartbeat.key, heartbeat.Value);
            return response;
        }


        [HttpGet("getVotes/{guid}/{currentTerm}")]
        public bool GetVotes(Guid guid, int currentTerm)
        {
            if (_nodeService.CurrentState != State.Unhealthy && _nodeService.CurrentTerm <= currentTerm)
            {
                return _nodeService.VoteForTheCurrentTerm(currentTerm, guid);
            }
            else
                return false;
        }

        [HttpGet("eventalGet/{key}")]
        public async Task<(string?, int?)> EventualGet(string key)
        {
            return _nodeService.EventualGet(key);
        }

        [HttpGet("strongGet/{key}")]
        public async Task<(string?, int?)> StrongGet(string key)
        {
            return await _nodeService.StrongGetAsync(key);
        }

        [HttpPost("compareandswap")]
        public bool CompareAndSwap(SwapInfo swap)
        {
            return _nodeService.CompareVersionAndSwap(swap.Key, swap.ExpectedIndex, swap.NewValue);
        }

    }
}
