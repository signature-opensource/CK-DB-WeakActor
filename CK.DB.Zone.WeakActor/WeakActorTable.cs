using CK.Core;
using CK.SqlServer;
using System.Threading.Tasks;

namespace CK.DB.Zone.WeakActor
{
    /// <summary>
    /// Holds the persisted <see cref="Actor.WeakActor"/>.
    /// </summary>
    [SqlTable( "tWeakActor", Package = typeof( Package ), ResourcePath = "Res" )]
    [SetupName("CK.WeakActorTable-Zone")]
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
    }
}
