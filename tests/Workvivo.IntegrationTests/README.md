# Workvivo.IntegrationTests

End-to-end tests that exercise the real HTTP pipeline against a real SQL Server, started
as a container by Testcontainers and reset between tests with Respawn.

Deliberately not an in-memory provider: the things worth testing at this level are the
things an in-memory provider does not have - the audience-key indexes, the unique
constraints, full-text search, `ExecuteUpdate`, and the SQL EF actually generates.

The fixture and the first suites arrive with the authentication phase, once there are
endpoints whose behaviour is worth asserting end to end. The project exists now so the
solution layout, package versions and CI wiring are settled before then.

**Requires Docker.** Skipped automatically in environments without it.
