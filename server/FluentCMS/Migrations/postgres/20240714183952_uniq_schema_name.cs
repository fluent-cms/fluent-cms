using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FluentCMS.Migrations.postgres
{
    /// <inheritdoc />
    public partial class uniq_schema_name : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX___schemas_Name",
                table: "__schemas",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX___schemas_Name",
                table: "__schemas");
        }
    }
}
