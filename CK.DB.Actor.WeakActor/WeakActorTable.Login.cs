using CK.Auth;
using CK.Core;
using CK.Cris;
using CK.DB.Auth;
using CK.IO.Authentication;
using CK.IO.Authentication.Weak;
using CK.SqlServer;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CK.DB.Actor.WeakActor;

/// <summary>
/// The weak actors are the actors of the "Weak" authentication provider: a weak actor logs in with its name only,
/// at the <see cref="AuthLevel.Unsafe"/> level (the "Weak" scheme caps it, see <see cref="UserInfo.IsWeak"/>).
/// <para>
/// The payload of the provider is the <see cref="IWeakLoginCommand"/> itself and this table handles it: a package that
/// implements an authentication provider handles its login commands.
/// </para>
/// </summary>
public abstract partial class WeakActorTable : IGenericAuthenticationProvider
{
    /// <summary>
    /// The provider name: "Weak" (<see cref="UserInfo.WeakScheme"/>).
    /// </summary>
    public string ProviderName => UserInfo.WeakScheme;

    /// <summary>
    /// The payload is the <see cref="IWeakLoginCommand"/>: it cannot be created here.
    /// </summary>
    public bool CanCreatePayload => false;

    object IGenericAuthenticationProvider.CreatePayload() => throw new NotSupportedException( "The payload of the Weak provider is the IWeakLoginCommand." );

    /// <summary>
    /// Handles the <see cref="IWeakLoginCommand"/>: the session is opened by the authentication center.
    /// <para>
    /// When a package extends the command (CK.IO.Authentication.WeakZone), this handler is ignored: the
    /// extending package handles the whole command.
    /// </para>
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="cmd">The command.</param>
    /// <param name="opener">The session opener.</param>
    /// <returns>The session result.</returns>
    [CommandHandler]
    public virtual ValueTask<ISessionResult> HandleWeakLoginAsync( IActivityMonitor monitor, IWeakLoginCommand cmd, IAuthenticationSessionOpener opener )
    {
        return opener.OpenSessionAsync( monitor, cmd, ProviderName, cmd );
    }

    /// <summary>
    /// Logs a weak actor in. An unknown or disabled weak actor fails with <see cref="KnownLoginFailureCode.InvalidUserKey"/>.
    /// </summary>
    /// <param name="ctx">The call context to use.</param>
    /// <param name="cmd">The login command.</param>
    /// <param name="actualLogin">False to only check the login.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The login result.</returns>
    protected virtual Task<LoginResult> LoginWeakActorAsync( ISqlCallContext ctx, IWeakLoginCommand cmd, bool actualLogin, CancellationToken cancellationToken )
    {
        return WeakActorLoginAsync( ctx, cmd.WeakActorName, actualLogin, cancellationToken );
    }

    /// <summary>
    /// Calls the login procedure.
    /// </summary>
    /// <param name="ctx">The call context to use.</param>
    /// <param name="weakActorName">The weak actor name.</param>
    /// <param name="actualLogin">False to only check the login.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The login result.</returns>
    [SqlProcedure( "sWeakActorLogin" )]
    protected abstract Task<LoginResult> WeakActorLoginAsync( ISqlCallContext ctx, string weakActorName, bool actualLogin, CancellationToken cancellationToken );

    static IWeakLoginCommand ToCommand( object payload )
    {
        Throw.CheckNotNullArgument( payload );
        return payload as IWeakLoginCommand
               ?? Throw.ArgumentException<IWeakLoginCommand>( nameof( payload ), "The payload of the Weak provider must be a IWeakLoginCommand." );
    }

    #region IGenericAuthenticationProvider explicit implementation.

    LoginResult IGenericAuthenticationProvider.LoginUser( ISqlCallContext ctx, object payload, bool actualLogin )
    {
        return LoginWeakActorAsync( ctx, ToCommand( payload ), actualLogin, default ).GetAwaiter().GetResult();
    }

    Task<LoginResult> IGenericAuthenticationProvider.LoginUserAsync( ISqlCallContext ctx, object payload, bool actualLogin, CancellationToken cancellationToken )
    {
        return LoginWeakActorAsync( ctx, ToCommand( payload ), actualLogin, cancellationToken );
    }

    // A weak actor is not an account to bind to a user: it is its own actor, created by CreateAsync.
    UCLResult IGenericAuthenticationProvider.CreateOrUpdateUser( ISqlCallContext ctx, int actorId, int userId, object payload, UCLMode mode )
    {
        throw new NotSupportedException( "A weak actor cannot be bound to a user: use CreateAsync." );
    }

    Task<UCLResult> IGenericAuthenticationProvider.CreateOrUpdateUserAsync( ISqlCallContext ctx, int actorId, int userId, object payload, UCLMode mode, CancellationToken cancellationToken )
    {
        throw new NotSupportedException( "A weak actor cannot be bound to a user: use CreateAsync." );
    }

    // There is no provider data to destroy: the weak actor itself is destroyed by DestroyAsync.
    void IGenericAuthenticationProvider.DestroyUser( ISqlCallContext ctx, int actorId, int userId, string? schemeSuffix )
    {
    }

    Task IGenericAuthenticationProvider.DestroyUserAsync( ISqlCallContext ctx, int actorId, int userId, string? schemeSuffix, CancellationToken cancellationToken )
    {
        return Task.CompletedTask;
    }

    #endregion
}
