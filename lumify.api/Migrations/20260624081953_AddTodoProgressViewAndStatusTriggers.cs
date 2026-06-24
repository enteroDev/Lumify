using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace lumify.api.Migrations
{
    /// <inheritdoc />
    public partial class AddTodoProgressViewAndStatusTriggers : Migration
    {
        // The recompute rule, shared by all three triggers:
        //   - list has no active entries        -> Status 1 (pending)
        //   - list has active entries, all done -> Status 2 (done)
        //   - otherwise (>=1 open entry)         -> Status 1 (pending)
        // "active" = not soft-deleted (DeletedAt IS NULL); "done" = Status = 2.

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // --- VIEW: per-list progress aggregation (read-only) --- //
            migrationBuilder.Sql(@"
                CREATE OR REPLACE VIEW vw_todolist_progress AS
                SELECT
                    tl.ID          AS TodoListID,
                    tl.Name        AS Name,
                    tl.OwnerID     AS OwnerID,
                    tl.WorkspaceID AS WorkspaceID,
                    tl.Status      AS ListStatus,
                    COUNT(te.ID)                                                 AS TotalEntries,
                    COALESCE(SUM(CASE WHEN te.Status = 2 THEN 1 ELSE 0 END), 0)  AS DoneEntries,
                    COALESCE(SUM(CASE WHEN te.Status <> 2 THEN 1 ELSE 0 END), 0) AS OpenEntries,
                    CASE
                        WHEN COUNT(te.ID) = 0 THEN 0
                        ELSE ROUND(100.0 * SUM(CASE WHEN te.Status = 2 THEN 1 ELSE 0 END) / COUNT(te.ID))
                    END AS CompletionPercent
                FROM TodoList tl
                LEFT JOIN TodoEntry te
                    ON te.TodoListID = tl.ID
                    AND te.DeletedAt IS NULL
                WHERE tl.DeletedAt IS NULL
                GROUP BY tl.ID, tl.Name, tl.OwnerID, tl.WorkspaceID, tl.Status;
            ");

            // --- TRIGGERS: keep TodoList.Status in sync with its entries --- //

            // A new entry is always pending -> a previously completed list must reopen.
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_todoentry_after_insert;");
            migrationBuilder.Sql(@"
                CREATE TRIGGER trg_todoentry_after_insert
                AFTER INSERT ON TodoEntry
                FOR EACH ROW
                UPDATE TodoList
                SET Status = CASE
                    WHEN (SELECT COUNT(*) FROM TodoEntry te
                          WHERE te.TodoListID = NEW.TodoListID AND te.DeletedAt IS NULL) = 0 THEN 1
                    WHEN (SELECT COUNT(*) FROM TodoEntry te
                          WHERE te.TodoListID = NEW.TodoListID AND te.DeletedAt IS NULL AND te.Status <> 2) = 0 THEN 2
                    ELSE 1
                END
                WHERE ID = NEW.TodoListID;
            ");

            // Catches status check/uncheck AND soft-deletes (DeletedAt is set via UPDATE).
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_todoentry_after_update;");
            migrationBuilder.Sql(@"
                CREATE TRIGGER trg_todoentry_after_update
                AFTER UPDATE ON TodoEntry
                FOR EACH ROW
                UPDATE TodoList
                SET Status = CASE
                    WHEN (SELECT COUNT(*) FROM TodoEntry te
                          WHERE te.TodoListID = NEW.TodoListID AND te.DeletedAt IS NULL) = 0 THEN 1
                    WHEN (SELECT COUNT(*) FROM TodoEntry te
                          WHERE te.TodoListID = NEW.TodoListID AND te.DeletedAt IS NULL AND te.Status <> 2) = 0 THEN 2
                    ELSE 1
                END
                WHERE ID = NEW.TodoListID;
            ");

            // Safety net for hard deletes (the app soft-deletes, but this keeps the rule complete).
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_todoentry_after_delete;");
            migrationBuilder.Sql(@"
                CREATE TRIGGER trg_todoentry_after_delete
                AFTER DELETE ON TodoEntry
                FOR EACH ROW
                UPDATE TodoList
                SET Status = CASE
                    WHEN (SELECT COUNT(*) FROM TodoEntry te
                          WHERE te.TodoListID = OLD.TodoListID AND te.DeletedAt IS NULL) = 0 THEN 1
                    WHEN (SELECT COUNT(*) FROM TodoEntry te
                          WHERE te.TodoListID = OLD.TodoListID AND te.DeletedAt IS NULL AND te.Status <> 2) = 0 THEN 2
                    ELSE 1
                END
                WHERE ID = OLD.TodoListID;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_todoentry_after_insert;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_todoentry_after_update;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_todoentry_after_delete;");
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_todolist_progress;");
        }
    }
}
