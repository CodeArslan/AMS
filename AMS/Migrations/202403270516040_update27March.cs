namespace AMS.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class update27March : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ReceivedLeaveRequests", "Decision", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.ReceivedLeaveRequests", "Decision");
        }
    }
}
