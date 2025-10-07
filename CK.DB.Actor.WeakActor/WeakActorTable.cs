using CK.Core;
using CK.SqlServer;
using System.Threading.Tasks;

namespace CK.DB.Actor.WeakActor;

/// <summary>
/// Holds the persisted <see cref="Actor.WeakActor"/>.
/// </summary>
[SqlTable( "tWeakActor", Package = typeof( Package ) )]
[Versions( "1.0.0, 1.1.0" )]
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
    /// Adds a WeakActor into a Group.
    /// Throws if input WeakActorId is not a WeakActor.
    /// </summary>
    /// <param name="c">The sql call context to use.</param>
    /// <param name="actorId">The current actor identifier.</param>
    /// <param name="groupId">The group identifier.</param>
    /// <param name="weakActorId">The WeakActor identifier to add.</param>
    /// <returns>An awaitable.</returns>
    [SqlProcedure( "sGroupWeakActorAdd" )]
    public abstract Task AddIntoGroupAsync( ISqlCallContext c, int actorId, int groupId, int weakActorId );

    /// <summary>
    /// Archives a WeakActor by its identifier (does nothing if The WeakActor does not exist).
    /// </summary>
    /// <param name="c">The sql call context to use.</param>
    /// <param name="actorId">The current actor identifier.</param>
    /// <param name="weakActorId">The WeakActor identifier to archive.</param>
    [SqlProcedure( "sWeakActorArchive" )]
    public abstract Task ArchiveAsync( ISqlCallContext c, int actorId, int weakActorId );

    /// <summary>
    /// Restores a WeakActor by its identifier (does nothing if The WeakActor does not exist).
    /// </summary>
    /// <param name="c">The sql call context to use.</param>
    /// <param name="actorId">The current actor identifier.</param>
    /// <param name="weakActorId">The WeakActor identifier to restore.</param>
    [SqlProcedure( "sWeakActorRestore" )]
    public abstract Task RestoreAsync( ISqlCallContext c, int actorId, int weakActorId );

    /// <summary>
    /// Creates a WeakActor.
    /// </summary>
    /// <param name="c">The sql call context to use.</param>
    /// <param name="actorId">The current actor identifier.</param>
    /// <param name="weakActorName">The WeakActor new name.</param>
    [SqlProcedure( "sWeakActorRename" )]
    public abstract Task RenameAsync( ISqlCallContext c, int actorId, int weakActorId, string weakActorName );
}
