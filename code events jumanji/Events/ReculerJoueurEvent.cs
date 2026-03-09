using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace code_events_jumanji.Events
{
    public class ReculerJoueurEvent : EventJeu
    {
        private int joueur;
        private int cases;

        public ReculerJoueurEvent(int joueur, int cases)
        {
            this.joueur = joueur;
            this.cases = cases;
        }

        public override void Executer(Dictionary<int, Joueur> joueurs)
        {
            joueurs[joueur].Position -= cases;
            Console.WriteLine($"Le joueur {joueur} recule de {cases} cases.");
        }
    }

}
