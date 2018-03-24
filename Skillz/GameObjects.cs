using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pirates;

namespace MyBot
{
    public class GameObjects
    {

        // PIRATES
        public Pirate[] MyLivingPirates { get; set; }
        public Pirate[] EnemyLivingPirates { get; set; }

        // MAP OBJECTS
        public Mothership[] MyMotherships { get; set; }
        public Mothership[] EnemyMotherships { get; set; }
        // OBJECTIVES
        public Capsule[] MyCapsules { get; set; }
        public Capsule[] EnemyCapsules { get; set; }
        public Asteroid[] LivingAsteroids { get; set; }
        public Wormhole[] AvailableWormholes { get; set; }

        public GameObjects(Pirate[] MyLivingPirates, Pirate[] EnemyLivingPirates, Capsule[] MyCapsules, Capsule[] EnemyCapsules, Mothership[] MyMotherships, Mothership[] EnemyMotherships, Asteroid[] LivingAsteroid, Wormhole[] AvailableWormholes)
        {
            this.MyLivingPirates = MyLivingPirates;
            this.EnemyLivingPirates = EnemyLivingPirates;
            this.MyCapsules = MyCapsules;
            this.EnemyCapsules = EnemyCapsules;
            this.EnemyMotherships = EnemyMotherships;
            this.MyMotherships = MyMotherships;
            this.LivingAsteroids = LivingAsteroid;
            this.AvailableWormholes = AvailableWormholes;
        }


        public List<MapObject> MyLivingPiratesAsObject()
        {
            return this.MyLivingPirates.Cast<MapObject>().ToList();
        }
        public List<MapObject> EnemyLivingPiratesAsObject()
        {
            return this.EnemyLivingPirates.Cast<MapObject>().ToList();
        }
        public List<MapObject> MyMothershipsAsObject()
        {
            return this.MyMotherships.Cast<MapObject>().ToList();
        }
        public List<MapObject> EnemyMothershipsAsObject()
        {
            return this.EnemyMotherships.Cast<MapObject>().ToList();
        }
        public List<MapObject> LivingAsteroidsAsObject()
        {
            return this.LivingAsteroids.Cast<MapObject>().ToList();
        }
        public List<MapObject> EnemyCapsulesAsObject()
        {
            return this.EnemyCapsules.Cast<MapObject>().ToList();
        }
        public List<MapObject> MyCapsulesAsObject()
        {
            return this.MyCapsules.Cast<MapObject>().ToList();
        }
        public List<MapObject> AvailableWormholesAsObject()
        {
            return this.AvailableWormholes.Cast<MapObject>().ToList();
        }
    }
}
