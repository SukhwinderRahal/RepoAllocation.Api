namespace RepoAllocation.Api.Domain;

public sealed class AllocationValidationException : Exception
{
    public AllocationValidationException(string message)
        : base(message)
    {
    }
}