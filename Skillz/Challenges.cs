using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pirates;

namespace MyBot
{
    #region Enum
    public enum Position { UP, RIGHT, DOWN, LEFT, UP_RIGHT, DOWN_RIGHT, DOWN_LEFT, UP_LEFT, SAME_SPOT }
    public enum Role { FRONT_LINE, BACK_LINE }

    #endregion
    static class Challenges
    {
        public static bool switchedThisTurn = false;
        public static bool stickedThisTurn = false;
        public static void SailMezik<T>(this T pir, MapObject destination)
        {
            /*
                  _____    ___       _      ____    _____   _____   ____    ___   _____   _     
                 |_   _|  / _ \     / \    / ___|  |_   _| | ____| |  _ \  |_ _| | ____| | |    
                   | |   | | | |   / _ \   \___ \    | |   |  _|   | |_) |  | |  |  _|   | |    
                   | |   | |_| |  / ___ \   ___) |   | |   | |___  |  _ <   | |  | |___  | |___ 
                   |_|    \___/  /_/   \_\ |____/    |_|   |_____| |_| \_\ |___| |_____| |_____|
                                                                                
             */
            // BASICALLY THE IDEA HERE IS TO SKIL OBJECTS THAT IS ON THEY WAY, BECAUSE WE HAVE THE LOCATION AND WE NEED TO SKIL THOSE OBJECTS
            // WE JUST NEED TO CHECK IF THE OBJECT WE WANT TO RUNAWAY IS IN RANGE OF US AND THE JUST SAIL -TOWARDS IT'S LOCATION TO OUR DEST (AHLAKA)
            PirateGame game = PirateGame._game;
            Pirate pirate = pir as Pirate;
            if (pirate == null) return;
            Asteroid ast = game.GetLivingAsteroids().Any() ? game.GetLivingAsteroids().ClosestObject(pirate) as Asteroid : null;
            Wormhole cwh = game.GetAllWormholes().withReachTime(pirate).Any() ? game.GetAllWormholes().withReachTime(pirate).ClosestObject(pirate) as Wormhole : null;
            Pirate enemy = game.GetEnemyLivingPirates().Any() ? game.GetEnemyLivingPirates().ClosestObject(pirate) as Pirate : null;
            Mothership ms = game.GetMyMotherships().Any() ? game.GetMyMotherships().ClosestObject(pirate) as Mothership : null;
            StickyBomb bomber = game.GetAllStickyBombs().Any() ? game.GetAllStickyBombs().ClosestObject(pirate) as StickyBomb : null;
            System.Console.WriteLine($"--- PIRATE {pirate.Id} ---");
            //System.Console.WriteLine($"CAN EXPLODE: {weCanExplode(game,pirate)}");
            if (weCanExplode(game, pirate)) return;
            // ASTRO VIZAU
            if (ast != null && pirate.Location.Towards(destination, pirate.MaxSpeed).InRange(ast.Location.Add(ast.Direction), ast.Size))
            {
                // IF PIRATE IS HEAVY AND ASTRO IS ABOUT TO KILL HIM SWITCH
                if (!switchedThisTurn && pirate.StateName == game.STATE_NAME_HEAVY && Normal(game) != null)
                {
                    pirate.SwapStates(Normal(game));
                    switchedThisTurn = true;
                    return;
                }

                Location loc = ast.Location.Add(ast.Direction).Towards(pirate, ast.Size);
                Location loc2 = new Location(0, 0);

                if (System.Math.Abs(pirate.Location.Col - ast.Location.Add(ast.Direction).Col) < 999)
                {
                    loc2 = loc.Add(new Location(0, 999));
                }
                else
                {
                    double m = (pirate.Location.Row - ast.Location.Add(ast.Direction).Row) / (pirate.Location.Col - ast.Location.Add(ast.Direction).Col);
                    m = -1 / m;
                    Line l1 = new Line(m, loc);
                    loc2 = l1.Tariehu(loc, pirate);
                }
                Location final = loc.Towards(loc2, pirate.MaxSpeed);
                Location option1 = loc.Towards(loc2, pirate.MaxSpeed);
                Location option2 = loc.Towards(loc2, -pirate.MaxSpeed);
                if (ast.Direction.Equals(new Location(0, 0)))
                {
                    if (option1.Distance(destination) > option2.Distance(destination))
                    {
                        if (option2.InRange(ast.Location.Add(ast.Direction), ast.Size))
                            pirate.Sail(option1);
                        else
                            pirate.Sail(option2);
                    }
                    else
                    {
                        if (option1.InRange(ast.Location.Add(ast.Direction), ast.Size))
                            pirate.Sail(option2);
                        else
                            pirate.Sail(option1);
                    }
                }
                else
                {
                    //TAASE
                    //Position pos = MaamidLi(game, pirate, ast);
                    Position pos = getPosition(game, pirate, ast);
                    Location zav = ast.Location.Add(ast.Direction);
                    System.Console.WriteLine(pos);
                    if (option1.InRange(ast.Location.Add(ast.Direction), ast.Size) && option2.InRange(ast.Location.Add(ast.Direction), ast.Size))
                    {
                        if (pirate.CanPush(ast))
                        {
                            if (closestEnemyWithoutPush(game, pirate) != null && !ast.Location.Towards(closestEnemyWithoutPush(game, pirate), pirate.PushDistance).InRange(pirate, ast.Size))
                                pirate.Push(ast, closestEnemyWithoutPush(game, pirate));
                            else
                                pirate.Push(ast, ast);
                            return;
                        }
                    }
                    if (pos == Position.RIGHT)
                    {
                        if (option1.InRange(ast.Location.Add(ast.Direction), ast.Size))
                            pirate.Sail(pirate.Location.Towards(zav, -pirate.MaxSpeed));
                        else
                            pirate.Sail(option1);
                    }
                    else
                    {
                        if (option2.InRange(ast.Location.Add(ast.Direction), ast.Size))
                            pirate.Sail(pirate.Location.Towards(zav, -pirate.MaxSpeed));
                        else
                            pirate.Sail(option2);
                    }


                }
                return;
            }
            // SAME IDEA FOR WH
            if (cwh != null && pirate.Location.Towards(destination, pirate.MaxSpeed).InRange(cwh, cwh.WormholeRange))
            {
                System.Console.WriteLine(NeedToGoToWH(game, pirate, cwh));
                if (ms != null && pirate.HasCapsule() && cwh.Partner.Distance(ms) + cwh.Distance(pirate) <= pirate.Distance(ms) && NeedToGoToWH(game, pirate, cwh))
                {
                    Asteroid a = game.GetLivingAsteroids().Any() ? game.GetLivingAsteroids().ClosestObject(cwh.Partner) as Asteroid : null;
                    if (a != null && !a.Location.Add(a.Direction).InRange(cwh.Partner, a.Size))
                    {
                        pirate.Sail(destination);
                        return;
                    }
                    StickyBomb bomb = game.GetAllStickyBombs().Any() ? game.GetAllStickyBombs().ClosestObject(cwh.Partner) as StickyBomb : null;
                    if (bomb != null && !bomb.Location.InRange(cwh.Partner, bomb.ExplosionRange))
                    {
                        pirate.Sail(destination);
                        return;
                    }


                }

                Location loc = cwh.Location.Towards(pirate, cwh.WormholeRange);
                Location loc2 = new Location(0, 0);

                if (System.Math.Abs(pirate.Location.Col - cwh.Location.Col) < 999)
                {
                    loc2 = loc.Add(new Location(0, 999));
                }
                else
                {
                    double m = (pirate.Location.Row - cwh.Location.Row) / (pirate.Location.Col - cwh.Location.Col);
                    m = -1 / m;
                    Line l1 = new Line(m, loc);
                    loc2 = l1.Tariehu(loc, pirate);
                }

                // SAIL OPTIONS
                Location option1 = loc.Towards(loc2, pirate.MaxSpeed);
                Location option2 = loc.Towards(loc2, -pirate.MaxSpeed);

                if (option1.Distance(destination) > option2.Distance(destination))
                {
                    if (option2.InRange(cwh, cwh.WormholeRange))
                        pirate.Sail(option1);
                    else
                        pirate.Sail(option2);
                }
                else
                {
                    if (option1.InRange(cwh, cwh.WormholeRange))
                        pirate.Sail(option2);
                    else
                        pirate.Sail(option1);
                }
                return;
            }

            if (pirate.HasCapsule() && enemysNearPirate(game, pirate).Count >= pirate.NumPushesForCapsuleLoss && PiratesInRangeOfPush(game, pirate).Count > 0)
            {
                pirate.Sail(destination);
                return;
            }
            // BOMBEER RUNAWAY
            if (bomber != null && ((pirate.Location.Towards(destination, pirate.MaxSpeed).InRange(bomber, game.StickyBombExplosionRange)) || (EPiratesInRangeOfPush(game, bomber.Carrier).Count > 0 && bomber.Countdown < 2 && bomber.Location.Towards(pirate, PushSumList(EPiratesInRangeOfPush(game, bomber.Carrier))).InRange(pirate, game.StickyBombExplosionRange))) && bomber.Carrier != pirate)
            {
                // IF WE CANT GET OUT OF RANGE AND BOMBER HAS 2 TURNS TO EXPLODE...
                if (bomber.InRange(pirate, game.StickyBombExplosionRange) && bomber.Carrier.StickyBombs.OrderBy(x => x.Countdown).First().Countdown <= 2 && pirate.PushReloadTurns < 2)
                {
                    System.Console.WriteLine("GO TO BOMBER");
                    pirate.Sail(bomber);
                    return;
                }
                System.Console.WriteLine("RUNNING FROM BOMBER");
                Location loc = bomber.GetLocation().Towards(pirate, game.StickyBombExplosionRange);
                if (bomber.Carrier is Asteroid)
                {
                    Asteroid a = bomber.Carrier as Asteroid;
                    loc = a.Location.Add(a.Direction).Towards(pirate, game.StickyBombExplosionRange);
                }
                Location loc2 = new Location(0, 0);

                if (System.Math.Abs(pirate.Location.Col - bomber.GetLocation().Col) < game.StickyBombExplosionRange)
                {
                    loc2 = loc.Add(new Location(0, 999));
                }
                else
                {
                    double m = (pirate.Location.Row - bomber.GetLocation().Row) / (pirate.Location.Col - bomber.GetLocation().Col);
                    m = -1 / m;
                    Line l1 = new Line(m, loc);
                    loc2 = l1.Tariehu(loc, pirate);
                }

                // SAIL OPTIONS
                Location option1 = loc.Towards(loc2, pirate.MaxSpeed);
                Location option2 = loc.Towards(loc2, -pirate.MaxSpeed);
                if (option1.InRange(bomber, game.StickyBombExplosionRange) && option2.InRange(bomber, game.StickyBombExplosionRange))
                {
                    System.Console.WriteLine("TWO RANGES ARE IN BOMB ZONE");
                    pirate.Sail(pirate.Location.Towards(bomber, -pirate.MaxSpeed));
                    return;
                }
                //TODO
                if (option1.Distance(destination) < option2.Distance(destination))
                {
                    if (option1.InRange(bomber, game.StickyBombExplosionRange))
                    {
                        pirate.Sail(pirate.Location.Towards(option1, -pirate.MaxSpeed));
                    }
                    else
                    {
                        pirate.Sail(option1);
                    }
                }
                else
                {
                    if (option2.InRange(bomber, game.StickyBombExplosionRange))
                    {
                        pirate.Sail(pirate.Location.Towards(option2, -pirate.MaxSpeed));
                    }
                    else
                    {
                        pirate.Sail(option2);
                    }
                }
                return;
            }
            if (ms != null && pirate.HasCapsule() && pirate.Location.Towards(ms, pirate.MaxSpeed).InRange(ms, ms.UnloadRange))
            {
                pirate.Sail(destination);
                return;
            }
            // PIRATE RUNAWAY
            if (enemy != null && pirate.HasCapsule() && pirate.Location.Towards(destination, pirate.MaxSpeed).InRange(enemy, game.PushRange) && enemysAroundPirate(game, pirate).Count >= pirate.NumPushesForCapsuleLoss /*&& enemysAroundPirate(game, pirate).Any()*/)
            {
                System.Console.WriteLine("RUNNING FROM PIRATE");
                Pirate closest = enemysAroundPirate(game, pirate).ToArray().ClosestObject(pirate) as Pirate;

                Location loc = closest.Location.Towards(pirate, closest.PushRange);
                Location loc2 = new Location(0, 0);

                if (System.Math.Abs(pirate.Location.Col - closest.Location.Col) < closest.PushRange)
                {
                    loc2 = loc.Add(new Location(0, 999));
                }
                else
                {
                    double m = (pirate.Location.Row - closest.Location.Row) / (pirate.Location.Col - closest.Location.Col);
                    m = -1 / m;
                    Line l1 = new Line(m, loc);
                    loc2 = l1.Tariehu(loc, pirate);
                }

                // SAIL OPTIONS
                Location option1 = loc.Towards(loc2, pirate.MaxSpeed);
                Location option2 = loc.Towards(loc2, -pirate.MaxSpeed);
                if (option1.InRange(closest, game.StickyBombExplosionRange) && option2.InRange(closest, game.StickyBombExplosionRange))
                {
                    System.Console.WriteLine("TWO RANGES ARE PUSH ZONE");
                    pirate.Sail(pirate.Location.Towards(closest, -pirate.MaxSpeed));
                    return;
                }
                if (option1.Distance(destination) < option2.Distance(destination))
                {
                    if (option1.InRange(closest, closest.PushRange))
                    {
                        pirate.Sail(pirate.Location.Towards(option1, -pirate.MaxSpeed));
                    }
                    else
                    {
                        pirate.Sail(option1);
                    }
                }
                else
                {
                    if (option2.InRange(closest, closest.PushRange))
                    {
                        pirate.Sail(pirate.Location.Towards(option2, -pirate.MaxSpeed));
                    }
                    else
                    {
                        pirate.Sail(option2);
                    }
                }
                return;
            }

            // OTHERWISE WE SAIL STRAIGHT TO DEST
            System.Console.WriteLine(destination);
            pirate.Sail(destination);

        }
        public static List<MapObject> asMapObject<T>(this List<T> list)
        {
            // TRY CAST A LIST OF ANYTHING TO MAP OBJECT
            try
            {
                return list.Cast<MapObject>().ToList();
            }
            catch
            {
                System.Console.WriteLine("ERROR CASTING LIST. 1");
                return null;
            }
        }
        public static MapObject ClosestObject<T>(this T[] array, MapObject obj)
        {
            // TRY CAST A LIST OF ANYTHING TO MAP OBJECT
            try
            {
                return array.ToList().Cast<MapObject>().ToList().OrderBy(x => obj.Distance(x)).First();
            }
            catch
            {
                if (array.Length > 0)
                    System.Console.WriteLine(array[0].GetType());
                System.Console.WriteLine("ERROR CASTING LIST. 2");
                System.Console.WriteLine(obj);
                System.Console.WriteLine(array.Length + " " + array.GetType());
                array.ToList().ForEach(x => System.Console.WriteLine(x));
                return null;
            }
        }
        //public MapObject xClosestToY(MapObject obj, List<T> list)
        //{
        //    return obj != null && list.Any() ? list.OrderBy(x => obj.Distance(x)).First() : null;
        //}
        public static Location[] InitiaLocations<T>(this T[] array)
        {
            try
            {
                List<Location> l = new List<Location>();
                foreach (Capsule capsule in PirateGame._game.GetMyCapsules())
                {
                    l.Add(capsule.InitialLocation);
                }
                return l.ToArray();
            }
            catch
            {
                System.Console.WriteLine("ERROR CASTING LIST. 3");
                return null;
            }
        }
        public static List<Pirate> removeHolders<T>(this T[] array)
        {
            // TRY CAST A LIST OF ANYTHING TO MAP OBJECT
            try
            {
                List<Pirate> l = array.Cast<Pirate>().ToList();
                foreach (Capsule cap in PirateGame._game.GetMyCapsules())
                {
                    if (l.Contains(cap.Holder)) l.Remove(cap.Holder);
                }
                return l;
            }
            catch
            {
                System.Console.WriteLine("ERROR CASTING LIST. 4");
                return null;
            }
        }
        public static Pirate[] withPush<T>(this T[] array)
        {
            // TRY CAST A LIST OF ANYTHING TO MAP OBJECT
            try
            {
                List<Pirate> l = new List<Pirate>();
                foreach (Pirate pirate in PirateGame._game.GetMyLivingPirates())
                {
                    if (pirate.PushReloadTurns < 3)
                        l.Add(pirate);
                }
                return l.ToArray();
            }
            catch
            {
                System.Console.WriteLine("ERROR CASTING LIST. 5");
                return null;
            }
        }
        public static List<Pirate> OnlyHolders<T>(this T[] array)
        {
            // TRY CAST A LIST OF ANYTHING TO MAP OBJECT
            try
            {
                List<Pirate> l = new List<Pirate>();
                foreach (Pirate pirate in PirateGame._game.GetMyLivingPirates())
                {
                    if (pirate.HasCapsule()) l.Add(pirate);
                }
                return l;
            }
            catch
            {
                System.Console.WriteLine("ERROR CASTING LIST. 6");
                return null;
            }
        }
        public static List<Pirate> EOnlyHolders<T>(this T[] array)
        {
            // TRY CAST A LIST OF ANYTHING TO MAP OBJECT
            try
            {
                List<Pirate> l = new List<Pirate>();
                foreach (Pirate pirate in PirateGame._game.GetEnemyLivingPirates())
                {
                    if (pirate.HasCapsule()) l.Add(pirate);
                }
                return l;
            }
            catch
            {
                System.Console.WriteLine("ERROR CASTING LIST. 10");
                return null;
            }
        }

        public static Pirate[] EOnlyBombers<T>(this T[] array)
        {
            // TRY CAST A LIST OF ANYTHING TO MAP OBJECT
            try
            {
                List<Pirate> l = new List<Pirate>();
                foreach (Pirate pirate in PirateGame._game.GetEnemyLivingPirates())
                {
                    if (pirate.StickyBombs.Any()) l.Add(pirate);
                }
                return l.ToArray();
            }
            catch
            {
                System.Console.WriteLine("ERROR CASTING LIST. 10");
                return null;
            }
        }


        public static List<MapObject> asMapObject<T>(this T[] array)
        {
            // TRY CAST A ARRAY OF ANYTHING TO MAP OBJECT
            try
            {
                return array.Cast<MapObject>().ToList();
            }
            catch
            {
                System.Console.WriteLine("ERROR CASTING LIST. 7");
                return null;
            }
        }
        public static Wormhole[] withReachTime<T>(this T[] array, Pirate pirate)
        {
            // TRY CAST A ARRAY OF ANYTHING TO MAP OBJECT
            try
            {
                List<Wormhole> l = new List<Wormhole>();
                foreach (Wormhole wormhole in array.Cast<Wormhole>().ToList())
                {
                    if (wormhole.TurnsToReactivate <= pirate.Distance(wormhole) / pirate.MaxSpeed + 2)
                        l.Add(wormhole);
                }
                return l.ToArray();
            }
            catch
            {
                System.Console.WriteLine("ERROR CASTING LIST. 8");
                return null;
            }
        }

        public static Pirate[] RemoveBombers<T>(this T[] array)
        {
            try
            {
                List<Pirate> pirates = new List<Pirate>();
                foreach (var x1 in array)
                {
                    if (!(x1 as Pirate).StickyBombs.Any())
                        pirates.Add(x1 as Pirate);
                }
                return pirates.ToArray();
            }
            catch
            {
                System.Console.WriteLine("ERROR CASTING LIST. 12");
                return null;
            }
        }
        public static bool IsWithin<T>(this T value, T minimum, T maximum) where T : System.IComparable<T>
        {
            if (value.CompareTo(minimum) < 0)
                return false;
            if (value.CompareTo(maximum) > 0)
                return false;
            return true;
        }

        static int count = 0;
        public static bool MissionChooser(PirateGame game)
        {
            GameObjects Go = GetData(game);
            switch (game.GetEnemy().BotName)
            {
                case "25184":
                    GeorgePushSenior(game, Go);
                    return true;
                case "25185":
                    LargeRocks(game, Go);
                    return true;
                case "25186":
                    OneManArmy(game, Go);
                    return true;
                case "25187":
                    Metorite(game, Go);
                    return true;
                case "25188":
                    Steroids(game, Go);
                    return true;
                case "25189":
                    Lepton(game, Go);
                    return true;
                case "25190":
                    Voyager1(game, Go);
                    return true;
                case "25191":
                    YouShallNotPass(game, Go);
                    return true;
                case "25192":
                    Pullpullon(game, Go);
                    return true;
                case "25235":
                    Graviton(game, Go);
                    return true;
                case "25236":
                    OutOfSpace(game, Go);
                    return true;
                case "25237":
                    Pushti(game, Go);
                    return true;
                case "25238":
                    SpaceTime(game, Go);
                    return true;
                case "25239":
                    Momentum(game, Go);
                    return true;
                case "25240":
                    William(game, Go);
                    return true;
                case "25241":
                    Spaghettification(game, Go);
                    return true;
                case "25242":
                    PullPushon(game, Go);
                    return true;
                case "25766":
                    StateMachine(game, Go);
                    return true;
                case "25767":
                    DarkInvasion(game, Go);
                    return true;
                case "25768":
                    HeavyLifting(game, Go);
                    return true;
                case "25769":
                    DeathStar(game, Go);
                    return true;
                case "25770":
                    StaticVoid(game, Go);
                    return true;
                case "25771":
                    Pushiner(game, Go);
                    return true;
                case "25772":
                    SpaceRace(game, Go);
                    return true;
                case "25773":
                    GravitySlingshot(game, Go);
                    return true;
                case "25774":
                    NeutronStar(game, Go);
                    return true;
                case "26069":
                    RainFromHell(game, Go);
                    return true;
                default:
                    return false;
            }
        }
        public static void GeorgePushSenior(PirateGame game, GameObjects go)
        {
            if (game.GetMyself().Score == 7 && game.Turn == 706)
            {
                count++;
                System.Console.WriteLine(count);
            }
            foreach (Pirate pirate in game.GetMyLivingPirates())
            {
                if (game.GetMyCapsules()[0].Holder != null && pirate.CanPush(game.GetMyCapsules()[0].Holder))
                {
                    pirate.Push(game.GetMyCapsules()[0].Holder, game.GetMyMotherships()[1]);
                }
                else if (pirate.HasCapsule() && game.GetMyself().Score == 7 && game.Turn != 706)
                {
                    pirate.Sail(go.MyMotherships[1].Location.Subtract(new Location(0, game.MothershipUnloadRange + 1)));
                }
                else if (pirate.HasCapsule())
                {
                    pirate.Sail(xClosestToY(pirate, go.MyMothershipsAsObject())[0]);
                }
                else if (pirate == xClosestToY(go.MyCapsules[0], go.MyLivingPiratesAsObject())[0] as Pirate)
                {
                    pirate.Sail(go.MyCapsules[0]);
                }
                else if (game.GetEnemyCapsules()[0].Holder != null && pirate.CanPush(game.GetEnemyCapsules()[0].Holder))
                {
                    pirate.Push(game.GetEnemyCapsules()[0].Holder, game.GetEnemyMotherships()[0]);
                }
                else if (xClosestToY(game.GetEnemyMotherships()[1], go.MyLivingPiratesAsObject())[0] as Pirate == pirate || xClosestToY(game.GetEnemyMotherships()[1], go.MyLivingPiratesAsObject())[1] as Pirate == pirate || xClosestToY(game.GetEnemyMotherships()[1], go.MyLivingPiratesAsObject())[2] as Pirate == pirate)
                {
                    pirate.Sail(game.GetEnemyMotherships()[1].Location.Towards(game.GetEnemyCapsules()[0], game.MothershipUnloadRange + game.PirateMaxSpeed));
                }
                else if (xClosestToY(game.GetEnemyMotherships()[1], go.MyLivingPiratesAsObject())[3] as Pirate == pirate || xClosestToY(game.GetEnemyMotherships()[1], go.MyLivingPiratesAsObject())[4] as Pirate == pirate)
                {
                    pirate.Sail(game.GetEnemyMotherships()[1].Location.Towards(game.GetEnemyCapsules()[0], game.MothershipUnloadRange * 2 + game.PirateMaxSpeed));
                }
                else
                {
                    pirate.Sail(go.MyCapsules[0].InitialLocation);
                }
            }
        }
        public static void LargeRocks(PirateGame game, GameObjects go)
        {
            foreach (Pirate pirate in game.GetMyLivingPirates())
            {
                if (!TryPush(pirate, game))
                {
                    if (pirate.HasCapsule())
                    {
                        pirate.Sail(xClosestToY(pirate, go.MyMothershipsAsObject())[0]);
                    }
                    else if (pirate == game.GetAllMyPirates()[4])
                    {
                        pirate.Sail(game.GetAllAsteroids()[1]);
                    }
                    else if (pirate == xClosestToY(go.MyCapsules[0], go.MyLivingPiratesAsObject())[0] as Pirate)
                    {
                        pirate.Sail(go.MyCapsules[0]);
                    }
                    else
                    {
                        pirate.Sail(go.MyCapsules[0].InitialLocation);
                    }
                }
            }
        }
        public static void OneManArmy(PirateGame game, GameObjects go)
        {
            Pirate pirate = game.GetMyLivingPirates()[0];
            foreach (Pirate epirate in game.GetEnemyLivingPirates())
            {
                if (go.EnemyCapsules[0].Holder != null)
                {
                    if (pirate.CanPush(epirate) && epirate.HasCapsule())
                    {
                        pirate.Push(epirate, new Location(epirate.Location.Row + pirate.PushDistance, epirate.Location.Col));
                    }
                }
                else
                {
                    if (pirate.CanPush(epirate))
                    {
                        pirate.Push(epirate, new Location(epirate.Location.Row + pirate.PushDistance, epirate.Location.Col));
                    }
                }
            }
        }
        public static void Metorite(PirateGame game, GameObjects go)
        {
            foreach (Pirate pirate in game.GetMyLivingPirates())
            {
                if (!TryPush(pirate, game))
                {
                    if (pirate.HasCapsule())
                    {
                        if (game.GetMyself().Score == 7 && game.Turn != 555)
                        {
                            pirate.Sail(go.MyMotherships[0].Location.Towards(pirate, game.MothershipUnloadRange + 1));
                        }
                        else
                        {
                            pirate.Sail(xClosestToY(pirate, go.MyMothershipsAsObject())[0]);
                        }
                    }
                    else if (pirate == xClosestToY(go.MyCapsules[0], go.MyLivingPiratesAsObject())[0] as Pirate)
                    {
                        pirate.Sail(go.MyCapsules[0]);
                    }
                    else if (pirate == xClosestToY(go.EnemyMotherships[0], go.MyLivingPiratesAsObject())[0] as Pirate || pirate == xClosestToY(go.EnemyMotherships[0], go.MyLivingPiratesAsObject())[1] as Pirate)
                    {
                        pirate.Sail(go.EnemyMotherships[0].Location.Towards(go.EnemyCapsules[0], game.MothershipUnloadRange + pirate.MaxSpeed));
                    }
                    else if (pirate == xClosestToY(go.EnemyMotherships[0], go.MyLivingPiratesAsObject())[2] as Pirate || pirate == xClosestToY(go.EnemyMotherships[0], go.MyLivingPiratesAsObject())[3] as Pirate)
                    {
                        pirate.Sail(go.EnemyMotherships[0].Location.Towards(go.EnemyCapsules[0], game.MothershipUnloadRange * 2 + pirate.MaxSpeed));
                    }
                    else
                    {
                        pirate.Sail(go.MyCapsules[0]);
                    }
                }
            }
        }
        public static void Steroids(PirateGame game, GameObjects go)
        {
            foreach (Pirate pirate in game.GetMyLivingPirates())
            {
                if (!TryPush(pirate, game))
                {
                    if (game.GetMyself().Score == 7)
                    {
                        if (go.MyCapsules[0].Holder != null && go.MyCapsules[1].Holder != null && go.MyCapsules[0].Holder.Location.Equals(go.MyCapsules[1].Holder.Location))
                        {
                            pirate.Sail(go.MyMotherships[0]);
                        }
                        else if (pirate.HasCapsule() && pirate == xClosestToY(go.MyCapsules[0], go.MyLivingPiratesAsObject())[0] as Pirate)
                        {
                            pirate.Sail(go.MyCapsules[1]);
                        }
                        else if (pirate == xClosestToY(go.MyCapsules[0], go.MyLivingPiratesAsObject())[0] as Pirate)
                        {
                            pirate.Sail(go.MyCapsules[0]);
                        }
                        else if (pirate == xClosestToY(go.MyCapsules[1], go.MyLivingPiratesAsObject())[1] as Pirate)
                        {
                            pirate.Sail(go.MyCapsules[1]);
                        }
                        else
                        {
                            pirate.Sail(go.MyCapsules[0].InitialLocation);
                        }
                    }
                    else
                    {
                        if (!TryPush(pirate, game))
                        {
                            if (pirate.HasCapsule())
                            {
                                pirate.Sail(xClosestToY(pirate, go.MyMothershipsAsObject())[0]);
                            }
                            else if (pirate == xClosestToY(go.MyCapsules[0], go.MyLivingPiratesAsObject())[0] as Pirate)
                            {
                                pirate.Sail(go.MyCapsules[0]);
                            }
                            else if (pirate == xClosestToY(go.MyCapsules[1], go.MyLivingPiratesAsObject())[0] as Pirate)
                            {
                                pirate.Sail(go.MyCapsules[1]);
                            }
                            else
                            {
                                pirate.Sail(go.MyCapsules[0].InitialLocation);
                            }
                        }
                    }
                }
            }
        }
        public static void Lepton(PirateGame game, GameObjects go)
        {
            foreach (Pirate pirate in game.GetMyLivingPirates())
            {
                if (pirate.HasCapsule())
                {
                    if (pirate.CanPush(xClosestToY(pirate, go.MyLivingPiratesAsObject())[1] as Pirate))
                    {
                        pirate.Push(xClosestToY(pirate, go.MyLivingPiratesAsObject())[1] as Pirate, go.MyMotherships[0]);
                    }
                    else
                    {
                        pirate.Sail(xClosestToY(pirate, go.MyMothershipsAsObject())[0]);
                    }
                }
                else if (go.MyCapsules[0].Holder != null && pirate == xClosestToY(go.MyCapsules[0], go.MyLivingPiratesAsObject())[1] as Pirate && pirate.CanPush(go.MyCapsules[0].Holder))
                {
                    pirate.Push(go.MyCapsules[0].Holder, go.MyMothershipsAsObject()[0]);
                }
                else if (go.MyCapsules[0].Holder != null && pirate == xClosestToY(go.MyCapsules[0], go.MyLivingPiratesAsObject())[1] as Pirate && !pirate.CanPush(go.MyCapsules[0].Holder))
                {
                    pirate.Sail(go.MyCapsules[0].Holder);
                }
                else if (game.GetLivingAsteroids().Length != 0 && (pirate == xClosestToY(game.GetLivingAsteroids()[0], go.MyLivingPiratesAsObject())[0] as Pirate || pirate == xClosestToY(game.GetLivingAsteroids()[0], go.MyLivingPiratesAsObject())[1] as Pirate || pirate == xClosestToY(game.GetLivingAsteroids()[0], go.MyLivingPiratesAsObject())[2] as Pirate || pirate == xClosestToY(game.GetLivingAsteroids()[0], go.MyLivingPiratesAsObject())[3] as Pirate) && !pirate.CanPush(game.GetLivingAsteroids()[0]))
                {
                    pirate.Sail(game.GetLivingAsteroids()[0]);
                }
                else if (game.GetLivingAsteroids().Length != 0 && (pirate == xClosestToY(game.GetLivingAsteroids()[0], go.MyLivingPiratesAsObject())[0] as Pirate || pirate == xClosestToY(game.GetLivingAsteroids()[0], go.MyLivingPiratesAsObject())[1] as Pirate || pirate == xClosestToY(game.GetLivingAsteroids()[0], go.MyLivingPiratesAsObject())[2] as Pirate || pirate == xClosestToY(game.GetLivingAsteroids()[0], go.MyLivingPiratesAsObject())[3] as Pirate) && pirate.CanPush(game.GetLivingAsteroids()[0]))
                {
                    pirate.Push(game.GetLivingAsteroids()[0], game.GetLivingAsteroids()[0]);
                    count++;
                    System.Console.WriteLine(count);
                }
                else if (pirate == xClosestToY(go.MyCapsules[0], go.MyLivingPiratesAsObject())[0] as Pirate)
                {
                    pirate.Sail(go.MyCapsules[0]);
                }
                else
                {
                    pirate.Sail(go.MyCapsules[0].InitialLocation);
                }
            }
        }
        public static void Voyager1(PirateGame game, GameObjects go)
        {
            foreach (Pirate pirate in game.GetMyLivingPirates())
            {
                if (!TryPush(pirate, game))
                {
                    if (game.GetLivingAsteroids().Length != 0 && pirate == xClosestToY(game.GetLivingAsteroids()[0], go.MyLivingPiratesAsObject())[0] as Pirate && !pirate.CanPush(game.GetLivingAsteroids()[0]))
                    {
                        pirate.Sail(game.GetLivingAsteroids()[0]);
                    }
                    else if (pirate.HasCapsule())
                    {
                        pirate.Sail(xClosestToY(pirate, go.MyMothershipsAsObject())[0]);
                    }
                    else if (pirate == xClosestToY(go.MyCapsules[0], go.MyLivingPiratesAsObject())[0] as Pirate)
                    {
                        pirate.Sail(go.MyCapsules[0]);
                    }
                    else if (pirate == xClosestToY(go.MyCapsules[1], go.MyLivingPiratesAsObject())[0] as Pirate)
                    {
                        pirate.Sail(go.MyCapsules[1]);
                    }
                    else
                    {
                        pirate.Sail(go.MyCapsules[0].InitialLocation);
                    }
                }
            }
        }
        public static void YouShallNotPass(PirateGame game, GameObjects go)
        {
            if (go.MyLivingPirates.Length > 0)
            {
                Pirate pirate = game.GetMyLivingPirates()[0];
                if (game.Turn > 108 && game.Turn < 112 && pirate.CanPush(xClosestToY(pirate, go.LivingAsteroidsAsObject())[0] as SpaceObject))
                {
                    pirate.Push(xClosestToY(pirate, go.LivingAsteroidsAsObject())[0] as SpaceObject, new Location(0, 6330));
                }
                else if (game.Turn > 315 && game.Turn < 322)
                {
                    // MA LAASOT? KLOM
                }
                else if (game.Turn > 349 && game.Turn < 355)
                {
                    // MA LAASOT? KLOM
                }
                else if (pirate.CanPush(xClosestToY(pirate, go.LivingAsteroidsAsObject())[0] as SpaceObject))
                {
                    pirate.Push(xClosestToY(pirate, go.LivingAsteroidsAsObject())[0] as SpaceObject, xClosestToY(pirate, go.LivingAsteroidsAsObject())[1] as SpaceObject);
                }
                else if (pirate.HasCapsule())
                {
                    pirate.Sail(xClosestToY(pirate, go.MyMothershipsAsObject())[0]);
                }
                else if (pirate == xClosestToY(go.MyCapsules[0], go.MyLivingPiratesAsObject())[0] as Pirate)
                {
                    pirate.Sail(go.MyCapsules[0]);
                }
            }
        }
        public static void Pullpullon(PirateGame game, GameObjects go)
        {
            foreach (Pirate pirate in game.GetMyLivingPirates())
            {
                if (game.GetLivingAsteroids().Length > 0 && pirate.CanPush(game.GetLivingAsteroids()[0]))
                {
                    if (game.GetEnemyCapsules()[0].Holder != null)
                    {
                        pirate.Push(game.GetLivingAsteroids()[0], game.GetEnemyCapsules()[0].Holder);
                    }
                    else if (game.GetEnemyCapsules()[1].Holder != null)
                    {
                        pirate.Push(game.GetLivingAsteroids()[0], game.GetEnemyCapsules()[1].Holder);
                    }
                    //else
                    //{
                    //    pirate.Push(game.GetLivingAsteroids()[0], game.GetEnemyCapsules()[0].InitialLocation);
                    //}
                }
                else if (pirate.HasCapsule())
                {
                    pirate.Sail(game.GetMyMotherships()[0]);
                }
                else if (pirate == xClosestToY(go.MyCapsules[0], go.MyLivingPiratesAsObject())[0] as Pirate)
                {
                    pirate.Sail(go.MyCapsules[0]);
                }
                else if (pirate == xClosestToY(go.MyCapsules[1], go.MyLivingPiratesAsObject())[0] as Pirate)
                {
                    pirate.Sail(go.MyCapsules[1]);
                }
                else
                {
                    pirate.Sail(game.GetEnemyMotherships()[0].Location.Towards(game.GetEnemyCapsules()[0], game.MothershipUnloadRange + game.PirateMaxSpeed));
                }
            }
        }
        public static void Graviton(PirateGame game, GameObjects go)
        {
            foreach (Pirate pir in game.GetMyLivingPirates())
            {
                if (!pir.HasCapsule() && pir.InRange(game.GetMyMotherships()[0], 700)) pir.Sail(xClosestToY(pir, go.LivingAsteroidsAsObject())[0]);
                else if (pir.HasCapsule()) pir.Sail(game.GetMyMotherships()[0]);
                else if (xClosestToY(go.MyCapsules[0].InitialLocation, go.MyLivingPiratesAsObject())[0] as Pirate == pir) pir.Sail(go.MyCapsules[0]);
                else pir.Sail(xClosestToY(pir, go.LivingAsteroidsAsObject())[0]);
            }
        }
        public static void Pushti(PirateGame game, GameObjects go)
        {
            foreach (Pirate pir in game.GetMyLivingPirates())
            {
                if (!TryPush(pir, game))
                {
                    if (go.MyCapsules[0].Holder != null && pir.CanPush(go.MyCapsules[0].Holder) && EPiratesInRangeOfPush(game, pir).Count == 2) pir.Push(go.MyCapsules[0].Holder, go.MyMotherships[0]);
                    else if (pir.HasCapsule() && game.GetAllWormholes()[1].InRange(pir, 1000)) pir.Sail(game.GetAllWormholes()[1]);
                    else if (pir.HasCapsule()) pir.Sail(go.MyMotherships[0]);
                    else if (pir == xClosestToY(go.MyCapsules[0], go.MyLivingPiratesAsObject())[0] as Pirate) pir.Sail(go.MyCapsules[0]);
                    else if (pir == xClosestToY(go.MyCapsules[0], go.MyLivingPiratesAsObject())[1] as Pirate) pir.Sail(go.MyCapsules[0].Location.Towards(go.MyMotherships[0], game.PushRange + 1));
                    else if (pir == xClosestToY(game.GetAllWormholes()[0], go.MyLivingPiratesAsObject())[0] as Pirate) pir.Sail(game.GetAllWormholes()[0].Location.Towards(go.MyMotherships[0], game.PushRange + 1));
                    else if (pir == xClosestToY(game.GetAllWormholes()[1], go.MyLivingPiratesAsObject())[0] as Pirate) pir.Sail(game.GetAllWormholes()[1].Location.Towards(pir, 100));
                    else if (pir == xClosestToY(game.GetAllWormholes()[1], go.MyLivingPiratesAsObject())[1] as Pirate) pir.Sail(game.GetAllWormholes()[1].Location.Towards(pir, 100));
                }
            }
        }
        public static void OutOfSpace(PirateGame game, GameObjects go)
        {
            foreach (Pirate pirate in game.GetMyLivingPirates())
            {
                Capsule close = xClosestToY(game.GetEnemyMotherships()[0], go.EnemyCapsulesAsObject())[0] as Capsule;
                if (!TryPush(pirate, game))
                {
                    // sends 2 closest to our ms to frontline
                    if (!pirate.HasCapsule() && game.GetMyLivingPirates().Length > 3 && (pirate == xClosestToY(game.GetEnemyMotherships()[0], go.MyLivingPiratesAsObject())[2] || pirate == xClosestToY(game.GetEnemyMotherships()[0], go.MyLivingPiratesAsObject())[3]))
                    {
                        if (close.Holder == null)
                        {
                            pirate.Sail(game.GetEnemyMotherships()[0].Location.Towards(close, game.MothershipUnloadRange * 2));
                        }
                        else
                        {
                            pirate.Sail(game.GetEnemyMotherships()[0].Location.Towards(close.Holder, game.MothershipUnloadRange * 2));
                        }

                    }
                    // sends 2 closest to our ms to backline
                    else if (!pirate.HasCapsule() && game.GetMyLivingPirates().Length > 1 && (pirate == xClosestToY(game.GetEnemyMotherships()[0], go.MyLivingPiratesAsObject())[0] || pirate == xClosestToY(game.GetEnemyMotherships()[0], go.MyLivingPiratesAsObject())[1]))
                    {
                        if (close.Holder == null)
                        {
                            pirate.Sail(game.GetEnemyMotherships()[0].Location.Towards(close, game.MothershipUnloadRange * 2 + game.PushDistance));
                        }
                        else
                        {
                            pirate.Sail(game.GetEnemyMotherships()[0].Location.Towards(close.Holder, game.MothershipUnloadRange * 2 + game.PushDistance));
                        }
                    }
                    else if (pirate == game.GetMyCapsules()[0].Holder)
                    {
                        pirate.Sail(game.GetMyMotherships()[0]);
                    }
                    else if (game.GetMyCapsules()[0].Holder == null && pirate == xClosestToY(game.GetMyCapsules()[0], go.MyLivingPiratesAsObject())[0])
                    {
                        pirate.Sail(game.GetMyCapsules()[0].InitialLocation);
                    }
                    else if (game.GetMyCapsules()[0].Holder == null && pirate == xClosestToY(game.GetMyCapsules()[0], go.MyLivingPiratesAsObject())[1])
                    {
                        pirate.Sail((xClosestToY(game.GetMyCapsules()[0], go.MyLivingPiratesAsObject())[0] as Pirate).Location.Towards(game.GetMyMotherships()[0], game.PushRange + game.PirateMaxSpeed));
                    }
                    else if (game.GetMyCapsules()[0].Holder != null && pirate == xClosestToY(game.GetMyCapsules()[0].Holder, go.MyLivingPiratesAsObject())[1])
                    {
                        pirate.Sail((xClosestToY(game.GetMyCapsules()[0], go.MyLivingPiratesAsObject())[0] as Pirate).Location.Towards(game.GetMyMotherships()[0], game.PushRange + game.PirateMaxSpeed));
                    }
                    else
                    {
                        pirate.Sail(game.GetMyCapsules()[0].InitialLocation);
                    }
                }
            }

        }
        public static void SpaceTime(PirateGame game, GameObjects go)
        {
            foreach (Pirate pirate in game.GetMyLivingPirates())
            {
                if (game.GetMyCapsules()[0].Holder == null)
                {
                    pirate.Sail(game.GetMyCapsules()[0].InitialLocation);
                }
                else if (game.GetActiveWormholes().Length > 0 && xClosestToY(pirate, go.AvailableWormholesAsObject())[0] != null && pirate.Distance(xClosestToY(pirate, go.AvailableWormholesAsObject())[0]) < pirate.Distance(game.GetMyMotherships()[0]))
                {
                    pirate.Sail(xClosestToY(pirate, go.AvailableWormholesAsObject())[0]);
                }
                else
                {
                    pirate.Sail(game.GetMyMotherships()[0]);
                }
            }
        }
        public static void Momentum(PirateGame game, GameObjects go)
        {
            foreach (Pirate pirate in game.GetMyLivingPirates())
            {
                if (game.GetMyCapsules()[0].Holder == null)
                {
                    pirate.Sail(game.GetMyCapsules()[0].InitialLocation);
                }
                else if (game.GetActiveWormholes().Length > 0 && xClosestToY(pirate, go.AvailableWormholesAsObject())[0] != null && pirate.Distance(xClosestToY(pirate, go.AvailableWormholesAsObject())[0]) < pirate.Distance(game.GetMyMotherships()[0]))
                {
                    pirate.Sail(xClosestToY(pirate, go.AvailableWormholesAsObject())[0]);
                }
                else
                {
                    pirate.Sail(game.GetMyMotherships()[0]);
                }
            }
        }
        public static void William(PirateGame game, GameObjects go)
        {
            foreach (Pirate pirate in game.GetMyLivingPirates())
            {
                Capsule close = xClosestToY(game.GetEnemyMotherships()[0], go.EnemyCapsulesAsObject())[0] as Capsule;
                if (!TryPush(pirate, game))
                {
                    if (game.GetAllWormholes()[1].Distance(game.GetMyMotherships()[0]) > game.MothershipUnloadRange * 3)
                    {
                        pirate.Sail(game.GetAllWormholes()[1].Location.Towards(pirate, game.WormholeSize + game.PirateMaxSpeed));
                    }
                    else if (!pirate.HasCapsule() && game.GetMyLivingPirates().Length > 3 && (pirate == xClosestToY(game.GetEnemyMotherships()[0], go.MyLivingPiratesAsObject())[2] || pirate == xClosestToY(game.GetEnemyMotherships()[0], go.MyLivingPiratesAsObject())[3]))
                    {
                        if (close.Holder == null)
                        {
                            pirate.Sail(game.GetEnemyMotherships()[0].Location.Towards(close, game.MothershipUnloadRange * 2));
                        }
                        else
                        {
                            pirate.Sail(game.GetEnemyMotherships()[0].Location.Towards(close.Holder, game.MothershipUnloadRange * 2));
                        }

                    }
                    // sends 2 closest to our ms to backline
                    else if (!pirate.HasCapsule() && game.GetMyLivingPirates().Length > 1 && (pirate == xClosestToY(game.GetEnemyMotherships()[0], go.MyLivingPiratesAsObject())[0] || pirate == xClosestToY(game.GetEnemyMotherships()[0], go.MyLivingPiratesAsObject())[1]))
                    {
                        if (close.Holder == null)
                        {
                            pirate.Sail(game.GetEnemyMotherships()[0].Location.Towards(close, game.MothershipUnloadRange * 2 + game.PushDistance));
                        }
                        else
                        {
                            pirate.Sail(game.GetEnemyMotherships()[0].Location.Towards(close.Holder, game.MothershipUnloadRange * 2 + game.PushDistance));
                        }
                    }
                    else if (pirate == game.GetMyCapsules()[0].Holder)
                    {
                        if (game.GetActiveWormholes().Length > 0 && xClosestToY(pirate, go.AvailableWormholesAsObject())[0] != null && pirate.Distance(xClosestToY(pirate, go.AvailableWormholesAsObject())[0]) < pirate.Distance(game.GetMyMotherships()[0]))
                        {
                            pirate.Sail(xClosestToY(pirate, go.AvailableWormholesAsObject())[0]);
                        }
                        else
                        {
                            pirate.Sail(game.GetMyMotherships()[0]);
                        }
                    }
                    else if (game.GetMyCapsules()[0].Holder == null && pirate == xClosestToY(game.GetMyCapsules()[0], go.MyLivingPiratesAsObject())[0])
                    {
                        pirate.Sail(game.GetMyCapsules()[0].InitialLocation);
                    }
                    else if (game.GetMyCapsules()[0].Holder == null && pirate == xClosestToY(game.GetMyCapsules()[0], go.MyLivingPiratesAsObject())[1])
                    {
                        pirate.Sail((xClosestToY(game.GetMyCapsules()[0], go.MyLivingPiratesAsObject())[0] as Pirate).Location.Towards(game.GetMyMotherships()[0], game.PushRange + game.PirateMaxSpeed));
                    }
                    else if (game.GetMyCapsules()[0].Holder != null && pirate == xClosestToY(game.GetMyCapsules()[0].Holder, go.MyLivingPiratesAsObject())[1])
                    {
                        pirate.Sail((xClosestToY(game.GetMyCapsules()[0], go.MyLivingPiratesAsObject())[0] as Pirate).Location.Towards(game.GetMyMotherships()[0], game.PushRange + game.PirateMaxSpeed));
                    }
                    else
                    {
                        pirate.Sail(game.GetMyCapsules()[0].InitialLocation);
                    }
                }
            }
        }
        public static void Spaghettification(PirateGame game, GameObjects go)
        {
            foreach (Pirate pirate in game.GetMyLivingPirates())
            {
                if (pirate.HasCapsule())
                {
                    if (game.GetAllWormholes()[0].IsActive && pirate.Distance(game.GetAllWormholes()[0]) < pirate.Distance(game.GetAllWormholes()[2]))
                    {
                        pirate.Sail(game.GetAllWormholes()[0]);
                    }
                    else if (game.GetAllWormholes()[2].IsActive && pirate.Distance(game.GetAllWormholes()[2]) < pirate.Distance(game.GetAllWormholes()[4]))
                    {
                        pirate.Sail(game.GetAllWormholes()[2]);
                    }
                    else if (game.GetAllWormholes()[4].IsActive && pirate.Distance(game.GetAllWormholes()[4]) < pirate.Distance(game.GetMyMotherships()[0]))
                    {
                        pirate.Sail(game.GetAllWormholes()[4]);
                    }
                    else
                    {
                        pirate.Sail(game.GetMyMotherships()[0]);
                    }
                }
                else
                {
                    if (game.GetAllWormholes()[5].IsActive && pirate.Distance(game.GetAllWormholes()[5]) < pirate.Distance(game.GetAllWormholes()[3]) && pirate.Distance(game.GetAllWormholes()[5]) < pirate.Distance(game.GetMyCapsules()[0]))
                    {
                        pirate.Sail(game.GetAllWormholes()[5]);
                    }
                    else if (game.GetAllWormholes()[3].IsActive && pirate.Distance(game.GetAllWormholes()[3]) < pirate.Distance(game.GetAllWormholes()[1]) && pirate.Distance(game.GetAllWormholes()[3]) < pirate.Distance(game.GetMyCapsules()[0]))
                    {
                        pirate.Sail(game.GetAllWormholes()[3]);
                    }
                    else if (game.GetAllWormholes()[1].IsActive && pirate.Distance(game.GetAllWormholes()[1]) < pirate.Distance(game.GetMyCapsules()[0]))
                    {
                        pirate.Sail(game.GetAllWormholes()[1]);
                    }
                    else
                    {
                        pirate.Sail(game.GetMyCapsules()[0]);
                    }
                }

            }
        }
        public static void PullPushon(PirateGame game, GameObjects go)
        {
            foreach (Pirate pir in game.GetMyLivingPirates())
            {
                if (game.Turn < 11)
                    pir.Sail(game.GetAllWormholes()[1].Location.Towards(pir, game.WormholeSize + game.PushRange));
                else if (game.Turn < 13)
                {
                    if (go.MyLivingPirates.Length > 2 && pir == xClosestToY(game.GetAllWormholes()[1], go.MyLivingPiratesAsObject())[0] as Pirate || pir == xClosestToY(game.GetAllWormholes()[1], go.MyLivingPiratesAsObject())[1] as Pirate || pir == xClosestToY(game.GetAllWormholes()[1], go.MyLivingPiratesAsObject())[2] as Pirate || pir == xClosestToY(game.GetAllWormholes()[1], go.MyLivingPiratesAsObject())[3] as Pirate)
                        pir.Sail(game.GetAllWormholes()[1]);
                }
                else if (!TryPush(pir, game))
                {
                    if (pir.HasCapsule() && pir.InRange(game.GetAllWormholes()[1], 1600)) pir.Sail(game.GetAllWormholes()[1]);
                    else if (pir.HasCapsule()) pir.Sail(game.GetMyMotherships()[0]);
                    else if (game.GetAllWormholes()[0].Distance(game.GetMyMotherships()[0]) > game.MothershipUnloadRange * 2)
                        pir.Sail(game.GetAllWormholes()[0].Location.Towards(pir, game.WormholeSize + game.PushRange));
                    else if (pir == xClosestToY(go.MyCapsules[1], go.MyLivingPiratesAsObject())[0])
                    {
                        pir.Sail(game.GetMyCapsules()[1]);
                    }
                    else if (pir == xClosestToY(game.GetAllWormholes()[1], go.MyLivingPiratesAsObject())[0] && game.GetAllWormholes()[1].Distance(game.GetMyCapsules()[1]) > 200)
                    {
                        pir.Sail(game.GetAllWormholes()[1].Location.Towards(pir, game.WormholeSize + game.PushRange));
                    }
                    else
                    {
                        pir.Sail(game.GetMyCapsules()[1].InitialLocation);
                    }

                }
            }
        }
        public static void StateMachine(PirateGame game, GameObjects go)
        {
            foreach (Pirate pirate in game.GetMyLivingPirates())
            {
                if (pirate.StateName == "normal" && game.GetMyself().Score == 3 && game.Turn < 113)
                {
                    pirate.SwapStates((xClosestToY(pirate, go.MyLivingPirates.asMapObject())[1] as Pirate));
                }
                else if (pirate.HasCapsule() && pirate.StateName == "heavy")
                {
                    pirate.Sail(game.GetMyMotherships()[0]);
                }
                else if (pirate.HasCapsule() && pirate.StateName == "normal")
                {
                    pirate.SwapStates((xClosestToY(pirate, go.MyLivingPirates.asMapObject())[1] as Pirate));
                }
                else if (pirate.StateName == "heavy" && pirate.CanPush(xClosestToY(pirate, go.MyLivingPirates.asMapObject())[1] as Pirate) && pirate.Location.Equals(new Location(2527, 3920)))
                {
                    pirate.Push(xClosestToY(pirate, go.MyLivingPirates.asMapObject())[1] as Pirate, go.MyCapsules[0]);
                }
                else
                {
                    pirate.Sail(new Location(2527, 3920));
                }
            }
        }
        public static void DarkInvasion(PirateGame game, GameObjects go)
        {
            foreach (Pirate pirate in game.GetMyLivingPirates())
            {
                Capsule cap = xClosestToY(game.GetEnemyMotherships()[0], game.GetEnemyCapsules().asMapObject())[0] as Capsule;
                Capsule cap2 = xClosestToY(game.GetEnemyMotherships()[0], game.GetEnemyCapsules().asMapObject())[1] as Capsule;

                if (cap.Holder != null && pirate.CanPush(cap.Holder))
                {
                    pirate.Push(cap.Holder, game.GetEnemyMotherships()[0]);
                }
                else
                {
                    if (pirate == xClosestToY(cap, go.MyLivingPirates.asMapObject())[0] as Pirate || pirate == xClosestToY(cap, go.MyLivingPirates.asMapObject())[1] || pirate == xClosestToY(cap, go.MyLivingPirates.asMapObject())[2])
                    {
                        pirate.Sail(cap.Location.Towards(game.GetEnemyMotherships()[0], game.PirateMaxSpeed));
                    }
                    else if (pirate == xClosestToY(cap2, go.MyLivingPirates.asMapObject())[0] as Pirate || pirate == xClosestToY(cap2, go.MyLivingPirates.asMapObject())[1] || pirate == xClosestToY(cap2, go.MyLivingPirates.asMapObject())[2])
                    {
                        pirate.Sail(cap2.Location.Towards(game.GetEnemyMotherships()[0], game.PirateMaxSpeed));
                    }
                }
            }
        }
        public static void HeavyLifting(PirateGame game, GameObjects go)
        {
            foreach (Pirate pirate in game.GetMyLivingPirates())
            {
                if (game.GetEnemyCapsules()[0].Holder != null && PiratesInRangeOfPush(game, game.GetEnemyCapsules()[0].Holder).Count == 2 && pirate.CanPush(game.GetEnemyCapsules()[0].Holder))
                {
                    pirate.Push(game.GetEnemyCapsules()[0].Holder, new Location(4900, 6000));
                }
                else if (pirate == xClosestToY(game.GetEnemyCapsules()[0], go.MyLivingPirates.asMapObject())[0] as Pirate || pirate == xClosestToY(game.GetEnemyCapsules()[0], go.MyLivingPirates.asMapObject())[1] as Pirate)
                {
                    pirate.Sail(game.GetEnemyCapsules()[0].Location.Towards(pirate, game.PirateMaxSpeed));
                }
                else
                {
                    pirate.Sail(game.GetEnemyCapsules()[0].Location.Towards(pirate, game.PushDistance + game.PirateMaxSpeed));
                }

            }
        }
        public static void DeathStar(PirateGame game, GameObjects go)
        {
            foreach (Pirate pirate in game.GetMyLivingPirates())
            {
                if (pirate.HasCapsule())
                {
                    pirate.Sail(xClosestToY(pirate, game.GetAllWormholes().asMapObject())[0]);
                }
                else if (xClosestToY(game.GetMyCapsules()[0], go.MyLivingPirates.asMapObject())[0] as Pirate == pirate)
                {
                    pirate.Sail(game.GetMyCapsules()[0]);
                }
                else if (xClosestToY(game.GetMyCapsules()[0], go.MyLivingPirates.asMapObject())[1] as Pirate == pirate)
                {
                    pirate.Sail(game.GetMyCapsules()[0]);
                }
            }
        }
        public static void StaticVoid(PirateGame game, GameObjects go)
        {
            foreach (Pirate pirate in game.GetMyLivingPirates())
            {
                if (pirate.HasCapsule())
                {
                    pirate.Sail(game.GetMyMotherships()[0]);
                }
                else if (pirate == game.GetAllMyPirates()[6] && pirate.CanPush(game.GetAllAsteroids()[0]))
                {
                    pirate.Push(game.GetAllAsteroids()[0], game.GetEnemyMotherships()[0]);
                }
                else if (pirate == game.GetAllMyPirates()[6] && !pirate.CanPush(game.GetAllAsteroids()[0]))
                {
                    pirate.Sail(game.GetAllAsteroids()[0].InitialLocation.Towards(pirate, game.AsteroidSize + game.PirateMaxSpeed));
                }
                else if (pirate == game.GetAllMyPirates()[8])
                {
                    if (pirate.CanPush(game.GetAllAsteroids()[0]))
                        pirate.Push(game.GetAllAsteroids()[0], game.GetAllAsteroids()[0]);
                    else
                        pirate.Sail(new Location(200, 3600));
                }
                else if (xClosestToY(game.GetMyCapsules()[0], game.GetMyLivingPirates().asMapObject())[0] as Pirate == pirate)
                {
                    pirate.Sail(game.GetMyCapsules()[0]);
                }
                else if (xClosestToY(game.GetMyCapsules()[1], game.GetMyLivingPirates().asMapObject())[0] as Pirate == pirate)
                {
                    pirate.Sail(game.GetMyCapsules()[1]);
                }
                else
                {
                    pirate.Sail((xClosestToY(pirate, game.GetMyCapsules().asMapObject())[0] as Capsule).InitialLocation);
                }

            }
        }
        public static void Pushiner(PirateGame game, GameObjects go)
        {
            foreach (Pirate pirate in game.GetMyLivingPirates())
            {
                Wormhole wh = xClosestToY(pirate, game.GetAllWormholes().asMapObject())[0] as Wormhole;

                if (pirate.HasCapsule())
                {
                    if (pirate.InRange(game.GetAllWormholes()[1], game.PushDistance * 3))
                        pirate.Sail(game.GetAllWormholes()[1]);
                    else
                        pirate.Sail(game.GetMyMotherships()[0]);
                }
                else if (xClosestToY(game.GetEnemyMotherships()[0], go.MyLivingPirates.asMapObject())[0] as Pirate == pirate || xClosestToY(game.GetEnemyMotherships()[0], go.MyLivingPirates.asMapObject())[1] as Pirate == pirate || xClosestToY(game.GetEnemyMotherships()[0], go.MyLivingPirates.asMapObject())[2] as Pirate == pirate)
                {
                    Capsule closest = xClosestToY(game.GetEnemyMotherships()[0], go.EnemyCapsules.asMapObject())[0] as Capsule;
                    if (closest.Holder != null && pirate.CanPush(closest.Holder))
                        pirate.Push(closest.Holder, game.GetEnemyMotherships()[0]);
                    else
                        pirate.Sail(game.GetEnemyMotherships()[0].Location.Towards(closest, game.MothershipUnloadRange + game.PushDistance));
                }
                else if (wh == game.GetAllWormholes()[0] && pirate.CanPush(wh) && !wh.Location.Equals(game.GetMyMotherships()[0]))
                {
                    pirate.Push(wh, game.GetMyMotherships()[0]);
                }
                else if (wh == game.GetAllWormholes()[0] && !pirate.CanPush(wh) && !wh.Location.Equals(game.GetMyMotherships()[0]))
                {
                    pirate.Sail(wh.Location.Towards(game.GetMyMotherships()[0], game.WormholeSize + 200));
                }
                else if (wh == game.GetAllWormholes()[1] && pirate.CanPush(wh) && !wh.InRange(game.GetMyCapsules()[0], game.MothershipUnloadRange))
                {
                    pirate.Push(wh, game.GetMyCapsules()[0].Location.Towards(game.GetMyCapsules()[0], game.PushRange));
                }
                else if (wh == game.GetAllWormholes()[1] && !pirate.CanPush(wh) && !wh.InRange(game.GetMyCapsules()[0], game.MothershipUnloadRange))
                {
                    pirate.Sail(wh.Location.Towards(game.GetMyCapsules()[0], game.WormholeSize + 200));
                }
                else
                {
                    pirate.Sail(game.GetMyCapsules()[0].InitialLocation);
                }
            }
        }
        public static void SpaceRace(PirateGame game, GameObjects go)
        {
            Pirate p0 = game.GetAllMyPirates()[0];
            Pirate p1 = game.GetAllMyPirates()[1];
            Pirate p2 = game.GetAllMyPirates()[2];
            Mothership ms = game.GetMyMotherships()[0];
            if (game.Turn < 2)
            {
                p2.Push(p1, game.GetEnemyCapsules()[0]);
                p0.Push(p1, game.GetEnemyCapsules()[0]);
            }
            else if (game.Turn == 3)
            {
                p0.SwapStates(p1);
            }
            else if (game.GetEnemyCapsules()[0].Holder != null && p1.CanPush(game.GetEnemyCapsules()[0].Holder))
                p1.Push(game.GetEnemyCapsules()[0].Holder, new Location(9999, 2000));
            else
            {
                p2.Sail(ms);
            }

        }
        public static void GravitySlingshot(PirateGame game, GameObjects go)
        {
            // KINDA CHALLENGING :)
            List<int> dists = new List<int>();
            for (int i = 0; i <= game.GetMyLivingPirates().Length; i++)
            {
                dists.Add(i * 1200 + 1000);
            }
            int index = 0;
            int push = 0;
            foreach (Pirate pirate in game.GetMyLivingPirates())
            {
                if (pirate.HasCapsule())
                {
                    pirate.Sail(game.GetMyMotherships()[0]);
                }
                else if (game.GetMyCapsules()[0].Holder != null && pirate.CanPush(game.GetMyCapsules()[0].Holder) && push == 0)
                {
                    pirate.Push(game.GetMyCapsules()[0].Holder, game.GetMyMotherships()[0]);
                    push++;
                }
                else if ((xClosestToY(game.GetMyCapsules()[0].InitialLocation, go.MyLivingPirates.asMapObject())[0] as Pirate).StateName == "heavy" && pirate != xClosestToY(game.GetMyCapsules()[0].InitialLocation, go.MyLivingPirates.asMapObject())[0] as Pirate && pirate == Normal(game))
                {
                    pirate.SwapStates(xClosestToY(game.GetMyCapsules()[0].InitialLocation, go.MyLivingPirates.asMapObject())[0] as Pirate);
                }
                else if (xClosestToY(game.GetMyCapsules()[0].InitialLocation, go.MyLivingPirates.asMapObject())[1] as Pirate == pirate)
                {
                    pirate.Sail(game.GetMyCapsules()[0].InitialLocation);
                }
                else if (xClosestToY(game.GetMyCapsules()[0].InitialLocation, go.MyLivingPirates.asMapObject())[0] as Pirate == pirate)
                {
                    pirate.Sail(game.GetMyCapsules()[0].InitialLocation);
                }
                else
                {
                    pirate.Sail(game.GetMyMotherships()[0].Location.Towards(game.GetMyCapsules()[0].InitialLocation, dists[index]));
                    index++;
                }
            }
        }
        public static void NeutronStar(PirateGame game, GameObjects go)
        {
            foreach (Pirate pirate in game.GetMyLivingPirates())
            {
                if (pirate.HasCapsule())
                {
                    if (pirate.CanPush(game.GetAllAsteroids()[0]))
                        pirate.Push(game.GetAllAsteroids()[0], game.GetMyMotherships()[0]);
                    else
                        pirate.Sail(game.GetMyMotherships()[0]);
                }
                else if (pirate == game.GetAllMyPirates()[5] && game.Turn < 18)
                {
                    if (pirate.CanPush(game.GetAllAsteroids()[1]))
                        pirate.Push(game.GetAllAsteroids()[1], game.GetAllAsteroids()[1]);
                    else
                        pirate.Sail(new Location(3700, 500));
                }
                else if (pirate == game.GetAllMyPirates()[1] && game.Turn < 18 && game.Turn > 3)
                {
                    if (pirate.CanPush(game.GetAllAsteroids()[1]))
                        pirate.Push(game.GetAllAsteroids()[1], game.GetEnemyMotherships()[0]);
                    else
                        pirate.Sail(game.GetAllAsteroids()[1]);
                }
                else
                {
                    pirate.Sail(game.GetMyCapsules()[0].InitialLocation);
                }
            }
        }
        public static void RainFromHell(PirateGame game, GameObjects go)
        {
            Pirate pirate0 = game.GetAllMyPirates()[0];
            Pirate pirate1 = game.GetAllMyPirates()[1];
            Pirate pirate2 = game.GetAllMyPirates()[2];
            Mothership ms0 = game.GetEnemyMotherships()[0];
            Mothership ms1 = game.GetEnemyMotherships()[1];
            Mothership ms2 = game.GetEnemyMotherships()[2];
            Asteroid ast0 = game.GetAllAsteroids()[0];
            Asteroid ast1 = game.GetAllAsteroids()[1];
            if (game.Turn > 37)
            {
                Pirate enemy0 = game.GetEnemyLivingPirates().ClosestObject(ms1) as Pirate;
                Location loc = ms1.Location.Towards(game.GetEnemyCapsules()[3], game.PushDistance);
                foreach (Pirate pir in game.GetMyLivingPirates())
                {
                    if (pir.CanPush(enemy0))
                    {
                        pir.Push(enemy0, ast0);
                    }
                    else
                    {
                        pir.Sail(loc);
                    }
                }
            }
            else if (game.Turn < 19)
            {
                pirate0.Sail(ms0.Location.Towards(ms1, game.AsteroidSize + pirate0.MaxSpeed));
                pirate2.Sail(ms2.Location.Towards(ms1, game.AsteroidSize + pirate2.MaxSpeed));
                pirate1.Sail(ast0.Location.Towards(pirate1, game.AsteroidSize + pirate1.MaxSpeed + 1));
            }
            else if (game.Turn > 19)
            {
                if (pirate1.CanPush(ast0) || pirate1.CanPush(ast1))
                {
                    if (game.Turn > 23)
                        pirate1.Push(ast1, ms2);
                    else
                        pirate1.Push(game.GetAllAsteroids().ClosestObject(pirate1) as Asteroid, game.GetEnemyMotherships().ClosestObject(game.GetAllAsteroids().ClosestObject(pirate1) as Asteroid));
                }
                else if (pirate0.CanPush(ast0))
                {
                    pirate0.Push(ast0, ast0);
                }
                else if (pirate2.CanPush(ast1))
                {
                    pirate2.Push(ast1, ast1);
                }
                else
                {
                    pirate1.Sail(ast1.Location.Towards(pirate1, game.AsteroidSize + pirate1.MaxSpeed + 1));
                }
            }
        }
        public static Pirate Normal(PirateGame game)
        {

            foreach (Capsule capsule in game.GetMyCapsules())
            {
                foreach (Pirate pir in game.GetMyLivingPirates())
                {
                    StickyBomb bomber = game.GetAllStickyBombs().Any() ? game.GetAllStickyBombs().ClosestObject(pir) as StickyBomb : null;
                    if (bomber != null && pir.InRange(bomber, game.StickyBombExplosionRange)) continue;
                    if (pir.StateName != game.STATE_NAME_NORMAL || pir.HasCapsule()) continue;

                    if (game.GetMyLivingPirates().OnlyHolders().Count == 0)
                    {
                        if (pir.StateName == game.STATE_NAME_NORMAL && pir == game.GetMyLivingPirates().ClosestObject(capsule))
                        {
                            continue;
                        }
                        if (pir.StateName == game.STATE_NAME_NORMAL && game.GetMyLivingPirates().Length > 2)
                            return pir;
                    }
                    else if (pir.StateName == game.STATE_NAME_NORMAL && !pir.HasCapsule())
                        return pir;
                }
            }

            return null;
        }
        public static Pirate Heavy(PirateGame game)
        {
            foreach (Pirate pir in game.GetMyLivingPirates())
            {
                StickyBomb bomber = game.GetAllStickyBombs().Any() ? game.GetAllStickyBombs().ClosestObject(pir) as StickyBomb : null;
                if (bomber != null && pir == bomber.Carrier) continue;
                ;
                if (pir.StateName == game.STATE_NAME_HEAVY)
                    return pir;
            }
            return null;
        }
        public static List<MapObject> xClosestToY(MapObject x, List<MapObject> y)
        {
            return x != null && y != null ? y.OrderBy(o => o.Distance(x)).ToList() : null;
        }
        private static bool TryPush(Pirate pirate, PirateGame game)
        {
            if (game.GetEnemy().BotName == "25242")
            {
                foreach (Wormhole worm in game.GetAllWormholes())
                {
                    if (worm == game.GetAllWormholes()[1] && worm.Distance(game.GetMyCapsules()[1].InitialLocation) > 200 && pirate.CanPush(worm))
                    {
                        pirate.Push(worm, game.GetMyCapsules()[1].InitialLocation);
                        return true;
                    }
                    if (worm == game.GetAllWormholes()[0] && worm.Distance(game.GetMyMotherships()[0]) > game.MothershipUnloadRange * 2 && pirate.CanPush(worm))
                    {
                        pirate.Push(worm, game.GetMyMotherships()[0]);
                        return true;
                    }
                }
            }
            if (game.GetEnemy().BotName == "25240")
            {
                foreach (Pirate enemy in game.GetEnemyLivingPirates())
                {
                    // Check if the pirate can push the enemy
                    if (pirate.CanPush(enemy) && enemy.HasCapsule())
                    {
                        // Push enemy!
                        pirate.Push(enemy, enemy.InitialLocation);

                        // Print a message
                        System.Console.WriteLine("pirate " + pirate + " pushes " + enemy + " towards " + enemy.InitialLocation);

                        // Did push
                        return true;
                    }
                }
            }
            else
            {
                foreach (Pirate enemy in game.GetEnemyLivingPirates())
                {
                    if (pirate.CanPush(enemy) && game.GetEnemy().BotName == "25242")
                    {
                        // Push enemy!
                        pirate.Push(enemy, new Location(0, 6500));

                        // Print a message
                        System.Console.WriteLine("pirate " + pirate + " pushes " + enemy + " towards " + enemy.InitialLocation);

                        // Did push
                        return true;
                    }
                    // Check if the pirate can push the enemy
                    if (pirate.CanPush(enemy) && game.GetEnemy().BotName != "25237")
                    {
                        // Push enemy!
                        pirate.Push(enemy, enemy.InitialLocation);

                        // Print a message
                        System.Console.WriteLine("pirate " + pirate + " pushes " + enemy + " towards " + enemy.InitialLocation);

                        // Did push
                        return true;
                    }

                }
            }
            if (game.GetEnemy().BotName == "25237")
            {
                foreach (Wormhole worm in game.GetAllWormholes())
                {
                    if (pirate.CanPush(worm))
                    {
                        pirate.Push(worm, game.GetMyCapsules()[0].InitialLocation);
                        return true;
                    }
                }
            }

            if (game.GetEnemy().BotName == "25240")
            {
                foreach (Wormhole worm in game.GetAllWormholes())
                {
                    if (pirate.CanPush(worm) && worm == game.GetAllWormholes()[1])
                    {
                        pirate.Push(worm, game.GetMyMotherships()[0]);
                        return true;
                    }
                    if (pirate.CanPush(worm) && worm == game.GetAllWormholes()[0])
                    {
                        pirate.Push(worm, game.GetMyCapsules()[0]);
                        return true;
                    }
                }
            }


            // Go over all living asteroids
            foreach (Asteroid asteroid in game.GetLivingAsteroids())
            {
                // Check if the pirate can push the asteroid
                if (pirate.CanPush(asteroid) && game.GetEnemyCapsules().Length > 0)
                {
                    pirate.Push(asteroid, game.GetEnemyCapsules()[0]);
                    // Print a message
                    System.Console.WriteLine("pirate " + pirate + " pushes " + asteroid + " towards " + game.GetEnemyCapsules()[0]);

                    // Did push
                    return true;
                }
            }

            // Didn't push
            return false;
        }
        public static List<Pirate> EPiratesInRangeOfPush(PirateGame game, SpaceObject pir)
        {
            List<Pirate> list = new List<Pirate>();
            foreach (Pirate enemy in game.GetEnemyLivingPirates())
            {
                if (enemy.CanPush(pir))
                {
                    list.Add(enemy);
                }
            }
            return list;
        }
        public static List<Pirate> PiratesInRangeOfPush(PirateGame game, Pirate enemy)
        {
            List<Pirate> list = new List<Pirate>();
            foreach (Pirate pir in game.GetMyLivingPirates())
            {
                if (pir.CanPush(enemy))
                {
                    list.Add(pir);
                }
            }
            return list;
        }
        public static Dictionary<string, List<Dictionary<string, object>>> GetGo(PirateGame game)
        {
            // DICT OF ALL OBJCTS IN THE GAME
            Dictionary<string, List<Dictionary<string, object>>> Go = new Dictionary<string, List<Dictionary<string, object>>>();

            // INIT ALL OBJECTS AS LISTS OF DICTS
            List<Dictionary<string, object>> myPirates = new List<Dictionary<string, object>>();
            List<Dictionary<string, object>> enemyPirates = new List<Dictionary<string, object>>();
            List<Dictionary<string, object>> myCapsules = new List<Dictionary<string, object>>();
            List<Dictionary<string, object>> enemyCapsules = new List<Dictionary<string, object>>();
            List<Dictionary<string, object>> myMotherships = new List<Dictionary<string, object>>();
            List<Dictionary<string, object>> enemyMotherships = new List<Dictionary<string, object>>();
            List<Dictionary<string, object>> asteroids = new List<Dictionary<string, object>>();

            // FILL THE LISTS WITH GAME CURRENT DATA
            #region DATA FILL
            foreach (Pirate pirate in game.GetMyLivingPirates())
            {
                Dictionary<string, object> dict = new Dictionary<string, object>()
                {
                    {"id", pirate.Id},
                    {"col", pirate.Location.Col},
                    {"row", pirate.Location.Row},
                    {"pt", pirate.PushReloadTurns},
                    {"hc", pirate.HasCapsule()}
                };
                myPirates.Add(dict);
            }

            foreach (Pirate epirate in game.GetEnemyLivingPirates())
            {
                Dictionary<string, object> dict = new Dictionary<string, object>()
                {
                    {"id", epirate.Id},
                    {"col", epirate.Location.Col},
                    {"row", epirate.Location.Row},
                    {"pt", epirate.PushReloadTurns},
                    {"hc", epirate.HasCapsule()}
                };
                enemyPirates.Add(dict);
            }

            foreach (Mothership ms in game.GetMyMotherships())
            {
                Dictionary<string, object> dict = new Dictionary<string, object>()
                {
                    {"id", ms.Id},
                    {"col", ms.Location.Col},
                    {"row", ms.Location.Row}
                };
                myMotherships.Add(dict);
            }

            foreach (Mothership ems in game.GetEnemyMotherships())
            {
                Dictionary<string, object> dict = new Dictionary<string, object>()
                {
                    {"id", ems.Id},
                    {"col", ems.Location.Col},
                    {"row", ems.Location.Row}
                };
                enemyMotherships.Add(dict);
            }

            foreach (Capsule capsule in game.GetMyCapsules())
            {
                Dictionary<string, object> dict = new Dictionary<string, object>()
                {
                    {"id", capsule.Id},
                    {"col", capsule.Location.Col},
                    {"row", capsule.Location.Row},
                    {"icol", capsule.InitialLocation.Col},
                    {"irow", capsule.InitialLocation.Row},
                    {"ah", capsule.Holder ?? null},
                };
                myCapsules.Add(dict);
            }

            foreach (Capsule ecapsule in game.GetEnemyCapsules())
            {
                Dictionary<string, object> dict = new Dictionary<string, object>()
                {
                    {"id", ecapsule.Id},
                    {"col", ecapsule.Location.Col},
                    {"row", ecapsule.Location.Row},
                    {"icol", ecapsule.InitialLocation.Col},
                    {"irow", ecapsule.InitialLocation.Row},
                    {"ah", ecapsule.Holder ?? null},
                };
                enemyCapsules.Add(dict);
            }

            foreach (Asteroid asteroid in game.GetLivingAsteroids())
            {
                Dictionary<string, object> dict = new Dictionary<string, object>()
                {
                    {"id", asteroid.Id},
                    {"col", asteroid.Location.Col},
                    {"row", asteroid.Location.Row}
                };
                asteroids.Add(dict);
            }
            #endregion

            // APPEND ALL DATA TO MAIN DICT
            Go.Add("p", myPirates);
            Go.Add("ep", enemyPirates);
            Go.Add("ms", myMotherships);
            Go.Add("ems", enemyMotherships);
            Go.Add("mc", myCapsules);
            Go.Add("ec", enemyCapsules);
            Go.Add("ast", asteroids);
            // RETURN THE DICT
            return Go;
        }

        public static Dictionary<string, Dictionary<int, List<Location>>> GetDict(PirateGame game)
        {
            Dictionary<string, Dictionary<int, List<Location>>> dict = new Dictionary<string, Dictionary<int, List<Location>>>();
            Dictionary<int, List<Location>> ahDict = new Dictionary<int, List<Location>>();
            Dictionary<int, List<Location>> ehDict = new Dictionary<int, List<Location>>();

            game.GetAllMyPirates().ToList().ForEach(x => ahDict[x.Id] = new List<Location>());
            game.GetAllEnemyPirates().ToList().ForEach(x => ehDict[x.Id] = new List<Location>());

            dict["e"] = ehDict;
            dict["a"] = ahDict;

            return dict;
        }
        public static Dictionary<string, Dictionary<int, bool>> GetPushed(PirateGame game)
        {
            Dictionary<string, Dictionary<int, bool>> dict = new Dictionary<string, Dictionary<int, bool>>();
            Dictionary<int, bool> astroPushedDict = new Dictionary<int, bool>();
            Dictionary<int, bool> enemyPushedDict = new Dictionary<int, bool>();
            Dictionary<int, bool> piratePushedDict = new Dictionary<int, bool>();
            game.GetMyLivingPirates().ToList().ForEach(x => piratePushedDict[x.Id] = false);
            game.GetLivingAsteroids().ToList().ForEach(x => astroPushedDict[x.Id] = false);
            game.GetEnemyLivingPirates().ToList().ForEach(x => enemyPushedDict[x.Id] = false);

            dict["enemy"] = enemyPushedDict;
            dict["astro"] = astroPushedDict;
            dict["ally"] = piratePushedDict;
            return dict;
        }
        public static GameObjects GetData(PirateGame game)
        {
            return new GameObjects(game.GetMyLivingPirates(), game.GetEnemyLivingPirates(), game.GetMyCapsules(), game.GetEnemyCapsules(), game.GetMyMotherships(), game.GetEnemyMotherships(), game.GetLivingAsteroids(), game.GetActiveWormholes());
        }
        public static Position calcPos(PirateGame game, Location p, Location obj)
        {
            //Positions: { 1:UP, 2:RIGHT, 3:DOWN, 4:LEFT, 5:UP_RIGHT, 6:DOWN_RIGHT, 7:DOWN_LEFT, 8:UP_LEFT, 9:SAME_SPOT }
            if (p.Equals(obj))
                return Position.SAME_SPOT;
            if (p.Row.Equals(obj.Row))
                if (p.Col < obj.Col)
                    return Position.RIGHT;
                else
                    return Position.LEFT;
            if (p.Col.Equals(obj.Col))
                if (p.Row < obj.Row)
                    return Position.DOWN;
                else
                    return Position.UP;
            if (p.Col < obj.Col)
                if (p.Row > obj.Row)
                    return Position.UP_RIGHT;
                else
                    return Position.DOWN_RIGHT;
            if (p.Col > obj.Col)
                if (p.Row > obj.Row)
                    return Position.UP_LEFT;
                else
                    return Position.DOWN_LEFT;
            return Position.SAME_SPOT;
        }
        public static Position MaamidLi(PirateGame game, GameObject p, Asteroid ast)
        {
            Location loc = ast.Location.Towards(ast.Direction, ast.Size);
            Position pos = calcPos(game, loc, p.Location);
            switch (pos)
            {
                case Position.UP:
                    return Position.RIGHT;
                case Position.RIGHT:
                    return Position.RIGHT;
                case Position.DOWN:
                    return Position.LEFT;
                case Position.LEFT:
                    return Position.LEFT;
                case Position.UP_RIGHT:
                    return Position.RIGHT;
                case Position.DOWN_RIGHT:
                    return Position.RIGHT;
                case Position.DOWN_LEFT:
                    return Position.LEFT;
                case Position.UP_LEFT:
                    return Position.LEFT;
                case Position.SAME_SPOT:
                    return Position.RIGHT;
                default:
                    break;
            }
            return Position.SAME_SPOT;
        }
        public static Position getPosition(PirateGame game, GameObject p, Asteroid ast)
        {
            Line l1 = new Line(ast.Location, ast.Location.Add(ast.Direction));
            int num = l1.LineDistance(p.Location);
            System.Console.WriteLine(num);
            if (num > 0)
                return Position.RIGHT;
            else
                return Position.LEFT;
        }
        public static List<Pirate> enemysAroundPirate(PirateGame game, Pirate pirate)
        {
            List<Pirate> l = new List<Pirate>();
            Mothership closestMS = game.GetMyMotherships().Any() ? game.GetMyMotherships().ClosestObject(pirate) as Mothership : null;
            foreach (Pirate enemy in game.GetEnemyLivingPirates())
            {
                if (enemy.InRange(pirate, game.PushRange + enemy.MaxSpeed * 2) && enemy.PushReloadTurns < 3 && closestMS != null && enemy.Distance(closestMS) - pirate.Distance(closestMS) <= game.PushRange)
                {
                    l.Add(enemy);
                }
            }
            return l;
        }
        public static List<Pirate> enemysNearPirate(PirateGame game, Pirate pirate)
        {
            List<Pirate> l = new List<Pirate>();
            Mothership closestMS = game.GetMyMotherships().Any() ? game.GetMyMotherships().ClosestObject(pirate) as Mothership : null;
            foreach (Pirate enemy in game.GetEnemyLivingPirates())
            {
                if (enemy.InRange(pirate, game.PushRange + enemy.MaxSpeed + 1) && enemy.PushReloadTurns < 2 && closestMS != null && enemy.Distance(closestMS) < pirate.Distance(closestMS))
                {
                    l.Add(enemy);
                }
            }
            return l;
        }
        public static bool NeedToGoToWH(PirateGame game, Pirate holder, Wormhole wh)
        {
            // if he is the only capsule we have, go 
            if (game.GetMyLivingPirates().OnlyHolders().Count == 1) return true;
            else
            {
                foreach (Pirate pirate in game.GetMyLivingPirates().OnlyHolders())
                {
                    if (holder != pirate && holder.Distance(wh) <= pirate.Distance(wh))
                        return true;
                }
                return false;
            }
        }
        public static bool weCanExplode(PirateGame game, Pirate pirate)
        {
            // CHECKS IF AN ENEMY CAN PUSH US OUT OF MAP OR ON ASTRO

            // GETS ALL THE PIRATES THAT CAN PUSH THIS PIRATE
            List<Pirate> enemys = EPiratesCanPushObject(game, pirate);

            // RETURN FALSE IF NONE CAN PUSH THIS PIRATE
            if (enemys.Any())
            {

                // GETS THE MAXIMUM AMOUT OF PUSH THEY CAN GET
                int pushSum = PushSumList(enemys);

                // CLOSEST ENEMY TO THIS PIRATE
                Pirate closest = enemys.ToArray().ClosestObject(pirate) as Pirate;

                // PIRATE LOCATION
                int col = pirate.Location.Col;
                int row = pirate.Location.Row;

                // IF THEY CAN PUSH THIS PIRATE OUT OF MAP
                if (row + pushSum > game.Rows)
                {
                    pirate.Sail(pirate.Location.Add(new Location(-pirate.MaxSpeed, 0)));
                    return true;
                }
                if (row - pushSum < 0)
                {
                    pirate.Sail(pirate.Location.Add(new Location(pirate.MaxSpeed, 0)));
                    return true;
                }
                if (col + pushSum > game.Cols)
                {
                    pirate.Sail(pirate.Location.Add(new Location(0, -pirate.MaxSpeed)));
                    return true;
                }
                if (col - pushSum < 0)
                {
                    pirate.Sail(pirate.Location.Add(new Location(0, pirate.MaxSpeed)));
                    return true;
                }
                // ALSO WE NEED TO CHECK IF THEY CAN PUSH ASTEROID ON US
                foreach (Asteroid asteroid in game.GetLivingAsteroids())
                {
                    // NOW WE CHECK IF THEY CAN PUSH US TO ASTRO
                    if (enemys.Any() && PushSumList(enemys) >= pirate.Distance(asteroid) - asteroid.Size)
                    {
                        System.Console.WriteLine("MEZIK FROM AST 2");
                        pirate.Sail(pirate.Location.Towards(asteroid, -pirate.MaxSpeed));
                        return true;
                    }
                }
            }

            System.Console.WriteLine("NOTHING...");
            // DIDNT FIND ANYTHING, RETURN FALSE
            return false;
        }
        public static List<Pirate> EPiratesCanPushObject(PirateGame game, SpaceObject obj)
        {
            List<Pirate> list = new List<Pirate>();
            foreach (Pirate pir in game.GetEnemyLivingPirates())
            {
                if (pir.CanPush(obj))
                {
                    list.Add(pir);
                }
            }
            return list;
        }
        public static int PushSumList(List<Pirate> list)
        {
            return list.Sum(x => x.PushDistance);
        }
        public static Pirate closestEnemyWithoutPush(PirateGame game, MapObject obj)
        {
            List<Pirate> list = new List<Pirate>();
            foreach (Pirate enemy in game.GetEnemyLivingPirates())
            {
                if (enemy.PushReloadTurns != 0) list.Add(enemy);
            }

            if (!list.Any()) return null;

            return list.OrderBy(x => x.Distance(obj)).First();
        }
    }
}
