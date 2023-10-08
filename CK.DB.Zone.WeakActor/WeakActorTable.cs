using CK.Core;
using CK.SqlServer;
using System.Threading.Tasks;

namespace CK.DB.Zone.WeakActor
{
    /// <summary>
    /// Holds the persisted <see cref="Actor.WeakActor"/>.
    /// </summary>
    [SqlTable( "tWeakActor", Package = typeof( Package ), ResourcePath = "Res" )]
    [SetupName( "CK.WeakActorTable-Zone" )]
    [Versions( "1.0.0" )]
    [SqlObjectItem( "transform:vWeakActor, transform:sGroupWeakActorAdd" )]
    public abstract partial class WeakActorTable : Actor.WeakActor.WeakActorTable
    {
        void StObjConstruct( Zone.ZoneTable zoneTable ) { }

        /// <summary>
        /// Creates a WeakActor.
        /// </summary>
        /// <param name="c">The sql call context to use.</param>
        /// <param name="actorId">The current actor identifier.</param>
        /// <param name="weakActorName">The WeakActor name to create.</param>
        /// <param name="zoneId">The zoneId to set to the WeakActor.</param>
        [SqlProcedure( "transform:sWeakActorCreate" )]
        public abstract Task<int> CreateAsync( ISqlCallContext c, int actorId, string weakActorName, int zoneId = 0 );

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
        public abstract Task MoveZoneAsync
        (
            ISqlCallContext c,
            int actorId,
            int weakActorId,
            int newZoneId,
            WeakActorZoneMoveOption option = 0,
            string newWeakActorName = null
        );
    }
}
