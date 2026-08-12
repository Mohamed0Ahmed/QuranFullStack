namespace QuranDashboard.Application.Abstractions.Linking;

public sealed class LinkingSourceNotFoundException(string reference)
    : Exception($"The referenced linking source dimension does not exist: {reference}.")
{
    public string Reference { get; } = reference;
}
