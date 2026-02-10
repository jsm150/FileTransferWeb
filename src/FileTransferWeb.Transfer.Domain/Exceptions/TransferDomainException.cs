using FileTransferWeb.Domain.Shared;

namespace FileTransferWeb.Transfer.Domain.Exceptions;

public sealed class TransferDomainException(string message) : DomainException(message);
