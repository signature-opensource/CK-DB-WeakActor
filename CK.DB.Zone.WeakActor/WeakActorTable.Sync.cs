using CK.Core;
using CK.SqlServer;

namespace CK.DB.Zone.WeakActor
{
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
    }
}
