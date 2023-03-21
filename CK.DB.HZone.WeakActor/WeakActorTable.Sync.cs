using CK.Core;
using CK.SqlServer;

namespace CK.DB.HZone.WeakActor
{
    public abstract partial class WeakActorTable
    {
        /// <summary>
        /// Try to find any occurence of the given WeakActorName in the whole hierarchy of provided zoneId.
        /// </summary>
        /// <param name="ctx">The call context.</param>
        /// <param name="zoneId">The Zone to lookup.</param>
        /// <param name="weakActorName">The WeakActorName candidate.</param>
        /// <returns>True when WeakActorName already exists in the hierarchy.</returns>
        [SqlScalarFunction( "fIsWeakActorNameInHierarchy" )]
        public abstract bool IsWeakActorNameInHierarchy
        (
            ISqlCallContext ctx,
            int zoneId,
            string weakActorName
        );
    }
}
