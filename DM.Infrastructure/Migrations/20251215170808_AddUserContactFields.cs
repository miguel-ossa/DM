using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserContactFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // migrationBuilder.DropIndex(
            //     name: "IX_messages_ChatId",
            //     table: "messages");

            // migrationBuilder.DropIndex(
            //     name: "IX_message_deliveries_UserId",
            //     table: "message_deliveries");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "users",
                type: "varchar(254)",
                maxLength: 254,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "users",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "users",
                type: "varchar(32)",
                maxLength: 32,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "ClientMessageId",
                table: "messages",
                type: "varchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_users_Email",
                table: "users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_PhoneNumber",
                table: "users",
                column: "PhoneNumber",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_users_phone_or_email",
                table: "users",
                sql: "(PhoneNumber IS NOT NULL) OR (Email IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_messages_ChatId_ClientMessageId",
                table: "messages",
                columns: new[] { "ChatId", "ClientMessageId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_messages_ChatId_SentAt",
                table: "messages",
                columns: new[] { "ChatId", "SentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_message_deliveries_UserId_ReadAt",
                table: "message_deliveries",
                columns: new[] { "UserId", "ReadAt" });

            migrationBuilder.CreateIndex(
                name: "IX_message_deliveries_UserId_ServerSequence",
                table: "message_deliveries",
                columns: new[] { "UserId", "ServerSequence" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_Email",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_PhoneNumber",
                table: "users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_users_phone_or_email",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_messages_ChatId_ClientMessageId",
                table: "messages");

            migrationBuilder.DropIndex(
                name: "IX_messages_ChatId_SentAt",
                table: "messages");

            migrationBuilder.DropIndex(
                name: "IX_message_deliveries_UserId_ReadAt",
                table: "message_deliveries");

            migrationBuilder.DropIndex(
                name: "IX_message_deliveries_UserId_ServerSequence",
                table: "message_deliveries");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "users");

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "users");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "users");

            migrationBuilder.AlterColumn<string>(
                name: "ClientMessageId",
                table: "messages",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(64)",
                oldMaxLength: 64)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            // migrationBuilder.CreateIndex(
            //     name: "IX_messages_ChatId",
            //     table: "messages",
            //     column: "ChatId");

            // migrationBuilder.CreateIndex(
            //     name: "IX_message_deliveries_UserId",
            //     table: "message_deliveries",
            //     column: "UserId");
        }
    }
}
