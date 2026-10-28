using CK.Core;
using CK.DB.Auth;
using CK.IO.Authentication.Weak;
using CK.IO.Authentication.WeakZone;
using CK.SqlServer;
using System.Threading;
using System.Threading.Tasks;

namespace CK.DB.Zone.WeakActor;

public abstract partial class WeakActorTable
{
    /// <inheritdoc />
    /// <remarks>
    /// The name of a weak actor is unique in its zone only: the <see cref="IWeakZoneLoginCommand"/> (the login command
    /// extended by CK.IO.Authentication.WeakZone) carries the zone. The command is handled at the endpoint that receives
    /// it (CK.Cris.Authentication.Weak), never here: this provider is called by the authentication center.
    /// </remarks>
    protected override Task<LoginResult> LoginWeakActorAsync( ISqlCallContext ctx, IWeakLoginCommand cmd, bool actualLogin, CancellationToken cancellationToken )
    {
        // With CK.IO.Authentication.WeakZone, every IWeakLoginCommand is a IWeakZoneLoginCommand.
        return WeakActorLoginAsync( ctx, cmd.WeakActorName, actualLogin, ((IWeakZoneLoginCommand)cmd).ZoneId, cancellationToken );
    }

    /// <summary>
    /// Calls the login procedure in a zone.
    /// </summary>
    /// <param name="ctx">The call context to use.</param>
    /// <param name="weakActorName">The weak actor name.</param>
    /// <param name="actualLogin">False to only check the login.</param>
    /// <param name="zoneId">The zone of the weak actor.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The login result.</returns>
    [SqlProcedure( "transform:sWeakActorLogin" )]
    protected abstract Task<LoginResult> WeakActorLoginAsync( ISqlCallContext ctx, string weakActorName, bool actualLogin, int zoneId, CancellationToken cancellationToken );
}
