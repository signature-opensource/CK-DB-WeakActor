-- SetupConfig: { "Requires": "CK.sAuthUserOnLogin" }
--
-- Login of a weak actor (the "Weak" authentication provider): there is no credential, the name is the key.
-- An unknown or disabled weak actor fails with KnownLoginFailureCode.InvalidUserKey (3): a weak login never
-- answers UnregisteredUser (2) since a weak actor is never created by a login.
-- Like any provider, this always ends with CK.sAuthUserOnLogin: its extension points apply to the weak actors.
--
create procedure CK.sWeakActorLogin
(
    @WeakActorName nvarchar( 255 ),
    @ActualLogin bit,
    @UserId int output,
    @FailureCode int output,
    @FailureReason nvarchar(255) output
)
as
begin
    if @ActualLogin is null throw 50000, 'Argument.ActualLogin.MustNotBeNull', 1;

    --[beginsp]

    declare @LastLoginTime datetime2(2);
    declare @Now datetime2(2) = sysutcdatetime();

    set @UserId = 0;
    select @UserId = WeakActorId, @LastLoginTime = LastLoginTime
        from CK.tWeakActor
        where WeakActorName = @WeakActorName and WeakActorId > 0 and DisableDate = '0001-01-01';

    if @UserId = 0
    begin
        set @FailureCode = 3; -- InvalidUserKey
        set @FailureReason = null;
    end
    else
    begin
        exec CK.sAuthUserOnLogin 'Weak', @LastLoginTime, @UserId, @ActualLogin, @Now, @FailureCode output, @FailureReason output;
        if @ActualLogin = 1 and @FailureCode is null
        begin
            update CK.tWeakActor set LastLoginTime = @Now where WeakActorId = @UserId;
        end
    end

    --[endsp]
end
