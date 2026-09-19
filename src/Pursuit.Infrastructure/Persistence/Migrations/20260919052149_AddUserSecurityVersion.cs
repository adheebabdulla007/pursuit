using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pursuit.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserSecurityVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SecurityVersion",
                table: "Users",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            // Existing users receive distinct versions. The temporary default only
            // makes the non-null column add possible on populated databases.
            migrationBuilder.Sql("UPDATE [Users] SET [SecurityVersion] = NEWID() WHERE [SecurityVersion] = '00000000-0000-0000-0000-000000000000'");
            migrationBuilder.AlterColumn<Guid>(
                name: "SecurityVersion",
                table: "Users",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldDefaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SecurityVersion",
                table: "Users");
        }
    }
}
