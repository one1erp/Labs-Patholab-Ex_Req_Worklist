using Patholab_DAL_V1;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.DynamicData;
using System.ComponentModel.DataAnnotations;

namespace Ex_Req_Worklist
{
    class ExtraRequestRow 
    {

        public ExtraRequestRow()
        {
            scannedByUser = false;
        }



        public ExtraRequestRow(string p)
        {
            // TODO: Complete member initialization
            this.p = p;
        }
        [Browsable(false)]
        public long sdgId { get; set; }
        public string Group { get; set; }
        public string SdgPatholabNumber { get; set; }
        public string SamplePatholabName { get; set; }


        public string SampleName { get; set; }
        public string AliquotPatholabName { get; set; }

        public string ExReqCreatedBy { get; set; }

        public Decimal? Priority_num
        {
            get { return priority != null ? Convert.ToDecimal(priority) : 1; }
            set { priority = value.ToString(); }
        }


        public string _Priority { get; set; }

        public DateTime? CreatedOn { get; set; }

  
        public string BlockNumber { get; set; }
        public string SlideNumber { get; set; }
        public long? ExRequestId { get; set; }
        public DateTime? ExRequestCreatedOn { get; set; }


        public string ExRequestName { get; set; }
        public string ExRequestEntityType { get; set; }
        public string ExRequestDetails { get; set; }
        [DisplayName("סטטוס הבקשה")]

        public string ExRequestStatus { get; set; }
        public string PathologName { get; set; }
        public string CuttingLaborant { get; set; }
        public string Remarks { get; set; }
        
        private bool scannedByUser;

        public string Ex_req_status { get; set; }
        public bool ScannedByUser
        {
            get { return scannedByUser; }
            set
            {
                scannedByUser = value;
                //  OnPropertyChanged("scannedByUser");
            }
        }
      
        public string PathologMacro { get; set; }
        public DateTime? PathologMacroTime { get; set; }

        private string priority;
        public static DataLayer dal { get; set; }


        public override string ToString()
        {
            return string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13}", SdgPatholabNumber, _Priority, CreatedOn, BlockNumber, SlideNumber, ExRequestDetails,
                PathologName, ExRequestCreatedOn, CuttingLaborant, Remarks, PathologMacro, PathologMacroTime,AliquotPatholabName,ExReqCreatedBy);
        }

        public string RequestType { get; set; }
        public bool Has_I_color_same_date { get; internal set; }

        private string p;

        
    }
}
