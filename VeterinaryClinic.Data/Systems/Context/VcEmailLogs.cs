using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Data
{
    /// <summary>
    /// Bang lich su gui email
    /// </summary>
    [Table("vcEmailLogs")]
    public class VcEmailLogs : BaseEntity
    {
        public VcEmailLogs()
        {
        }
        
        /// <summary>
        /// Email nhan
        /// </summary>
        [Column("to_email"), MaxLength(255)]
        public string ToEmail { get; set; }
        
        /// <summary>
        /// Tieu de
        /// </summary>
        [Column("subject"), MaxLength(255)]
        public string Subject { get; set; }
        
        /// <summary>
        /// Noi dung
        /// </summary>
        [Column("body"), MaxLength(1000)]
        public string Body { get; set; }
        
        /// <summary>
        /// Tramg thai gui
        /// </summary>
        [Column("sent_status"), MaxLength(255)]
        public string SentStatus { get; set; }
    
        /// <summary>
        /// Loi neu gui that bai
        /// </summary>
        [Column("error_message")]
        public string ErrorMessage { get; set; }
        
        /// <summary>
        /// Loai du lieu lien quan
        /// </summary>
        [Column("reference_type"), MaxLength(100)]
        public string ReferenceType { get; set; }
        
        /// <summary>
        /// Id du lieu lien quan
        /// </summary>
        [Column("reference_id")]
        public int ReferenceId { get; set; }
    }
}