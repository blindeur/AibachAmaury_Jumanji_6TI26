namespace code_events_jumanji
{
    using System;
    using System.Collections.Generic;

    // -----------------------------------------------------------
    // CLASSE JOUEUR
    // -----------------------------------------------------------

    public class Joueur
    {
        public int Id { get; set; }
        public int Position { get; set; }
        public int PointsDeVie { get; set; } = 10;
        public bool EstBloque { get; set; } = false;

        public Joueur(int id)
        {
            Id = id;
        }

        public override string ToString()
        {
            return $"Joueur {Id} : Position={Position}, PV={PointsDeVie}, Bloqué={EstBloque}";
        }
    }

    // -----------------------------------------------------------
    // BASE EVENT
    // -----------------------------------------------------------

    public abstract class EventJeu
    {
        public abstract void Executer(Dictionary<int, Joueur> joueurs);
    }

    // -----------------------------------------------------------
    // ÉVÉNEMENTS INDIVIDUELS
    // -----------------------------------------------------------

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
            Console.WriteLine($"  Le joueur {joueur} recule de {cases} cases.");
        }
    }

    public class AvancerJoueurEvent : EventJeu
    {
        private int joueur;
        private int cases;

        public AvancerJoueurEvent(int joueur, int cases)
        {
            this.joueur = joueur;
            this.cases = cases;
        }

        public override void Executer(Dictionary<int, Joueur> joueurs)
        {
            joueurs[joueur].Position += cases;
            Console.WriteLine($"➡  Le joueur {joueur} avance de {cases} cases.");
        }
    }

    public class AttaqueTigreEvent : EventJeu
    {
        private int joueur;
        private int vie;

        public AttaqueTigreEvent(int joueur, int vie)
        {
            this.joueur = joueur;
            this.vie = vie;
        }

        public override void Executer(Dictionary<int, Joueur> joueurs)
        {
            joueurs[joueur].PointsDeVie -= vie;
            Console.WriteLine($" Le joueur {joueur} perd {vie} PV à cause du tigre !");
        }
    }

    public class VinesEvent : EventJeu
    {
        private int joueur;

        public VinesEvent(int joueur)
        {
            this.joueur = joueur;
        }

        public override void Executer(Dictionary<int, Joueur> joueurs)
        {
            joueurs[joueur].EstBloque = true;
            Console.WriteLine($" Le joueur {joueur} est bloqué par des vignes !");
        }
    }

    // -----------------------------------------------------------
    // ÉVÉNEMENTS GLOBAUX
    // -----------------------------------------------------------

    public class TheFloorIsLavaEvent : EventJeu
    {
        public override void Executer(Dictionary<int, Joueur> joueurs)
        {
            Console.WriteLine(" Le sol devient de la lave ! (à toi de définir les dégâts)");
        }
    }

    public class TremblementDeTerreEvent : EventJeu
    {
        public override void Executer(Dictionary<int, Joueur> joueurs)
        {
            Console.WriteLine(" Tremblement de terre ! Tous les joueurs perdent 1 PV et reculent de 3 cases.");
            foreach (var j in joueurs.Values)
            {
                j.PointsDeVie -= 1;
                j.Position -= 3;
            }
        }
    }

    // -----------------------------------------------------------
    // GESTIONNAIRE DE JEU
    // -----------------------------------------------------------

    public class Jeu
    {
        public Dictionary<int, Joueur> Joueurs = new Dictionary<int, Joueur>();

        public Jeu(int nbJoueurs)
        {
            for (int i = 1; i <= nbJoueurs; i++)
                Joueurs[i] = new Joueur(i);
        }

        public void LancerEvent(EventJeu evt)
        {
            evt.Executer(Joueurs);
        }

        public void AfficherEtat()
        {
            Console.WriteLine("\n État actuel des joueurs :");
            foreach (var j in Joueurs.Values)
                Console.WriteLine(j);
        }
    }

    // -----------------------------------------------------------
    // PROGRAMME PRINCIPAL
    // -----------------------------------------------------------

    public class Program
    {
        public static void Main()
        {
            Jeu jeu = new Jeu(2);  // 2 joueurs pour le test

            Console.WriteLine("=== TEST DES ÉVÉNEMENTS ===");

            jeu.AfficherEtat();

            // Tests
            jeu.LancerEvent(new AvancerJoueurEvent(1, 4));
            jeu.LancerEvent(new ReculerJoueurEvent(2, 2));
            jeu.LancerEvent(new AttaqueTigreEvent(1, 1));
            jeu.LancerEvent(new VinesEvent(2));
            jeu.LancerEvent(new TremblementDeTerreEvent());
            jeu.LancerEvent(new TheFloorIsLavaEvent());

            jeu.AfficherEtat();

            Console.WriteLine("\nTest terminé !");
        }
    }
}
