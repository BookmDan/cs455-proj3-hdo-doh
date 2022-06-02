using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketProcessingFunction
{
    internal class DriverUpQueueMessage
    {
        public bool isFromCali { get; set; }   
        public string personName { get; set; }
        public string carColor
        { get; set; }

        public string carMake
        { get; set; }

        public string carModel
        { get; set; }

        public string prefLanguage
        { get; set; }


        public string email
        { get; set; }
    }
}
