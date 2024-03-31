namespace AMS.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class receiveleaverequestmodel : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ReceivedLeaveRequests",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        From = c.String(),
                        Message = c.String(),
                        Subject = c.String(),
                        Date = c.DateTime(),
                        isRead = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.ReceivedLeaveRequests");
        }
    }
}
