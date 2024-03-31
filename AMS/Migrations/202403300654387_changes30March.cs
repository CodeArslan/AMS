namespace AMS.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class changes30March : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.AspNetUsers", "Gender", c => c.String());
            AddColumn("dbo.AspNetUsers", "Designation", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.AspNetUsers", "Designation");
            DropColumn("dbo.AspNetUsers", "Gender");
        }
    }
}
