using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
namespace AMS.Models
{
    // You can add profile data for the user by adding more properties to your ApplicationUser class, please visit https://go.microsoft.com/fwlink/?LinkID=317594 to learn more.
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [Display(Name ="CNIC:")]
        public string CNIC {  get; set; }
        [Required]
        [Display(Name = "First Name:")]
        public string FirstName { get; set; }
        [Required]
        [Display(Name = "Last Name:")]
        public string LastName { get; set; }
        [Required]
        [Display(Name = "Address:")]

        public string Address {  get; set; }
        [Required]
        [Display(Name = "Phone:")]
        public string Phone {  get; set; }
        [Required]
        [Display(Name = "Per Hour Wage:")]
        public int perHour {  get; set; }
        public int leaveBalance { get; set; }

        public int totalPay {  get; set; }
        public Department Department { get; set; }
        [ForeignKey("Department")]
        [Display(Name ="Department:")]
        public int DepartmentId {  get; set; }

        public Card Card { get; set; }
        [ForeignKey("Card")]
        [Display(Name = "Card Code:")]
        public int CardId { get; set; }

        public bool isActive { get; set; }
        public string Gender { get; set; }
        public string Designation { get; set; }
        public string Role { get; set; }
        public string employeeNumber { get; set; }
        public bool isPasswordChanged { get; set; }
        public bool? isLabour { get; set; }
        public Shift Shift { get; set; }
        [ForeignKey("Shift")]
        [Display(Name = "Shift")]
        public int? shiftId { get; set; }
        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager)
        {
            
            // Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
            // Add custom user claims here
            return userIdentity;
        }
    }

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext()
            : base("DefaultConnection", throwIfV1Schema: false)
        {
        }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Shift> Shifts { get; set; }
        public DbSet<Labour> Labours { get; set; }
        public DbSet<Card> Cards { get; set; }
        public DbSet<Meeting> Meetings { get; set; }
        public DbSet<EmployeeHasMeeting> employeeHasMeetings { get; set; }
        public DbSet<ReceivedLeaveRequests> receivedLeaveRequests { get; set; }
        public DbSet<LeaveResponse> LeaveResponses { get; set; }
        public DbSet<Attendance> Attendance { get; set; }
        public DbSet<Payroll> Payroll { get; set; }

        public static ApplicationDbContext Create()
        {
            return new ApplicationDbContext();
        }
    }
}