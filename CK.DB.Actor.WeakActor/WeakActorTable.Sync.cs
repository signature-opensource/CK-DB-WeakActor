using CK.Core;
using CK.SqlServer;

namespace CK.DB.Actor.WeakActor;

public abstract partial class WeakActorTable
{
    /// <summary>
    /// Creates a WeakActor.
    /// </summary>
    /// <param name="c">The sql call context to use.</param>
    /// <param name="actorId">The current actor identifier.</param>
    /// <param name="weakActorName">The WeakActor name to create.</param>
    [SqlProcedure( "sWeakActorCreate" )]
    public abstract int Create( ISqlCallContext c, int actorId, string weakActorName );

    /// <summary>
    /// Destroys a WeakActor by its identifier (does nothing if The WeakActor does not exist).
    /// </summary>
    /// <param name="c">The sql call context to use.</param>
    /// <param name="actorId">The current actor identifier.</param>
    /// <param name="weakActorId">The WeakActor identifier to destroy.</param>
    [SqlProcedure( "sWeakActorDestroy" )]
    public abstract void Destroy( ISqlCallContext c, int actorId, int weakActorId );

}
