using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace code_events_jumanji.Core
{
    public class Jeu
    {
        public Dictionary<int, Joueur> Joueurs = new Dictionary<int, Joueur>();

        public Jeu(int nombreJoueurs)
        {
            for (int i = 1; i <= nombreJoueurs; i++)
                Joueurs[i] = new Joueur(i);
        }

        public void LancerEvent(EventJeu evt)
        {
            evt.Executer(Joueurs);
        }
    }
}
