using System.Collections.Generic;

namespace Pixel.iris5.shared.br
{
    public class RuleDto
    {
        public iris5.models.CORE_RULE rule { get; set; }
        public List<iris5.models.CORE_RULE_ACTION> rule_action { get; set; }
        public List<iris5.models.CORE_RULE_OPERATION> rule_operation { get; set; }
        public List<iris5.models.CORE_RULE_PROD> rule_prod { get; set; }
      
        // DELETED
        public List<iris5.models.CORE_RULE_ACTION> rule_action_deleted { get; set; }
        public List<iris5.models.CORE_RULE_OPERATION> rule_operation_deleted { get; set; }
        public List<iris5.models.CORE_RULE_PROD> rule_prod_deleted { get; set; }

        //origin
        public iris5.models.CORE_RULE rule_origin { get; set; }
        public List<iris5.models.CORE_RULE_ACTION> rule_action_origin { get; set; }
        public List<iris5.models.CORE_RULE_OPERATION> rule_operation_origin { get; set; }
        public List<iris5.models.CORE_RULE_PROD> rule_prod_origin { get; set; }
        
        // initializer
        public RuleDto()
        {
            this.rule = new iris5.models.CORE_RULE();
            this.rule_action = new List<iris5.models.CORE_RULE_ACTION>();
            this.rule_operation = new List<iris5.models.CORE_RULE_OPERATION>();
            this.rule_prod = new List<iris5.models.CORE_RULE_PROD>();           
        }
    }
}
