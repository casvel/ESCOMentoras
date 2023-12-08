using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace DotNetCoreSqlDb.Models
{
    public class Profile
    {
        public int ID { get; set; }
        public required string AccountId { get; set; }
        public required string IdentityProvider { get; set; }
        public ProfileState State {  get; set; } 

        [DisplayName("Created Date")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime CreatedDate { get; set; }
    }

    public enum ProfileState
    {
        New = 0, 
        Validating = 1,
        Validated = 2
    }
}
