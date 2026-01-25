using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VeterinaryClinic.Shared
{
    /// <summary>
    /// Base entity class for workflow-enabled entities
    /// Inherits from BaseEntity and adds workflow-specific fields
    /// </summary>
    public class BaseWorkflowEntity: BaseEntity
    {
        /// <summary>
        /// ID của người tạo/khởi tạo quy trình
        /// </summary>
        [Column("author_id")]
        public string AuthorId { get; set; }

        /// <summary>
        /// ID của quy trình workflow
        /// </summary>
        [Column("process_id")]
        public Guid? ProcessId { get; set; }

        /// <summary>
        /// Trạng thái hiện tại trong quy trình
        /// </summary>
        [Column("state")]
        public string State { get; set; }

        /// <summary>
        /// Tên trạng thái hiện tại trong quy trình
        /// </summary>
        [Column("state_name")]
        public string StateName { get; set; }

        /// <summary>
        /// Đánh dấu có phải trạng thái cuối cùng hay không
        /// </summary>
        [Column("is_final_state")]
        public bool IsFinalState { get; set; } = false;
    }
    
    public class BaseEntity: TrackedChangeEntity
    {
        [Column("order", Order = 100)]
        public int Order { get; set; } = 0;

        [Column("is_active", Order = 101)]
        public bool IsActive { get; set; } = true;
    }

    public class TrackedChangeEntity
    {
        [Key]
        [Column("id", Order = 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("created_date", Order = 103)]
        public DateTime? CreatedDate { get; set; } = DateTime.Now;

        [Column("created_user_id", Order = 104)]
        public int? CreatedUserId { get; set; }

        [Column("created_user_name", Order = 105)]
        public string CreatedUserName { get; set; }

        [Column("modified_date", Order = 106)]
        public DateTime? ModifiedDate { get; set; }

        [Column("modified_user_id", Order = 107)]
        public int? ModifiedUserId { get; set; }

        [Column("modified_user_name", Order = 108)]
        public string ModifiedUserName { get; set; }
    }
}
