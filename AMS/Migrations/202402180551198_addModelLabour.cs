namespace AMS.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class addModelLabour : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.AspNetUsers", "shiftId", "dbo.Shifts");
            DropIndex("dbo.AspNetUsers", new[] { "shiftId" });
            CreateTable(
                "dbo.Labours",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(),
                        Email = c.String(),
                        CNIC = c.String(),
                        perHour = c.Int(nullable: false),
                        totalPay = c.Int(nullable: false),
                        departmentId = c.Int(nullable: false),
                        shiftId = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Departments", t => t.departmentId, cascadeDelete: true)
                .ForeignKey("dbo.Shifts", t => t.shiftId)
                .Index(t => t.departmentId)
                .Index(t => t.shiftId);
            
            DropColumn("dbo.AspNetUsers", "shiftId");
        }
        
        public override void Down()
        {
            AddColumn("dbo.AspNetUsers", "shiftId", c => c.Int());
            DropForeignKey("dbo.Labours", "shiftId", "dbo.Shifts");
            DropForeignKey("dbo.Labours", "departmentId", "dbo.Departments");
            DropIndex("dbo.Labours", new[] { "shiftId" });
            DropIndex("dbo.Labours", new[] { "departmentId" });
            DropTable("dbo.Labours");
            CreateIndex("dbo.AspNetUsers", "shiftId");
            AddForeignKey("dbo.AspNetUsers", "shiftId", "dbo.Shifts", "Id");
        }
    }
}
