namespace SeasonEnded.Api.Identity;

public sealed class BootstrapConflictException(string message) : Exception(message);
