using CK.Core;
using CK.SqlServer;

namespace CK.DB.Zone.WeakActor;

public abstract partial class WeakActorTable
{
    /// <summary>
    /// Creates a WeakActor.
    /// </summary>
    /// <param name="c">The sql call context to use.</param>
    /// <param name="actorId">The current actor identifier.</param>
    /// <param name="weakActorName">The WeakActor name to create.</param>
    /// <param name="zoneId">The zoneId to set to the WeakActor.</param>
    [SqlProcedure( "transform:sWeakActorCreate" )]
    public abstract int Create( ISqlCallContext c, int actorId, string weakActorName, int zoneId );

    /// <summary>
    /// Move a WeakActor into a new Zone.
    /// </summary>
    /// <param name="c">The sql call context to use.</param>
    /// <param name="actorId">The current actor identifier.</param>
    /// <param name="weakActorId">The WeakActorId to move.</param>
    /// <param name="newZoneId">The ZoneId destination.</param>
    /// <param name="option">Options that control the move. See <see cref="WeakActorZoneMoveOption"/>.</param>
    /// <param name="newWeakActorName">
    /// It can be useful to change the WeakActorName to comply with the unique constraint on the WeakActorName.
    /// A good working one would be to pass the DisplayName from CK.vWeakActor.
    /// </param>
    [SqlProcedure( "sWeakActorZoneMove" )]
    public abstract void MoveZone
    (
        ISqlCallContext c,
        int actorId,
        int weakActorId,
        int newZoneId,
        WeakActorZoneMoveOption option = 0,
        string newWeakActorName = null
    );
}
