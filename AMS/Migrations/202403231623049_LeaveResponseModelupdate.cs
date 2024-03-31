namespace AMS.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class LeaveResponseModelupdate : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.LeaveResponses", "Message", c => c.String());
            AddColumn("dbo.LeaveResponses", "From", c => c.DateTime(nullable: false));
            AddColumn("dbo.LeaveResponses", "To", c => c.DateTime(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.LeaveResponses", "To");
            DropColumn("dbo.LeaveResponses", "From");
            DropColumn("dbo.LeaveResponses", "Message");
        }
    }
}
