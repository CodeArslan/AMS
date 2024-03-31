namespace AMS.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class MeetingModel : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.EmployeeHasMeetings",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        meetingId = c.Int(nullable: false),
                        employeeId = c.String(maxLength: 128),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.AspNetUsers", t => t.employeeId)
                .ForeignKey("dbo.Meetings", t => t.meetingId, cascadeDelete: true)
                .Index(t => t.meetingId)
                .Index(t => t.employeeId);
            
            CreateTable(
                "dbo.Meetings",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Date = c.DateTime(nullable: false),
                        StartTime = c.Time(nullable: false, precision: 7),
                        EndTime = c.Time(nullable: false, precision: 7),
                        Agenda = c.String(nullable: false),
                        Location = c.String(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.EmployeeHasMeetings", "meetingId", "dbo.Meetings");
            DropForeignKey("dbo.EmployeeHasMeetings", "employeeId", "dbo.AspNetUsers");
            DropIndex("dbo.EmployeeHasMeetings", new[] { "employeeId" });
            DropIndex("dbo.EmployeeHasMeetings", new[] { "meetingId" });
            DropTable("dbo.Meetings");
            DropTable("dbo.EmployeeHasMeetings");
        }
    }
}
