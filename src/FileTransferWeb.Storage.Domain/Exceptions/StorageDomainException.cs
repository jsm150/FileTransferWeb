using FileTransferWeb.Domain.Shared;

namespace FileTransferWeb.Storage.Domain.Exceptions;

public sealed class StorageDomainException(string message) : DomainException(message);
