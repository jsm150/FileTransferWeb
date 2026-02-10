namespace FileTransferWeb.Domain.Shared;

public sealed class DomainRuleViolationException(string message) : InvalidOperationException(message);
