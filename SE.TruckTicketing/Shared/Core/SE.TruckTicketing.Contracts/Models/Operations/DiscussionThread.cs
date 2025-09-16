using System.Collections.Generic;

namespace SE.TruckTicketing.Contracts.Models.Operations;

public class DiscussionThread
{
    public string ThreadId { get; set; }

    public List<Note> Notes { get; set; }
}
