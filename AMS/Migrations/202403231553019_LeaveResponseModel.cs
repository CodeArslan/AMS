namespace AMS.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class LeaveResponseModel : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.LeaveResponses",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Decision = c.String(),
                        rlrId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.ReceivedLeaveRequests", t => t.rlrId, cascadeDelete: true)
                .Index(t => t.rlrId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.LeaveResponses", "rlrId", "dbo.ReceivedLeaveRequests");
            DropIndex("dbo.LeaveResponses", new[] { "rlrId" });
            DropTable("dbo.LeaveResponses");
        }
    }
}
