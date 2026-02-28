using System;

namespace ReceptionistAgent.Core.Models;

public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
    public Guid SessionId { get; set; }
}

public class ChatResponse
{
    public string Response { get; set; } = string.Empty;
    public Guid SessionId { get; set; }
}
