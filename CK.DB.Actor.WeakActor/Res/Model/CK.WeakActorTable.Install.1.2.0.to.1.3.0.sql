--[beginscript]

-- The weak actors are the actors of the "Weak" authentication provider.
alter table CK.tWeakActor add
    LastLoginTime datetime2 (2) not null
    constraint DF_CK_tWeakActor_LastLoginTime default ( '0001-01-01' );

-- The migrations from 1.0.0 and 1.1.0 created the weak actors enabled ('0001-01-01') whereas a fresh
-- installation creates them disabled: since a weak actor can log in, the safe behavior of a fresh installation wins.
alter table CK.tWeakActor drop constraint DF_CK_tWeakActor_DisableDate;
alter table CK.tWeakActor add constraint DF_CK_tWeakActor_DisableDate default ( sysutcdatetime() ) for DisableDate;

--[endscript]
