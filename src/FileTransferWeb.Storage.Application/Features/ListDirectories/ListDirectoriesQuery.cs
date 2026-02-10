using MediatR;

namespace FileTransferWeb.Storage.Application.Features.ListDirectories;

public sealed record ListDirectoriesQuery(string? RelativePath) : IRequest<ListDirectoriesResult>;
