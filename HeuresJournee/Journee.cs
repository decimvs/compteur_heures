using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HeuresJournee
{
    public class Journee
    {
        public int Id { get; set; }

        public string DateId { get; set; }

        public ObservableCollection<Pointage> Pointages { get; set; }
    }
}
