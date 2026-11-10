using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RaceDay.API.Data;

#nullable disable

namespace RaceDay.API.Migrations
{
    /// <summary>
    /// Adds per-event categories (many per event), remembers the category and status on each
    /// enrolment, and makes the old single Event.CategoryId optional.
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261109210000_AddEventCategoriesAndEnrolmentDetails")]
    public partial class AddEventCategoriesAndEnrolmentDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "CategoryId",
                table: "Events",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateTable(
                name: "EventCategories",
                columns: table => new
                {
                    EventCategoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Distance = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EntryFee = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MaxParticipants = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventCategories", x => x.EventCategoryId);
                    table.ForeignKey(
                        name: "FK_EventCategories_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "EventId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "Enrolments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Enrolments",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Confirmed");

            migrationBuilder.CreateIndex(
                name: "IX_EventCategories_EventId",
                table: "EventCategories",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_Enrolments_CategoryId",
                table: "Enrolments",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Enrolments_EventCategories_CategoryId",
                table: "Enrolments",
                column: "CategoryId",
                principalTable: "EventCategories",
                principalColumn: "EventCategoryId",
                onDelete: ReferentialAction.Restrict);

            // Keep existing events usable: give each one a category built from its old single category
            migrationBuilder.Sql(@"
                INSERT INTO EventCategories (EventId, Name, Distance, EntryFee, MaxParticipants)
                SELECT e.EventId,
                       LEFT(c.CategoryName, 100),
                       LEFT(e.Distance, 50),
                       0,
                       0
                FROM Events e
                INNER JOIN Categories c ON c.CategoryId = e.CategoryId;");

            // Point existing enrolments at that category so they show a real category name
            migrationBuilder.Sql(@"
                UPDATE en
                SET en.CategoryId = ec.EventCategoryId
                FROM Enrolments en
                INNER JOIN EventCategories ec ON ec.EventId = en.EventId;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Enrolments_EventCategories_CategoryId",
                table: "Enrolments");

            migrationBuilder.DropTable(
                name: "EventCategories");

            migrationBuilder.DropIndex(
                name: "IX_Enrolments_CategoryId",
                table: "Enrolments");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Enrolments");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Enrolments");

            // Events without a legacy category cannot go back to NOT NULL, so point them at the first one
            migrationBuilder.Sql(@"
                UPDATE Events SET CategoryId = (SELECT TOP 1 CategoryId FROM Categories ORDER BY CategoryId)
                WHERE CategoryId IS NULL;");

            migrationBuilder.AlterColumn<int>(
                name: "CategoryId",
                table: "Events",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
