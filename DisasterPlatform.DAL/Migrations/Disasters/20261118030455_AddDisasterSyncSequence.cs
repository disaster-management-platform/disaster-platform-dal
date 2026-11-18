using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DisasterPlatform.DAL.Migrations.Disasters
{
    /// <inheritdoc />
    public partial class AddDisasterSyncSequence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence<long>(name: "DisasterSyncSequence", schema: "disasters");
            migrationBuilder.AddColumn<long>(
                name: "SyncSequence",
                schema: "disasters",
                table: "Desastres",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.Sql("""
                UPDATE disasters."Desastres" SET "SyncSequence" = nextval('disasters."DisasterSyncSequence"');
                CREATE FUNCTION disasters."TrackDisasterSync"() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN
                    IF TG_LEVEL = 'STATEMENT' THEN
                        PERFORM pg_catalog.pg_advisory_xact_lock(6041, 1);
                        RETURN NULL;
                    END IF;
                    NEW."SyncSequence" := pg_catalog.nextval('disasters."DisasterSyncSequence"');
                    RETURN NEW;
                END;
                $$;
                CREATE TRIGGER "DisasterSyncLock" BEFORE INSERT OR UPDATE ON disasters."Desastres"
                    FOR EACH STATEMENT EXECUTE FUNCTION disasters."TrackDisasterSync"();
                CREATE TRIGGER "DisasterSyncRevision" BEFORE INSERT OR UPDATE ON disasters."Desastres"
                    FOR EACH ROW EXECUTE FUNCTION disasters."TrackDisasterSync"();
                REVOKE ALL ON FUNCTION disasters."TrackDisasterSync"() FROM PUBLIC;
                """);
            migrationBuilder.CreateIndex(
                name: "IX_Desastres_SyncSequence",
                schema: "disasters",
                table: "Desastres",
                column: "SyncSequence",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER "DisasterSyncRevision" ON disasters."Desastres";
                DROP TRIGGER "DisasterSyncLock" ON disasters."Desastres";
                DROP FUNCTION disasters."TrackDisasterSync"();
                """);
            migrationBuilder.DropSequence(name: "DisasterSyncSequence", schema: "disasters");
            migrationBuilder.DropIndex(
                name: "IX_Desastres_SyncSequence",
                schema: "disasters",
                table: "Desastres");

            migrationBuilder.DropColumn(
                name: "SyncSequence",
                schema: "disasters",
                table: "Desastres");
        }
    }
}
