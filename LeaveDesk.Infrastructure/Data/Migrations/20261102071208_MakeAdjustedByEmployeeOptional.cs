using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeaveDesk.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class MakeAdjustedByEmployeeOptional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "AdjustedByEmployeeId",
                table: "LeaveBalanceAdjustments",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "AdjustedByEmployeeId",
                table: "LeaveBalanceAdjustments",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
