namespace VeterinaryClinic.Business
{
    public class ProcessWorkFlowCommandModel
    {
        public string key { get; set; }
        public string value { get; set; }
        public OptimaJet.Workflow.Core.Model.TransitionClassifier Classifier { get; set; }
        public List<string> Params { get; set; }
    }
}