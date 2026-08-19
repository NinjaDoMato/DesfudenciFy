using DesfudenciFy.Application.Abstractions;
using DesfudenciFy.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DesfudenciFy.Application.Common;

internal static class EntryPairing
{
    public static Task<Entry?> FindCompanionAsync(
        IAppDbContext db,
        Entry entry,
        CancellationToken cancellationToken = default) =>
        db.Entries.FirstOrDefaultAsync(
            e => e.Id != entry.Id && e.Amount == -entry.Amount && e.OccurredAt == entry.OccurredAt,
            cancellationToken);
}
