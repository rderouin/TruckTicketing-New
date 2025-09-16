using System;
using System.Collections.Generic;

namespace SE.TruckTicketing.UI.ViewModels;

public class DiscussionThreadViewModel
{
    public List<NoteViewModel> discussionThread;

    public DiscussionThreadViewModel()
    {
        discussionThread = new();
    }

    public string ThreadId { get; set; }
}

public class NoteViewModel
{
    public Guid Id { get; set; }

    public string Comment { get; set; }

    public string CreatedBy { get; set; }

    public string CreatedById { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public string UpdatedById { get; set; }

    public string UpdatedBy { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public string ThreadId { get; set; }

    public string OriginalComment { get; set; }
}
