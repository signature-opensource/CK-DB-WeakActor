using CK.Auth;
using CK.Core;
using CK.Cris;
using CK.DB.Auth;
using CK.IO.Authentication;
using CK.IO.Authentication.Weak;
using CK.IO.Authentication.WeakZone;
using CK.SqlServer;
using System.Threading;
using System.Threading.Tasks;

namespace CK.DB.Zone.WeakActor;

public abstract partial class WeakActorTable
{
    /// <summary>
    /// Handles the <see cref="IWeakZoneLoginCommand"/>: the name of a weak actor is unique in its zone only.
    /// The session is opened by the authentication center.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="cmd">The command.</param>
    /// <param name="opener">The session opener.</param>
    /// <returns>The session result.</returns>
    [CommandHandler]
    public virtual ValueTask<ISessionResult> HandleWeakZoneLoginAsync( IActivityMonitor monitor, IWeakZoneLoginCommand cmd, IAuthenticationSessionOpener opener )
    {
        return opener.OpenSessionAsync( monitor, cmd, ProviderName, cmd );
    }

    /// <inheritdoc />
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
