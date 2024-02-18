namespace AMS.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class addAdditionalColumninRegister : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.AspNetUsers", "FirstName", c => c.String());
            AddColumn("dbo.AspNetUsers", "LastName", c => c.String());
            AddColumn("dbo.AspNetUsers", "shiftId", c => c.Int());
            CreateIndex("dbo.AspNetUsers", "shiftId");
            AddForeignKey("dbo.AspNetUsers", "shiftId", "dbo.Shifts", "Id", cascadeDelete: false);

        }

        public override void Down()
        {
            DropForeignKey("dbo.AspNetUsers", "shiftId", "dbo.Shifts");
            DropIndex("dbo.AspNetUsers", new[] { "shiftId" });
            DropColumn("dbo.AspNetUsers", "shiftId");
            DropColumn("dbo.AspNetUsers", "LastName");
            DropColumn("dbo.AspNetUsers", "FirstName");
        }
    }
}
