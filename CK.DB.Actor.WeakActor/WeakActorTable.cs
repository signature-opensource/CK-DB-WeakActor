using CK.Core;
using CK.SqlServer;
using System.Threading.Tasks;

namespace CK.DB.Actor.WeakActor;

/// <summary>
/// Holds the persisted weak actor.
/// </summary>
[SqlTable( "tWeakActor", Package = typeof( Package ) )]
[Versions( "1.0.0, 1.1.0, 1.2.0" )]
[SqlObjectItem( "vWeakActor" )]
public abstract partial class WeakActorTable : SqlTable
{
    /// <summary>
    /// Creates a WeakActor.
    /// </summary>
    /// <param name="c">The sql call context to use.</param>
    /// <param name="actorId">The current actor identifier.</param>
    /// <param name="weakActorName">The WeakActor name to create.</param>
    [SqlProcedure( "sWeakActorCreate" )]
    public abstract Task<int> CreateAsync( ISqlCallContext c, int actorId, string weakActorName );

    /// <summary>
    /// Destroys a WeakActor by its identifier (does nothing if The WeakActor does not exist).
    /// </summary>
    /// <param name="c">The sql call context to use.</param>
    /// <param name="actorId">The current actor identifier.</param>
    /// <param name="weakActorId">The WeakActor identifier to destroy.</param>
    [SqlProcedure( "sWeakActorDestroy" )]
    public abstract Task DestroyAsync( ISqlCallContext c, int actorId, int weakActorId );

    /// <summary>
    /// Disables a WeakActor by its identifier (does nothing if the WeakActor does not exist).
    /// </summary>
    /// <param name="c">The sql call context to use.</param>
    /// <param name="actorId">The current actor identifier.</param>
    /// <param name="weakActorId">The WeakActor identifier to disable.</param>
    [SqlProcedure( "sWeakActorDisable" )]
    public abstract Task DisableAsync( ISqlCallContext c, int actorId, int weakActorId );

    /// <summary>
    /// Enables a WeakActor by its identifier (does nothing if the WeakActor does not exist).
    /// </summary>
    /// <param name="c">The sql call context to use.</param>
    /// <param name="actorId">The current actor identifier.</param>
    /// <param name="weakActorId">The WeakActor identifier to enable.</param>
    [SqlProcedure( "sWeakActorEnable" )]
    public abstract Task EnableAsync( ISqlCallContext c, int actorId, int weakActorId );

    /// <summary>
    /// Renames a WeakActor.
    /// </summary>
    /// <param name="c">The sql call context to use.</param>
    /// <param name="actorId">The current actor identifier.</param>
    /// <param name="weakActorId">The WeakActor identifier to rename.</param>
    /// <param name="weakActorName">The WeakActor new name.</param>
    [SqlProcedure( "sWeakActorRename" )]
    public abstract Task RenameAsync( ISqlCallContext c, int actorId, int weakActorId, string weakActorName );
}
