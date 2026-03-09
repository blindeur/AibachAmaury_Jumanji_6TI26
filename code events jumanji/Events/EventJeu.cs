using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace code_events_jumanji.Events
{
   
        public abstract class EventJeu
        {
            public abstract void Executer(Dictionary<int, Joueur> joueurs);
        }
    
}
