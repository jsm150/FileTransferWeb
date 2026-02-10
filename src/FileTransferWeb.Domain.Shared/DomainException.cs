namespace FileTransferWeb.Domain.Shared;

public abstract class DomainException(string message) : Exception(message);
