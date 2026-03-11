namespace VeterinaryClinic.Business
{
    public class ProcessWorkFlowModel
    {
        public int Id { get; set; }
        public string CommandName { get; set; }
        public Guid ProcessId { get; set; }
        public string Commentary { get; set; }
        public DateTime? ActionDate { get; set; }
    }
}
