using System.Threading.Tasks;
using CK.Core;
using CK.SqlServer;

namespace CK.DB.HZone.WeakActor
{
    /// <summary>
    /// Holds the persisted <see cref="Actor.WeakActor"/>.
    /// </summary>
    [SqlTable( "tWeakActor", Package = typeof( Package ), ResourcePath = "Res" )]
    [SetupName( "CK.WeakActorTable-HZone" )]
    [Versions( "1.0.0" )]
    [SqlObjectItem( "transform:sGroupWeakActorAdd, transform:sWeakActorCreate" )]
    public abstract partial class WeakActorTable : Zone.WeakActor.WeakActorTable
    {
        void StObjConstruct( HZone.ZoneTable hZoneTable ) { }

        /// <summary>
        /// Try to find any occurence of the given WeakActorName in the whole hierarchy of provided zoneId.
        /// </summary>
        /// <param name="ctx">The call context.</param>
        /// <param name="zoneId">The Zone to lookup.</param>
        /// <param name="weakActorName">The WeakActorName candidate.</param>
        /// <returns>True when WeakActorName already exists in the hierarchy.</returns>
        [SqlScalarFunction( "fIsWeakActorNameInHierarchy" )]
        public abstract Task<bool> IsWeakActorNameInHierarchyAsync
        (
            ISqlCallContext ctx,
            int zoneId,
            string weakActorName
        );
    }
}
