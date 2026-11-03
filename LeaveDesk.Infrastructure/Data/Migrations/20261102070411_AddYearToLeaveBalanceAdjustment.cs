using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeaveDesk.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddYearToLeaveBalanceAdjustment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Year",
                table: "LeaveBalanceAdjustments",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Year",
                table: "LeaveBalanceAdjustments");
        }
    }
}
