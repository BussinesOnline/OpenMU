# Player Portal public world feed

The Player Portal exposes `GET /player-portal/v1/world` through the existing
worker-authenticated API. It returns bounded character and guild rankings and
the event timetable read from the active OpenMU configuration. Event times use
`SystemConfiguration.TimeZoneId`, the same setting the GameServer uses, and the
response includes UTC instants plus the resolved time zone. The WebEngine calls
this endpoint server-side; do not expose the worker token to browsers.

The Dapr secret store must contain a `connectionStrings` entry for
`MUnique.OpenMU.Persistence.EntityFramework.GuildContext`, as shown in
`secrets.json.template`. Its database role only needs `USAGE` on schema `guild`
and `SELECT` on `guild."Guild"` and `guild."GuildMember"`. It does not need
write access or access to `data."Character"`. The endpoint reads only the guild
name, score, and member count from that context.

This endpoint adds no schema objects and requires no migration. Existing
Player Portal secrets that predate the public world feed must be updated with
the GuildContext connection string before enabling the endpoint.
