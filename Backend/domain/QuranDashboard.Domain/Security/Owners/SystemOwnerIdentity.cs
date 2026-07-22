namespace QuranDashboard.Domain.Security.Owners;

// The immutable composite identity of a System Owner. Ordinal comparison — an owner is the SAME owner only
// when both the issuer and the subject match exactly (no case folding, no fallback).
public readonly record struct SystemOwnerIdentity(string Issuer, string Subject);
