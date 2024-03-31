namespace AMS.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class addLeaveBalanceData : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.AspNetUsers", "leaveBalance", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.AspNetUsers", "leaveBalance");
        }
    }
}
