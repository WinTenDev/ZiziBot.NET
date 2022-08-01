using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace WinTenDev.Zizi.Models.Types.Github
{
    public partial class GithubRoot
    {
        [JsonProperty("zen")]
        public string Zen { get; set; }

        [JsonProperty("hook_id")]
        public long HookId { get; set; }

        [JsonProperty("hook")]
        public Hook Hook { get; set; }

        [JsonProperty("repository")]
        public Repository Repository { get; set; }

        [JsonProperty("sender")]
        public Sender Sender { get; set; }

        [JsonProperty("pusher")]
        public Pusher Pusher { get; set; }

        [JsonProperty("ref")]
        public string Ref { get; set; }

        [JsonProperty("before")]
        public string Before { get; set; }

        [JsonProperty("after")]
        public string After { get; set; }

        [JsonProperty("action")]
        public string Action { get; set; }

        [JsonProperty("created")]
        public bool Created { get; set; }

        [JsonProperty("deleted")]
        public bool Deleted { get; set; }

        [JsonProperty("forced")]
        public bool Forced { get; set; }

        [JsonProperty("base_ref")]
        public object BaseRef { get; set; }

        [JsonProperty("compare")]
        public Uri Compare { get; set; }

        [JsonProperty("commits")]
        public List<Commit> Commits { get; set; }

        [JsonProperty("head_commit")]
        public Commit HeadCommit { get; set; }

        [JsonProperty("pull_request")]
        public PullRequest PullRequest { get; set; }
    }
}