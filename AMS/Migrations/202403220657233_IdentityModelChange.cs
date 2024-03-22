namespace AMS.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class IdentityModelChange : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.AspNetUsers", "CardId", c => c.Int(nullable: false));
            AddColumn("dbo.AspNetUsers", "isActive", c => c.Boolean(nullable: false));
            CreateIndex("dbo.AspNetUsers", "CardId");
            AddForeignKey("dbo.AspNetUsers", "CardId", "dbo.Cards", "Id", cascadeDelete: true);
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.AspNetUsers", "CardId", "dbo.Cards");
            DropIndex("dbo.AspNetUsers", new[] { "CardId" });
            DropColumn("dbo.AspNetUsers", "isActive");
            DropColumn("dbo.AspNetUsers", "CardId");
        }
    }
}
