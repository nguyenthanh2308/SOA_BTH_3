using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderService.Migrations
{
    /// <inheritdoc />
    public partial class ClarifyOrderMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add customer_id column only if it does not exist
            migrationBuilder.Sql(@"
SET @exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='orders' AND COLUMN_NAME='customer_id');
SET @sql = IF(@exists = 0, 'ALTER TABLE `orders` ADD COLUMN `customer_id` INT NULL', 'SELECT 0');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;
");

            // Backfill customer_id where possible by matching customer_email to customers.email (case-insensitive)
            migrationBuilder.Sql(@"
UPDATE `orders` o
JOIN `customers` c ON LOWER(o.customer_email) = LOWER(c.email)
SET o.customer_id = c.id
WHERE o.customer_id IS NULL;
");

            // Create index if not exists
            migrationBuilder.Sql(@"
SET @idx = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='orders' AND INDEX_NAME='IX_orders_customer_id');
SET @sql = IF(@idx = 0, 'CREATE INDEX `IX_orders_customer_id` ON `orders`(`customer_id`)', 'SELECT 0');
PREPARE stmt2 FROM @sql;
EXECUTE stmt2;
DEALLOCATE PREPARE stmt2;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove column if it exists
            migrationBuilder.Sql(@"
SET @exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='orders' AND COLUMN_NAME='customer_id');
SET @sql = IF(@exists = 1, 'ALTER TABLE `orders` DROP COLUMN `customer_id`', 'SELECT 0');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;
");
        }
    }
}
