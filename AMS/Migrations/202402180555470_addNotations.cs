namespace AMS.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class addNotations : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Labours", "FirstName", c => c.String(nullable: false));
            AddColumn("dbo.Labours", "LastName", c => c.String(nullable: false));
            AlterColumn("dbo.Labours", "Email", c => c.String(nullable: false));
            AlterColumn("dbo.Labours", "CNIC", c => c.String(nullable: false));
            DropColumn("dbo.Labours", "Name");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Labours", "Name", c => c.String());
            AlterColumn("dbo.Labours", "CNIC", c => c.String());
            AlterColumn("dbo.Labours", "Email", c => c.String());
            DropColumn("dbo.Labours", "LastName");
            DropColumn("dbo.Labours", "FirstName");
        }
    }
}
