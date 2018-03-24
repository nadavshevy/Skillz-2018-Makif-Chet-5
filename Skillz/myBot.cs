using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pirates;

namespace MyBot
{

    /*
          _   _   _           _         ____                                 
         | | | | (_)   __ _  | |__     | __ )   _ __   _   _   _ __     ___  
         | |_| | | |  / _` | | '_ \    |  _ \  | '__| | | | | | '_ \   / _ \ 
         |  _  | | | | (_| | | | | |   | |_) | | |    | |_| | | | | | | (_) |
         |_| |_| |_|  \__, | |_| |_|   |____/  |_|     \__,_| |_| |_|  \___/  *LIAM TARI*
                      |___/                                                                        
     */

    public class TutorialBot : IPirateBot
    {
        #region GLOBAL VARS
        Dictionary<int, List<Location>> ehDict = null;
        Dictionary<int, List<Location>> ahDict = null;
        List<Pirate> pirates = new List<Pirate>();
        #endregion

        // DO A TURN
        public void DoTurn(PirateGame game)
        {
            // NASE UH TFOS
            try
            {
                if (ehDict == null) ehDict = Challenges.GetDict(game)["e"];
                if (ahDict == null) ahDict = Challenges.GetDict(game)["a"];


                Challenges.switchedThisTurn = false;
                Challenges.stickedThisTurn = false;
                HandleHistory(game);

                pirates = game.GetMyLivingPirates().ToList();

                if (!pirates.Any()) return;

                if (!Challenges.MissionChooser(game))
                {
                    LookForPlans(game);
                    System.Console.WriteLine($"PIRATES AVAIL: {pirates.Count}");
                    foreach (Pirate pirate in pirates)
                    {
                        if (!TryPush(game, pirate))
                        {
                            MainHandler(game, pirate);
                        }
                    }
                }
            }
            catch
            {
                System.Console.WriteLine("2:59 AM <3");
            }
        }
        #region PLANS HANLDER
        // THIS REGION IS MAINLY FOR POSITIONING AND KILLING ENEMYS / OBJECTIVES
        // CHOOSES THE BEST PLAN TO PUSH A PIRATE / WH
        public Plan bestPlan(PirateGame game, Pirate enemy)
        {
            List<Plan> plans = new List<Plan>();

            if (AstroCanKill(game, enemy) != null)
                plans.Add(AstroCanKill(game, enemy));
            if (EnemyToAst(game, enemy) != null)
                plans.Add(EnemyToAst(game, enemy));
            if (EnemyCanExplode(game, enemy) != null)
                plans.Add(EnemyCanExplode(game, enemy));
            // LO AMOR LEIOT KAN AVAL LO MESHANE
            if (WHToPlace(game) != null && PositionWormhole(game, WHToPlace(game)) != null)
                plans.Add(PositionWormhole(game, WHToPlace(game)));

            if (!plans.Any()) return null;
            List<Plan> temp = new List<Plan>(plans);
            foreach (Plan plan in temp)
            {
                // CHECK IF PLAN IS POSSIBLE
                if (!plan.pirates.All(pirates.Contains))
                    plans.Remove(plan);
            }
            if (!plans.Any()) return null;
            // ORDERS THE PLAN BY AMOUNT OF PIRATES NEEDED
            return plans.OrderBy(x => x.pirates.Count).First();
        }

        // EXCEXUTES THE DESIRED PLAN
        public void executePlan(PirateGame game, Plan plan)
        {
            foreach (Pirate pir in plan.pirates)
            {
                if (plan.target is Asteroid)
                {
                    Asteroid ast = plan.target as Asteroid;
                    Location dest = plan.destination;
                    int amount = plan.pushDict[pir];
                    pir.Push(ast, ast.Location.Towards(dest, amount));
                    pirates.Remove(pir);
                }
                if (plan.target is Wormhole)
                {
                    Wormhole target = plan.target as Wormhole;
                    Location dest = plan.destination;
                    int amount = plan.pushDict[pir];
                    pir.Push(target, target.Location.Towards(dest, amount));
                    pirates.Remove(pir);
                }
                if (plan.target is Pirate)
                {
                    Pirate target = plan.target as Pirate;
                    Location dest = plan.destination;
                    int amount = plan.pushDict[pir];
                    pir.Push(target, target.Location.Towards(dest, amount));
                    pirates.Remove(pir);
                }

            }
        }

        // CHOOSES THE BEST PLAN FOR ENEMY PUT OF MAP (IF POSSIBLE)
        public Plan EnemyCanExplode(PirateGame game, Pirate enemy)
        {
            // TODO: ADD DIRECTION CALCULATIONS IN ORDER TO DECIDE WHICH PUSH WOULD GUARNTEE 100% EXPLODE
            List<Pirate> list = PiratesCanPushObject(game, enemy);
            list.OrderByDescending(x => x.PushDistance);
            int col = enemy.Location.Col;
            int row = enemy.Location.Row;
            for (int i = 1; i <= list.Count; i++)
            {
                List<Pirate> newList = list.GetRange(0, i);
                int pushSum = PushSumList(newList);
                Dictionary<Pirate, int> dict = new Dictionary<Pirate, int>();
                newList.ForEach(x => dict[x] = x.PushDistance);
                //ROWS
                if (row + pushSum - enemy.MaxSpeed > game.Rows)
                {
                    System.Console.WriteLine($"ENEMY OUT OF MAP FOUND ! ENEMY: {enemy.Id}");
                    Location loc = new Location(row + pushSum, col);
                    Plan plan = new Plan(loc, newList, enemy, dict);
                    return plan;
                }
                if (row - pushSum + enemy.MaxSpeed < 0)
                {
                    System.Console.WriteLine($"ENEMY OUT OF MAP FOUND ! ENEMY: {enemy.Id}");
                    Location loc = new Location(row - pushSum, col);
                    Plan plan = new Plan(loc, newList, enemy, dict);
                    return plan;
                }
                //COLS
                if (col + pushSum - enemy.MaxSpeed > game.Cols)
                {
                    System.Console.WriteLine($"ENEMY OUT OF MAP FOUND ! ENEMY: {enemy.Id}");
                    Location loc = new Location(row, col + pushSum);
                    Plan plan = new Plan(loc, newList, enemy, dict);
                    return plan;
                }
                if (col - pushSum + enemy.MaxSpeed < 0)
                {
                    System.Console.WriteLine($"ENEMY OUT OF MAP FOUND ! ENEMY: {enemy.Id}");
                    Location loc = new Location(row, col - pushSum);
                    Plan plan = new Plan(loc, newList, enemy, dict);
                    return plan;
                }
            }
            //CANT PUSH OUT OF MAP, SEND TI FARTHEST LOCATION FROM A LOCATION
            return null;
        }

        // CHOOSES THE BEST PLAN FOR ASTRO ON ENEMY (IF POSSIBLE)
        public Plan AstroCanKill(PirateGame game, Pirate enemy)
        {
            // BASICALLY A DIFFERENT VARIATION OF ENEMY CAN EXPLODE
            foreach (Asteroid ast in game.GetLivingAsteroids())
            {
                List<Pirate> list = PiratesCanPushObject(game, ast);
                if (!list.Any()) return null;
                list = list.OrderByDescending(x => x.PushDistance).ToList();
                for (int i = 1; i <= list.Count; i++)
                {
                    List<Pirate> newList = list.GetRange(0, i);
                    int pushSum = PushSumList(newList);
                    if (pushSum >= ast.Distance(enemy) - ast.Size + enemy.MaxSpeed && beneficialAstroPush(game, ast, ast.Distance(enemy) - ast.Size + enemy.MaxSpeed, enemy.Location))
                    {
                        // CREATE A PLAN
                        System.Console.WriteLine($"AST TO ENEMY FOUND ! ENEMY: {enemy.Id}");
                        int needed = ast.Distance(enemy) - ast.Size + enemy.MaxSpeed;
                        Location loc = ast.Location.Towards(enemy, needed);
                        Dictionary<Pirate, int> distribution = distsDistribute(game, newList, needed);
                        Plan plan = new Plan(loc, newList, ast, distribution);
                        plan.ShowDetails();
                        return plan;
                    }
                }
            }
            return null;
        }

        // CHOOSES THE BEST PLAN FOR ENEMY ON ASTRO (IF POSSIBLE)
        public Plan EnemyToAst(PirateGame game, Pirate enemy)
        {
            foreach (Asteroid ast in game.GetLivingAsteroids().OrderByDescending(x => x.Distance(enemy)))
            {
                List<Pirate> list = PiratesCanPushObject(game, enemy);
                if (!list.Any()) return null;
                list = list.OrderByDescending(x => x.PushDistance).ToList();

                for (int i = 1; i <= list.Count; i++)
                {
                    List<Pirate> newList = list.GetRange(0, i);
                    int pushSum = PushSumList(newList);
                    if (pushSum >= ast.Location.Add(ast.Direction).Distance(enemy) - ast.Size + enemy.MaxSpeed)
                    {
                        System.Console.WriteLine($"Va ENEMY TO AST FOUND ! ENEMY: {enemy.Id}");
                        // CREATE A PLAN
                        int needed = ast.Location.Add(ast.Direction).Distance(enemy) - ast.Size + enemy.MaxSpeed;
                        Location loc = enemy.Location.Towards(ast.Location.Add(ast.Direction), needed);
                        Dictionary<Pirate, int> distribution = distsDistribute(game, newList, needed);
                        Plan plan = new Plan(loc, newList, enemy, distribution);
                        plan.ShowDetails();
                        return plan;
                    }
                }
            }
            return null;
        }

        // CHOOSES THE BEST PLAN FOR WH PUSHING
        public Plan PositionWormhole(PirateGame game, Wormhole wh)
        {
            List<Pirate> list = PiratesCanPushObject(game, wh);
            if (!list.Any()) return null;
            list = list.OrderByDescending(x => x.PushDistance).ToList();
            for (int i = 1; i <= list.Count; i++)
            {

                Mothership ms = game.GetMyMotherships().Any() ? game.GetMyMotherships().ClosestObject(wh) as Mothership : null;
                Mothership ems = game.GetMyMotherships().Any() ? game.GetEnemyMotherships().ClosestObject(wh) as Mothership : null;

                List<Pirate> newList = list.GetRange(0, i);
                Location loc = ms.Location.Towards(ems, -ms.UnloadRange);
                if (wh.InRange(loc, ms.UnloadRange)) return null;
                int pushSum = PushSumList(newList);
                if (pushSum >= loc.Distance(wh))
                {
                    // CREATE A PLAN FOR PRECISE PUSH TO THIS LOCATION
                    System.Console.WriteLine($"WH TO POSITION FOUND ! WH: {wh.Id}");
                    Location dest = ms.Location.Towards(ems, -ms.UnloadRange);
                    int needed = dest.Distance(wh);
                    Dictionary<Pirate, int> distribution = distsDistribute(game, newList, needed);
                    Plan plan = new Plan(loc, newList, wh, distribution);
                    plan.ShowDetails();
                    return plan;
                }
                // IF THER'S NOT ENOUGH RANGE IN ORDER TO PRECISELY GET TO THIS POINT
                if (i == list.Count && pushSum < loc.Distance(wh))
                {
                    // CREATE A PLAN CLOSER PUSH TO THIS LOCATION
                    System.Console.WriteLine($"WH CLOSER TO POSITION FOUND ! WH: {wh.Id}");
                    Location dest = ms.Location.Towards(ems, -ms.UnloadRange);
                    int needed = pushSum;
                    Dictionary<Pirate, int> distribution = distsDistribute(game, newList, needed);
                    Plan plan = new Plan(loc, newList, wh, distribution);
                    plan.ShowDetails();
                    return plan;
                }
            }
            return null;
        }
        #endregion

        #region BOOLS
        // CHECKS IF PIRATE NEEDS TO RETURN TO ENEMY MS
        public bool NeedToReturn(PirateGame game, Pirate pirate, Wormhole wh)
        {
            Mothership ems = game.GetEnemyMotherships().Any() ? game.GetEnemyMotherships().ClosestObject(pirate) as Mothership : null;
            if (ems == null) return false;
            Pirate cCap = game.GetMyLivingPirates().OnlyHolders().Any() ? game.GetMyLivingPirates().OnlyHolders().ToArray().ClosestObject(wh.Partner) as Pirate : null;
            Pirate ePirateHolder = game.GetEnemyLivingPirates().Any() && game.GetEnemyLivingPirates().EOnlyHolders().Any() ? game.GetEnemyLivingPirates().EOnlyHolders().ToArray().ClosestObject(ems) as Pirate : null;
            if (ePirateHolder != null && ePirateHolder.InRange(ems, ePirateHolder.MaxSpeed + game.HeavyPushDistance) /*&& (wh.Partner.InRange(ePirateHolder, game.PushDistance) || wh.InRange(ePirateHolder, game.PushDistance)*/)
            {
                if (cCap != null && wh.Partner.Distance(cCap) <= cCap.MaxSpeed * 5)
                    return false;
                if (wh.Partner.Distance(ems) - ePirateHolder.Distance(ems) > pirate.PushRange)
                    return false;
                return true;
            }
            return false;
        }

        // CHECKS IF PIRATE NEEDS TO GO TO WH
        public bool NeedToGoToWH(PirateGame game, Pirate holder, Wormhole wh)
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

        // CHECKS IF OUR PIRATE CAN LEISHTAZEK ON WH TO PREVENT ENEMY FROM ENETERING
        public bool Shtazek(PirateGame game, Wormhole wh)
        {
            if (!game.GetEnemyLivingPirates().Any()) return false;

            if (!game.GetEnemyLivingPirates().EOnlyHolders().Any()) return false;

            Pirate eCap = game.GetEnemyLivingPirates().EOnlyHolders().ToArray().ClosestObject(wh) as Pirate;
            Pirate myCap = game.GetMyLivingPirates().OnlyHolders().Any() ? game.GetMyLivingPirates().OnlyHolders().ToArray().ClosestObject(wh.Partner) as Pirate : null;
            if (eCap.InRange(wh, eCap.MaxSpeed * 6))
            {
                if (myCap == null)
                    return true;
                if (myCap.Distance(wh.Partner) / myCap.MaxSpeed <= eCap.Distance(wh) / eCap.MaxSpeed)
                    return false;
                return true;
            }
            return false;
        }

        // CHECKS IF PIRATE OS FROM THIS LIST
        public bool isFromThisList(PirateGame game, List<Pirate> list, Pirate pirate)
        {
            return list.Contains(pirate);
        }
        // CHECKS IF WE GONNA PUSH A TARGET TO AST
        public bool ToAst(PirateGame game, Pirate pir, Location to, int sumPushes)
        {
            foreach (Asteroid ast in game.GetLivingAsteroids())
            {
                if (pir.Location.Towards(to, sumPushes).InRange(ast.Location.Add(ast.Direction), ast.Size))
                {
                    return true;
                }
            }
            return false;
        }
        // CHECKS IF WE GONNA PUSH A TARGET TO WH
        public bool ToWH(PirateGame game, Pirate pir, Location to, int sumPushes)
        {
            foreach (Wormhole wh in game.GetAllWormholes())
            {
                if (pir.Location.Towards(to, sumPushes).InRange(wh, wh.WormholeRange) && wh.TurnsToReactivate < 2)
                {
                    return true;
                }
            }
            return false;
        }
        // CHECK IF WE GONNA PUSH A TARGET TO BOMBER WITH 2 TURNS TO EXPLODE
        public bool ToBomber(PirateGame game, Pirate pir, Location to, int sumPushes)
        {
            foreach (StickyBomb bomb in game.GetAllStickyBombs())
            {
                if (pir.Location.Towards(to, sumPushes).InRange(bomb, bomb.ExplosionRange) && bomb.Countdown < 2)
                {
                    return true;
                }
            }
            return false;
        }
        // CHECK IF WE GONNA PUSH A TARGET TO BOMBER WITH 1 TURNS TO EXPLODE
        public bool ToBomber1(PirateGame game, Pirate pir, Location to, int sumPushes)
        {
            foreach (StickyBomb bomb in game.GetAllStickyBombs())
            {
                if (pir.Location.Towards(to, sumPushes).InRange(bomb, bomb.ExplosionRange) && bomb.Countdown < 1)
                {
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region MAIN
        // MAIN MOVMENTS OF THE BOT
        public void MainHandler(PirateGame game, Pirate pirate)
        {
            Mothership ems1 = game.GetEnemyMotherships().Any() ? game.GetEnemyMotherships().ClosestObject(pirate) as Mothership : null;
            Mothership ms = game.GetMyMotherships().Any() ? game.GetMyMotherships().ClosestObject(pirate) as Mothership : null;
            Location ccap = game.GetMyCapsules().InitiaLocations().Any() ? game.GetMyCapsules().InitiaLocations().ClosestObject(pirate) as Location : null;

            foreach (Pirate enemy in game.GetEnemyLivingPirates().EOnlyHolders())
            {
                Mothership ems = game.GetEnemyMotherships().Any() ? game.GetEnemyMotherships().ClosestObject(enemy) as Mothership : null;
                // CHECK IF WE CAN PUSH OUR ALLY TO THIER CAP HOLDER IN ORDER TO BOMB HIM
                if (PiratesCanPushObject(game, pirate).Count > 0 && pirate.Location.Towards(enemy, PiratesCanPushObject(game, pirate)[0].PushDistance).InRange(enemy, pirate.PushRange) && game.GetMyself().TurnsToStickyBomb == 0 && !Challenges.stickedThisTurn && !ToAst(game, pirate, enemy.Location, PiratesCanPushObject(game, pirate)[0].PushDistance) && (enemy.Distance(ems) - game.MothershipUnloadRange) / enemy.MaxSpeed - 1 > game.StickyBombCountdown)
                {
                    pirate.SailMezik(enemy);
                    return;
                }
            }

            // AWE DANAW - IF WE GOT PUSHED TO MS 
            if (pirate.HasCapsule())
            {
                if (ms != null && PiratesCanPushObject(game, pirate).Count > 0)
                {
                    if (PiratesCanPushObject(game, pirate).Count >= pirate.NumPushesForCapsuleLoss - 1)
                    {
                        if (pirate.Location.Towards(ms, PushSumList(PiratesCanPushObject(game, pirate).GetRange(0, pirate.NumPushesForCapsuleLoss - 1)) + pirate.MaxSpeed).InRange(ms, game.MothershipUnloadRange)
                            && !ToAst(game, pirate, ms.Location, PushSumList(PiratesCanPushObject(game, pirate).GetRange(0, pirate.NumPushesForCapsuleLoss - 1)) + pirate.MaxSpeed) && !ToWH(game, pirate, ms.Location, PushSumList(PiratesCanPushObject(game, pirate).GetRange(0, pirate.NumPushesForCapsuleLoss - 1)) + pirate.MaxSpeed))
                        {
                            pirate.Sail(ms);
                            return;
                        }
                    }
                    else
                    {
                        if (pirate.Location.Towards(ms, PushSumList(PiratesCanPushObject(game, pirate)) + pirate.MaxSpeed).InRange(ms, game.MothershipUnloadRange)
                            && !ToAst(game, pirate, ms.Location, PushSumList(PiratesCanPushObject(game, pirate)) + pirate.MaxSpeed) && !ToWH(game, pirate, ms.Location, PushSumList(PiratesCanPushObject(game, pirate)) + pirate.MaxSpeed))
                        {
                            pirate.Sail(ms);
                            return;
                        }
                    }
                }
            }
            // STICK BOMB BEHAVIOUR
            if (pirate.StickyBombs.Any())
            {
                if (pirate.HasCapsule())
                {
                    if (pirate.StickyBombs.OrderBy(x => x.Countdown).First().Countdown < 1)
                    {
                        List<Pirate> allys = game.GetMyLivingPirates().ToList();
                        allys.Remove(pirate);
                        Pirate ally = allys.ToArray().ClosestObject(pirate) as Pirate;
                        pirate.Sail(ally);
                        return;
                    }
                    // CHECKS IF WE CAN REACH OUR MS EVEN THO WE HAVE BOMB ON OUR HOLDER
                    if ((pirate.Distance(ms) - game.MothershipUnloadRange) / pirate.MaxSpeed <= pirate.StickyBombs.OrderBy(x => x.Countdown).First().Countdown)
                    {
                        System.Console.WriteLine("yay");
                        pirate.Sail(ms);
                        return;
                    }
                    foreach (Wormhole wh2 in game.GetAllWormholes().withReachTime(pirate))
                    {
                        if (wh2.Distance(pirate) + wh2.Partner.Distance(ms) <= pirate.Distance(ms) && NeedToGoToWH(game, pirate, wh2) && pirate.Distance(wh2) / pirate.MaxSpeed <= pirate.StickyBombs.OrderBy(x => x.Countdown).First().Countdown)
                        {
                            System.Console.WriteLine("awy");
                            pirate.Sail(wh2);
                            return;
                        }
                    }
                }
                // JUST GO AND SUICIDE ON CLOSESNT ENEMY
                if (game.GetEnemyLivingPirates().Any())
                {
                    System.Console.WriteLine("danaw");
                    Pirate enemy = game.GetEnemyLivingPirates().ClosestObject(pirate) as Pirate;
                    BeneficialSail(game, pirate);
                    return;
                }
            }

            // IF MELAVE AND HOLDER IS NEAR THIER CAP HOLDER TRY SailMezik TO HIM
            foreach (Pirate ecap in game.GetEnemyLivingPirates().EOnlyHolders())
            {
                if (piratesNearEnemy(game, ecap).Count >= ecap.NumPushesForCapsuleLoss && isFromThisList(game, piratesNearEnemy(game, ecap), pirate))
                {
                    pirate.SailMezik(ecap);
                    return;
                }
                Mothership ems = game.GetEnemyMotherships().Any() ? game.GetEnemyMotherships().ClosestObject(ecap) as Mothership : null;

                if (ems != null && PiratesCanPushObject(game, ecap).Count < ecap.NumPushesForCapsuleLoss && ecap.InRange((game.GetAllWormholes().ClosestObject(ems) as Wormhole).Partner, game.WormholeRange) && (game.GetAllWormholes().ClosestObject(ems) as Wormhole).TurnsToReactivate <= 2 && pirate.Distance(ecap) <= game.PushRange + game.PirateMaxSpeed * 2)
                {
                    pirate.SailMezik(ecap.Location.Towards(pirate, ecap.MaxSpeed));
                    return;
                }
            }

            // IF WE HAVE CAPSULE GO TO CLOSEST MS - in best way
            if (pirate.HasCapsule() && ms != null)
            {
                foreach (Wormhole wh2 in game.GetAllWormholes().withReachTime(pirate))
                {
                    if (wh2.Distance(pirate) + wh2.Partner.Distance(ms) <= pirate.Distance(ms) && NeedToGoToWH(game, pirate, wh2))
                    {
                        System.Console.WriteLine("tembel2");
                        pirate.SailMezik(wh2);
                        return;
                    }
                }

                System.Console.WriteLine("temobel");
                pirate.SailMezik(ms);
                return;
            }





            // SENDS CLOSEST PIRATE NEAR OUR MS
            if (ms != null && pirate == game.GetMyLivingPirates().ClosestObject(ms) as Pirate)
            {
                Pirate closestCap = game.GetMyLivingPirates().OnlyHolders().Any() ? game.GetMyLivingPirates().OnlyHolders().ToArray().ClosestObject(pirate) as Pirate : null;
                if (closestCap != null && game.GetEnemyLivingPirates().Any() && game.GetEnemyLivingPirates().ClosestObject(pirate).InRange(ms.Location.Towards(closestCap, game.PushRange + game.PushDistance + closestCap.MaxSpeed), game.PushRange))
                {
                    pirate.SailMezik(ms.Location.Towards(closestCap, game.PushRange + game.PushDistance + closestCap.MaxSpeed));
                    return;
                }
            }

            // SECOND PRIORITY HIS PICKING CAPS AND MELAVE
            foreach (Capsule cap in game.GetMyCapsules())
            {
                // IF HAVE HAVE NO HOLDERS SEND FIRST FOR EACH ONE
                if (cap.Holder == null)
                {
                    if (pirate == game.GetMyLivingPirates().ClosestObject(cap))
                    {
                        pirate.SailMezik(cap);
                        return;
                    }
                }
                // WE NEED MELAVE FOR EACH HOLDER
                else
                {
                    if (ms != null && game.GetMyLivingPirates().removeHolders().Any() && pirate == game.GetMyLivingPirates().removeHolders().ToArray().ClosestObject(cap))
                    {
                        pirate.SailMezik(cap.Location.Towards(ms, game.PushRange));
                        return;
                    }
                }
            }
            foreach (Wormhole wormhole in game.GetActiveWormholes())
            {
                if (ms != null && Shtazek(game, wormhole) && wormhole == game.GetActiveWormholes().ClosestObject(ms) && pirate == game.GetMyLivingPirates().removeHolders().ToArray().ClosestObject(wormhole))
                {
                    pirate.Sail(wormhole);
                    return;
                }
                if (NeedToReturn(game, pirate, wormhole))
                {
                    pirate.SailMezik(wormhole);
                    return;
                }
            }

            // IF PIRATE NEAR ENEMY MS AND ENEMIES HAVE CAPSULE, W8 FOR ENEMY CAPSULE
            if (ems1 != null && pirate.InRange(ems1, game.MothershipUnloadRange + pirate.MaxSpeed) && game.GetEnemyLivingPirates().Any() && game.GetEnemyLivingPirates().EOnlyHolders().Any())
            {
                pirate.SailMezik(ems1.Location.Towards(game.GetEnemyLivingPirates().EOnlyHolders().ToArray().ClosestObject(ems1), game.MothershipUnloadRange + ((Pirate)game.GetEnemyLivingPirates().EOnlyHolders().ToArray().ClosestObject(ems1)).MaxSpeed));
                return;
            }

            // PUSH WORMHOLES IF NEEDED (BOTH OF THEM)
            foreach (Wormhole wormhole in game.GetAllWormholes())
            {

                Mothership ems = game.GetEnemyMotherships().Any() ? game.GetEnemyMotherships().ClosestObject(pirate) as Mothership : null;

                if (ems != null && wormhole.InRange(ems, ems.UnloadRange + game.HeavyPushDistance) && pirate == game.GetMyLivingPirates().withPush().ClosestObject(wormhole) as Pirate)
                {
                    //pirate.SailMezik(ShortestWay(game, pirate, wormhole.Location));
                    pirate.SailMezik(wormhole.Location.Towards(pirate,/* wormhole.WormholeRange + pirate.MaxSpeed*/ game.PushRange));
                    return;
                }
                if (ms != null && pirate == game.GetMyLivingPirates().withPush().ClosestObject(wormhole) as Pirate && !wormhole.InRange(ms, game.MothershipUnloadRange + game.PirateMaxSpeed + game.PushDistance))
                {
                    pirate.SailMezik(wormhole.Location.Towards(pirate, /* wormhole.WormholeRange + pirate.MaxSpeed*/ game.PushRange));
                    return;
                }
            }

            // WE SEND 1 FOR DEF (PREFERABLY MORE NEAR ENEMY MS)
            foreach (Pirate ecap in game.GetEnemyLivingPirates().EOnlyHolders())
            {
                Mothership ems = game.GetEnemyMotherships().Any() ? game.GetEnemyMotherships().ClosestObject(ecap) as Mothership : null;

                if (ems != null && pirate == game.GetMyLivingPirates().ClosestObject(ems) as Pirate)
                {
                    pirate.SailMezik(ecap.Location.Towards(ems, game.PushRange + game.HeavyPushDistance));
                    return;
                }
            }

            if (ems1 != null && game.GetMyLivingPirates().removeHolders().Any() && pirate == game.GetMyLivingPirates().removeHolders().ToArray().ClosestObject(ems1) as Pirate && game.GetEnemyCapsules().Any())
            {
                Capsule cap = game.GetEnemyCapsules().ClosestObject(pirate) as Capsule;
                pirate.SailMezik(ems1.Location.Towards(cap, game.MothershipUnloadRange + game.PushRange));
                return;
            }

            Wormhole wh = game.GetAllWormholes().Any() && ms != null ? game.GetAllWormholes().ClosestObject(ms) as Wormhole : null;
            Mothership EnemyMS = game.GetEnemyMotherships().Any() ? game.GetEnemyMotherships().ClosestObject(pirate) as Mothership : null;

            if (wh != null && EnemyMS != null && wh != game.GetAllWormholes().ClosestObject(EnemyMS) && wh.IsActive)
            {
                if (NeedToReturn(game, pirate, wh))
                {
                    pirate.SailMezik(wh);
                    return;
                }
            }
            if (wh != null && !wh.InRange(ms.Location.Towards(EnemyMS, -ms.UnloadRange), ms.UnloadRange))
            {
                pirate.SailMezik(wh.Location.Towards(pirate, game.PushRange));
                return;
            }
            if (ms != null && game.GetEnemyLivingPirates().Any() && pirate == game.GetMyLivingPirates().ClosestObject(game.GetEnemyLivingPirates().ClosestObject(ms) as Pirate) as Pirate)
            {
                pirate.SailMezik(game.GetEnemyLivingPirates().ClosestObject(ms) as Pirate);
                return;
            }
            if (game.GetEnemyLivingPirates().Any() && game.GetEnemyLivingPirates().RemoveBombers().Any())
            {
                if (ems1 != null && game.GetEnemyLivingPirates().RemoveBombers().EOnlyHolders().Any())
                    pirate.SailMezik(game.GetEnemyLivingPirates().EOnlyHolders().ToArray().ClosestObject(pirate).GetLocation().Towards(ems1, game.PushRange));
                else
                {
                    pirate.SailMezik(game.GetEnemyLivingPirates().RemoveBombers().ClosestObject(pirate));
                }
                return;
            }
            if (game.GetMyCapsules().InitiaLocations().Any())
            {
                pirate.SailMezik(game.GetMyCapsules().InitiaLocations().ClosestObject(pirate));
                return;
            }
            else
            {
                return;
                // :)
            }
        }
        // LOOK FOR PLANS AND SWITCH HANDLING
        public void LookForPlans(PirateGame game)
        {
            Pirate pir = Switcheero(game);
            if (pir != null) pirates.Remove(pir);
            foreach (Pirate enemy in game.GetEnemyLivingPirates().EOnlyHolders())
            {
                Plan plan = bestPlan(game, enemy);
                if (plan != null) executePlan(game, plan);
            }
            foreach (Pirate enemy in game.GetEnemyLivingPirates().OrderByDescending(x => x.PushDistance))
            {
                Plan plan = bestPlan(game, enemy);
                if (plan != null) executePlan(game, plan);
            }
        }

        #endregion

        #region ACTING HANDLERS
        // CHECKS IF A PIRATE CAN STICK A BOMB ON EMEY HOLDER AND PREVENT HIM FROM GETTING TO HIS MS
        public bool TryStick(PirateGame game, Pirate pirate)
        {
            // IF WE HAVE COOLDOWN ON BOMB STICK GET OUT
            if (game.GetMyself().TurnsToStickyBomb != 0) return false;
            if (Challenges.stickedThisTurn) return false;
            // WE CHECK EACH ENEMY IF HE HAS CAPSULE AND HE CANT REACH TO IT'S MS WITHIN BOMB COUNTDOWN
            // * WE MIGHT HAVE TO TAKE IN CONSIDORATION ENEMY PIRATE PUSHES
            foreach (Pirate enemy in game.GetEnemyLivingPirates().EOnlyHolders())
            {
                // GET CLOSEST MS TO ENEMY HOLDER
                Mothership ems = game.GetEnemyMotherships().Any() ? game.GetEnemyMotherships().ClosestObject(enemy) as Mothership : null;
                // CHECKS IF A BOMB WILL SUCCEED IN TAKING DOWN THE CAP BEFORE IT ENTERS THE MS
                if (ems != null && !enemy.StickyBombs.Any() && pirate.InStickBombRange(enemy) && (enemy.Distance(ems) - game.MothershipUnloadRange) / enemy.MaxSpeed > game.StickyBombCountdown && PiratesCanPushObject(game, enemy).Count < enemy.NumPushesForCapsuleLoss)
                {
                    pirate.StickBomb(enemy);
                    Challenges.stickedThisTurn = true;
                    return true;
                }
            }
            return false;
        }

        // CHECKS IF A PIRATE CAN PUSH AN OBJECT (PIRATES,WH,ASTRO)
        public bool TryPush(PirateGame game, Pirate pirate)
        {
            //--- COMMON FOR BOTH CAPSULE AND NO CAPSULE---
            // IF WE CAN PUSH ALLY CAPSULE TO OUR MOTHERSHIP TO SCORE, PUSH HIM 
            Mothership ms = game.GetMyMotherships().Any() ? game.GetMyMotherships().ClosestObject(pirate) as Mothership : null;
            if (ms != null && pirate.HasCapsule() && pirate.Location.Towards(ms, pirate.MaxSpeed).InRange(ms, ms.UnloadRange))
                return false;
            foreach (Pirate allyPirate in game.GetMyLivingPirates())
            {
                Mothership closestMS = game.GetMyMotherships().Any() ? game.GetMyMotherships().ClosestObject(allyPirate) as Mothership : null;
                // TRIES TO PUSH CAP HOLDER TO MS
                if (allyPirate.HasCapsule() && closestMS != null && allyPirate.Location.Towards(closestMS, allyPirate.MaxSpeed).InRange(closestMS, closestMS.UnloadRange) && EPiratesCanPushObject(game, allyPirate).Count < allyPirate.NumPushesForCapsuleLoss && pirate.CanPush(allyPirate)) continue;

                if (closestMS != null && pirate.CanPush(allyPirate) && allyPirate.HasCapsule() && PiratesCanPushObject(game, allyPirate).Count > 0 && closestMS.Distance(allyPirate) + closestMS.UnloadRange >= game.PushRange)
                {
                    if (PiratesCanPushObject(game, allyPirate).Count >= allyPirate.NumPushesForCapsuleLoss - 1)
                    {
                        if (isFromThisList(game, PiratesCanPushObject(game, allyPirate).GetRange(0, allyPirate.NumPushesForCapsuleLoss - 1), pirate) && allyPirate.Location.Towards(closestMS, PushSumList(PiratesCanPushObject(game, allyPirate).GetRange(0, allyPirate.NumPushesForCapsuleLoss - 1)) + allyPirate.MaxSpeed).InRange(closestMS, game.MothershipUnloadRange)
                            && !ToAst(game, allyPirate, closestMS.Location, PushSumList(PiratesCanPushObject(game, allyPirate).GetRange(0, allyPirate.NumPushesForCapsuleLoss - 1)) + allyPirate.MaxSpeed) && !ToWH(game, allyPirate, closestMS.Location, PushSumList(PiratesCanPushObject(game, allyPirate).GetRange(0, allyPirate.NumPushesForCapsuleLoss - 1)) + allyPirate.MaxSpeed)
                            && !ToBomber1(game, allyPirate, closestMS.Location, PushSumList(PiratesCanPushObject(game, allyPirate).GetRange(0, allyPirate.NumPushesForCapsuleLoss - 1)) + allyPirate.MaxSpeed))

                        {
                            pirate.Push(allyPirate, closestMS.Location.Towards(allyPirate, game.MothershipUnloadRange - 1));
                            return true;
                        }
                    }
                    // SAME IF WE HAVE 1
                    else
                    {
                        if (isFromThisList(game, PiratesCanPushObject(game, allyPirate), pirate) && allyPirate.Location.Towards(closestMS, PushSumList(PiratesCanPushObject(game, allyPirate)) + allyPirate.MaxSpeed).InRange(closestMS, game.MothershipUnloadRange)
                            && !ToAst(game, allyPirate, closestMS.Location, PushSumList(PiratesCanPushObject(game, allyPirate)) + allyPirate.MaxSpeed) && !ToWH(game, allyPirate, closestMS.Location, PushSumList(PiratesCanPushObject(game, allyPirate)) + allyPirate.MaxSpeed)
                            && !ToBomber1(game, allyPirate, closestMS.Location, PushSumList(PiratesCanPushObject(game, allyPirate)) + allyPirate.MaxSpeed))

                        {
                            pirate.Push(allyPirate, closestMS.Location.Towards(allyPirate, game.MothershipUnloadRange - 1));
                            return true;
                        }
                    }
                }
            }

            foreach (Pirate ally in game.GetMyLivingPirates())
            {
                foreach (Pirate enemy in game.GetEnemyLivingPirates().EOnlyHolders())
                {
                    Mothership ems = game.GetEnemyMotherships().Any() ? game.GetEnemyMotherships().ClosestObject(enemy) as Mothership : null;
                    // CHECK IF WE CAN PUSH OUR ALLY TO THIER CAP HOLDER IN ORDER TO BOMB HIM
                    if (ems != null && pirate.CanPush(ally) && ally.Location.Towards(enemy, pirate.PushDistance).InRange(enemy, ally.PushRange) && game.GetMyself().TurnsToStickyBomb == 0 && !Challenges.stickedThisTurn && !ToAst(game, ally, enemy.Location, pirate.PushDistance) && !ToWH(game, ally, enemy.Location, pirate.PushDistance) && !ToBomber(game, ally, enemy.Location, pirate.PushDistance) && (enemy.Distance(ems) - game.MothershipUnloadRange) / enemy.MaxSpeed - 1 > game.StickyBombCountdown && ally == game.GetMyLivingPirates().ClosestObject(enemy))
                    {
                        List<Pirate> newList = game.GetMyLivingPirates().ToList();
                        newList.Remove(ally);
                        if (pirate == newList.ToArray().ClosestObject(ally))
                        {
                            pirate.Push(ally, pirate.Location.Towards(enemy, game.PushRange + enemy.MaxSpeed));
                            return true;
                        }
                    }
                }
            }

            foreach (Pirate enemy in game.GetEnemyLivingPirates().EOnlyHolders())
            {
                Mothership ems = game.GetEnemyMotherships().Any() ? game.GetEnemyMotherships().ClosestObject(enemy) as Mothership : null;
                if (PiratesCanPushObject(game, enemy).Count >= enemy.NumPushesForCapsuleLoss && isFromThisList(game, PiratesCanPushObject(game, enemy).GetRange(0, enemy.NumPushesForCapsuleLoss), pirate))
                {
                    pirate.Push(enemy, Aim(game, ehDict[enemy.Id], enemy.Location).Multiply(9999));
                    return true;
                }
                if (ems != null && PiratesCanPushObject(game, enemy).Count < enemy.NumPushesForCapsuleLoss && isFromThisList(game, PiratesCanPushObject(game, enemy), pirate) && enemy.InRange((game.GetAllWormholes().ClosestObject(ems) as Wormhole).Partner, game.WormholeRange) && (game.GetAllWormholes().ClosestObject(ems) as Wormhole).TurnsToReactivate < 2)
                {
                    pirate.Push(enemy, enemy.Location.Towards(ems, -pirate.PushDistance));
                    return true;
                }
            }

            if (TryStick(game, pirate)) return true;

            if (pirate.StickyBombs.Any() && pirate.StickyBombs.OrderBy(x => x.Countdown).First().Countdown < 1)
            {
                List<Pirate> allys = game.GetMyLivingPirates().ToList();
                allys.Remove(pirate);
                Pirate ally = allys.ToArray().ClosestObject(pirate) as Pirate;
                if (ally.HasCapsule())
                {
                    pirate.Push(ally, Aim(game, ahDict[ally.Id], ally.Location).Multiply(9999));
                    return true;
                }
                else
                {
                    pirate.Push(ally, ms);
                    return true;
                }
            }
            foreach (StickyBomb bomb in game.GetAllStickyBombs())
            {
                // BOMB CARRIR ISN'T NULL AND WE CAN PUSH THE CARRIER AWAY FROM US
                if (bomb.Carrier != null && pirate.CanPush(bomb.Carrier) && bomb.Countdown < 1 && AwAwAw(game, bomb, pirate) != null)
                {
                    pirate.Push(bomb.Carrier, AwAwAw(game, bomb, pirate));
                    return true;
                }
            }

            // IF WE CAN DROP ENEMY CAPSULE
            foreach (Pirate enemyPirate in game.GetEnemyLivingPirates())
            {
                Mothership closesEtMS = game.GetEnemyMotherships().Any() ? game.GetEnemyMotherships().ClosestObject(enemyPirate) as Mothership : null;
                if (enemyPirate.HasCapsule() && pirate.CanPush(enemyPirate) && PiratesCanPushObject(game, enemyPirate).Count >= enemyPirate.NumPushesForCapsuleLoss && isFromThisList(game, PiratesCanPushObject(game, enemyPirate).GetRange(0, enemyPirate.NumPushesForCapsuleLoss), pirate))
                {
                    pirate.Push(enemyPirate, Aim(game, ehDict[enemyPirate.Id], enemyPirate.Location).Multiply(9999));
                    return true;
                }
                // IF ONLY 1 PIRATE CAN PUSH ENEMY CAPSULE, AND ENEMIES CAN PUSH ENEMY CAPSULE TO ENEMY MS, PUSH WITH THEM TO FALL CAPSULE
                if (closesEtMS != null && enemyPirate.HasCapsule() && pirate.CanPush(enemyPirate) && PiratesCanPushObject(game, enemyPirate).Count < enemyPirate.NumPushesForCapsuleLoss && EPiratesCanPushObject(game, enemyPirate).Count > 0)
                {
                    if (enemyPirate.Location.Towards(closesEtMS, PushSumList(EPiratesCanPushObject(game, enemyPirate)) + enemyPirate.MaxSpeed).InRange(closesEtMS, game.MothershipUnloadRange))
                    {
                        pirate.Push(enemyPirate, closesEtMS);
                        return true;
                    }

                }
            }

            // ---------END COMMON ------------

            //START FOR EACH
            // FOR CAP HOLDERS
            if (pirate.HasCapsule())
            {
                if (pirate.HasCapsule() && EPiratesCanPushObject(game, pirate).Count < pirate.NumPushesForCapsuleLoss) return false;

                foreach (Pirate enemy in game.GetEnemyLivingPirates())
                {
                    // LEATIS DEFENSE 
                    if (pirate.CanPush(enemy) && enemysAroundPirate(game, pirate).Count == pirate.NumPushesForCapsuleLoss && EPiratesCanPushObject(game, pirate).Count < pirate.NumPushesForCapsuleLoss)
                    {
                        pirate.Push(enemy, Aim(game, ehDict[enemy.Id], enemy.Location).Multiply(9999));
                        return true;
                    }
                }
            }
            // FOR NOT CAP HOLDERS
            else
            {
                Mothership closestMS = game.GetMyMotherships().Any() ? game.GetMyMotherships().ClosestObject(pirate) as Mothership : null;
                foreach (Pirate holder in game.GetMyLivingPirates().OnlyHolders())
                {
                    // PUSH CAPSULE TO ANOTHER MELAVE
                    if (pirate.CanPush(holder) && closestMS != null && PiratesCanPushObject(game, holder).Count > 0 && holder.Distance(game.GetMyLivingPirates().ClosestObject(closestMS)) >= game.PushRange)
                    {
                        if (PiratesCanPushObject(game, holder).Count >= holder.NumPushesForCapsuleLoss - 1)
                        {
                            if (isFromThisList(game, PiratesCanPushObject(game, holder).GetRange(0, holder.NumPushesForCapsuleLoss - 1), pirate) && game.GetMyLivingPirates().ClosestObject(closestMS) != pirate && game.GetMyLivingPirates().ClosestObject(closestMS) != holder && game.GetMyLivingPirates().ClosestObject(closestMS).InRange(holder, PushSumList(PiratesCanPushObject(game, holder).GetRange(0, holder.NumPushesForCapsuleLoss - 1)) + game.PushRange + holder.MaxSpeed)
                                && !ToAst(game, holder, game.GetMyLivingPirates().ClosestObject(closestMS).GetLocation(), PushSumList(PiratesCanPushObject(game, holder).GetRange(0, holder.NumPushesForCapsuleLoss - 1)) + holder.MaxSpeed) && !ToWH(game, holder, game.GetMyLivingPirates().ClosestObject(closestMS).GetLocation(), PushSumList(PiratesCanPushObject(game, holder).GetRange(0, holder.NumPushesForCapsuleLoss - 1)) + holder.MaxSpeed)
                                && !ToBomber(game, holder, game.GetMyLivingPirates().ClosestObject(closestMS).GetLocation(), PushSumList(PiratesCanPushObject(game, holder).GetRange(0, holder.NumPushesForCapsuleLoss - 1)) + holder.MaxSpeed))

                            {
                                pirate.Push(holder, game.GetMyLivingPirates().ClosestObject(closestMS));
                                return true;
                            }
                        }
                        else
                        {
                            if (isFromThisList(game, PiratesCanPushObject(game, holder), pirate) && game.GetMyLivingPirates().ClosestObject(closestMS) != pirate && game.GetMyLivingPirates().ClosestObject(closestMS) != holder && game.GetMyLivingPirates().ClosestObject(closestMS).InRange(holder, PushSumList(PiratesCanPushObject(game, holder)) + game.PushRange + holder.MaxSpeed)
                                && !ToAst(game, holder, game.GetMyLivingPirates().ClosestObject(closestMS).GetLocation(), PushSumList(PiratesCanPushObject(game, holder)) + holder.MaxSpeed) && !ToWH(game, holder, game.GetMyLivingPirates().ClosestObject(closestMS).GetLocation(), PushSumList(PiratesCanPushObject(game, holder)) + holder.MaxSpeed)
                                && !ToBomber(game, holder, game.GetMyLivingPirates().ClosestObject(closestMS).GetLocation(), PushSumList(PiratesCanPushObject(game, holder)) + holder.MaxSpeed))

                            {
                                pirate.Push(holder, game.GetMyLivingPirates().ClosestObject(closestMS));
                                return true;
                            }
                        }
                    }
                    foreach (Wormhole wh in game.GetActiveWormholes())
                    {
                        // PUSH CAPSULE TO ANOTHER WH
                        if (pirate.CanPush(holder) && closestMS != null && PiratesCanPushObject(game, holder).Count > 0 && !holder.InRange(wh, holder.MaxSpeed + wh.WormholeRange))
                        {
                            if (PiratesCanPushObject(game, holder).Count >= holder.NumPushesForCapsuleLoss - 1)
                            {
                                if (ms != null && isFromThisList(game, PiratesCanPushObject(game, holder).GetRange(0, holder.NumPushesForCapsuleLoss - 1), pirate) && wh.InRange(holder, PushSumList(PiratesCanPushObject(game, holder).GetRange(0, holder.NumPushesForCapsuleLoss - 1)) + game.PushRange + holder.MaxSpeed) &&
                                    !ToAst(game, holder, game.GetMyLivingPirates().ClosestObject(closestMS).GetLocation(), PushSumList(PiratesCanPushObject(game, holder).GetRange(0, holder.NumPushesForCapsuleLoss - 1)) + holder.MaxSpeed) && wh.Distance(holder) + wh.Partner.Distance(ms) <= holder.Distance(ms) && wh != game.GetAllWormholes().ClosestObject(ms))
                                {
                                    pirate.Push(holder, wh);
                                    return true;
                                }
                            }
                            else
                            {
                                if (ms != null && isFromThisList(game, PiratesCanPushObject(game, holder), pirate) && wh.InRange(holder, PushSumList(PiratesCanPushObject(game, holder)) + game.PushRange + holder.MaxSpeed) &&
                                    !ToAst(game, holder, game.GetMyLivingPirates().ClosestObject(closestMS).GetLocation(), PushSumList(PiratesCanPushObject(game, holder)) + holder.MaxSpeed) && wh.Distance(holder) + wh.Partner.Distance(ms) <= holder.Distance(ms) && wh != game.GetAllWormholes().ClosestObject(ms))
                                {
                                    pirate.Push(holder, wh);
                                    return true;
                                }
                            }
                        }
                    }
                    //PUSH CAPSULE OVER ENEMIES
                    //TODO: WHERE TO PUSH
                    if (closestMS != null && enemysNearPirate(game, holder).Count >= holder.NumPushesForCapsuleLoss && pirate.CanPush(holder))
                    {
                        if (PiratesCanPushObject(game, holder).Count > 0)
                        {
                            if (PiratesCanPushObject(game, holder).Count >= holder.NumPushesForCapsuleLoss - 1)
                            {
                                if (isFromThisList(game, PiratesCanPushObject(game, holder).GetRange(0, holder.NumPushesForCapsuleLoss - 1), pirate) && !ToAst(game, holder, game.GetMyLivingPirates().ClosestObject(closestMS).GetLocation(), PushSumList(PiratesCanPushObject(game, holder).GetRange(0, holder.NumPushesForCapsuleLoss - 1)) + holder.MaxSpeed)
                                    && !ToWH(game, holder, game.GetMyLivingPirates().ClosestObject(closestMS).GetLocation(), PushSumList(PiratesCanPushObject(game, holder).GetRange(0, holder.NumPushesForCapsuleLoss - 1)) + holder.MaxSpeed)
                                    && !ToBomber(game, holder, game.GetMyLivingPirates().ClosestObject(closestMS).GetLocation(), PushSumList(PiratesCanPushObject(game, holder).GetRange(0, holder.NumPushesForCapsuleLoss - 1)) + holder.MaxSpeed))
                                {
                                    pirate.Push(holder, closestMS);
                                    return true;
                                }
                            }
                            else
                            {
                                if (isFromThisList(game, PiratesCanPushObject(game, holder), pirate) && !ToAst(game, holder, game.GetMyLivingPirates().ClosestObject(closestMS).GetLocation(), PushSumList(PiratesCanPushObject(game, holder)) + holder.MaxSpeed)
                                    && !ToWH(game, holder, game.GetMyLivingPirates().ClosestObject(closestMS).GetLocation(), PushSumList(PiratesCanPushObject(game, holder)) + holder.MaxSpeed)
                                    && !ToBomber(game, holder, game.GetMyLivingPirates().ClosestObject(closestMS).GetLocation(), PushSumList(PiratesCanPushObject(game, holder)) + holder.MaxSpeed))
                                {
                                    pirate.Push(holder, closestMS);
                                    return true;
                                }
                            }

                        }

                    }
                }

                foreach (Wormhole wormhole in game.GetActiveWormholes())
                {
                    foreach (Pirate enemy in game.GetEnemyLivingPirates())
                    {
                        Mothership closesEtMS = game.GetEnemyMotherships().Any() ? game.GetEnemyMotherships().ClosestObject(enemy) as Mothership : null;
                        // PUSH WORMHOLE TO ENEMY (NOT ENEMY CAPSULE)
                        //TODO : CAN TELEPORT PEOLA
                        if (closesEtMS != null && enemy.InRange(closesEtMS, game.MothershipUnloadRange + game.PushDistance * 2) && pirate.CanPush(wormhole) && wormhole.Location.Towards(enemy, game.PushDistance).InRange(enemy, wormhole.WormholeRange) && enemy.HasCapsule() && piratesNearEnemy(game, enemy).Count < enemy.NumPushesForCapsuleLoss)
                        {
                            pirate.Push(wormhole, Aim(game, ehDict[enemy.Id], enemy.Location));
                            return true;
                        }
                    }
                }
            }
            foreach (Asteroid ast in game.GetLivingAsteroids())
            {
                if (ast.InRange(ms, game.MothershipUnloadRange) && pirate.CanPush(ast) && ast.Direction.Equals(new Location(0, 0)))
                {
                    pirate.Push(ast, ast.Location.Towards(pirate, -pirate.PushDistance));
                    return true;
                }
            }

            return false;
        }
        public void kaka(PirateGame game, Pirate p)
        {
            Wormhole wh = game.GetAllWormholes().Any() ? game.GetAllWormholes().ClosestObject(p) as Wormhole : null;
            if (wh != null && wh.Partner.StickyBombs.Any()) p.Sail(wh.Location.Towards(p, wh.WormholeRange + p.MaxSpeed));
            else p.Sail(wh);
        }
        // SWITCH STATES BETWEEEN PIRATES
        public Pirate Switcheero(PirateGame game)
        {
            if (Challenges.switchedThisTurn) return null;
            foreach (Capsule cap in game.GetMyCapsules())
            {
                Pirate cToHolder = cap.Holder != null && game.GetMyLivingPirates().removeHolders().Any() ? game.GetMyLivingPirates().removeHolders().ToArray().ClosestObject(cap.Holder) as Pirate : null;
                Mothership ms = game.GetMyMotherships().Any() && cap.Holder != null ? game.GetMyMotherships().ClosestObject(cap.Holder) as Mothership : null;
                Pirate pirate = cap.Holder;

                // IF ENEMYS GONNA KILL US AS NORMAL WE SWITCH TO HEAVY
                if (cap.Holder != null && cap.Holder == pirate && cap.Holder.StateName == game.STATE_NAME_NORMAL && enemysAroundPirate(game, pirate).Count == pirate.NumPushesForCapsuleLoss && Challenges.Heavy(game) != null && !pirate.StickyBombs.Any())
                {
                    Challenges.switchedThisTurn = true;
                    Challenges.Heavy(game).SwapStates(pirate);

                    return Challenges.Heavy(game);
                }
                // IF HOLDER HAS NO DANGER CAN SWITCH BACK TO NORMAL
                if (cap.Holder != null && cap.Holder == pirate && cap.Holder.StateName == game.STATE_NAME_HEAVY && enemysAroundPirate(game, pirate).Count < game.NumPushesForCapsuleLoss && Challenges.Normal(game) != null)
                {
                    Challenges.switchedThisTurn = true;
                    Challenges.Normal(game).SwapStates(pirate);
                    return Challenges.Normal(game);
                }
                // IF PIRATE IS THE CLOSEST TO CAP INITAL LOC AND THERE IS NO HOLDERS SWITCH
                if (cap.Holder == null && (game.GetMyLivingPirates().ClosestObject(cap) as Pirate) != null && (game.GetMyLivingPirates().ClosestObject(cap) as Pirate).StateName == game.STATE_NAME_HEAVY && Challenges.Normal(game) != null && !(game.GetMyLivingPirates().ClosestObject(cap) as Pirate).StickyBombs.Any())
                {
                    Challenges.switchedThisTurn = true;
                    Challenges.Normal(game).SwapStates(game.GetMyLivingPirates().ClosestObject(cap) as Pirate);
                    return Challenges.Normal(game);
                }
                // IF OUR PIRATES CAN PUSH HOLDER MORE THAN CAP LOSS 
                if (cap.Holder != null && cap.Holder == pirate && cap.Holder.StateName == game.STATE_NAME_NORMAL && PiratesCanPushObject(game, cap.Holder).Count >= cap.Holder.NumPushesForCapsuleLoss && Challenges.Heavy(game) != null)
                {
                    Challenges.switchedThisTurn = true;
                    Challenges.Heavy(game).SwapStates(pirate);
                    return Challenges.Heavy(game);
                }
                // IF PIRATE/S CAN PUSH ALLY PERFECTLY TO MS
                if (cap.Holder != null && cToHolder != null && PiratesCanPushObject(game, cap.Holder).Count > 0 && pirate == cToHolder && cToHolder.StateName == game.STATE_NAME_NORMAL && Challenges.Heavy(game) != null)
                {
                    if (PiratesCanPushObject(game, cap.Holder).Count >= cap.Holder.NumPushesForCapsuleLoss - 1)
                    {
                        if (cap.Holder.Distance(ms) <= PushSumList(PiratesCanPushObject(game, cap.Holder).GetRange(0, cap.Holder.NumPushesForCapsuleLoss - 1)) + game.HeavyPushDistance - pirate.PushDistance + cap.Holder.MaxSpeed + game.MothershipUnloadRange + 1)
                        {
                            Challenges.switchedThisTurn = true;
                            Challenges.Heavy(game).SwapStates(pirate);
                            return Challenges.Heavy(game);
                        }
                    }
                    else if (cap.Holder != null && cap.Holder.Distance(ms) <= PushSumList(PiratesCanPushObject(game, cap.Holder)) - pirate.PushDistance + cap.Holder.MaxSpeed + game.HeavyPushDistance + game.MothershipUnloadRange + 1)
                    {
                        Challenges.switchedThisTurn = true;
                        Challenges.Heavy(game).SwapStates(pirate);
                        return Challenges.Heavy(game);
                    }
                }

                foreach (Pirate pir in game.GetMyLivingPirates())
                {
                    if (pir.StickyBombs.Any() && pir.StateName == game.STATE_NAME_HEAVY && Challenges.Normal(game) != null)
                    {
                        Challenges.switchedThisTurn = true;
                        Challenges.Normal(game).SwapStates(pir);
                        return Challenges.Normal(game);
                    }
                    //SWITCH THE HEAVY IN THE AREA OF THE BOMB TO NORMAL AND FINDING THE NORMAL TO SWITCH WITH THAT ISNT IN THE AREA.
                    StickyBomb bomber = game.GetAllStickyBombs().Any() ? game.GetAllStickyBombs().ClosestObject(pir) as StickyBomb : null;
                    if (bomber != null && pir != bomber.Carrier && pir.InRange(bomber, game.StickyBombExplosionRange) && pir.StateName == game.STATE_NAME_HEAVY && Challenges.Normal(game) != null)
                    {
                        Challenges.switchedThisTurn = true;
                        Challenges.Normal(game).SwapStates(pir);
                        return Challenges.Normal(game);
                    }
                }
            }

            return null;
        }
        #endregion

        #region HISTORY HANDLER
        // HISTORY - PREDICITING THE NEXT MOVE OF A PIRATE BY HIS LOCATION HISTORY
        private Tuple<Position, Location> ObjectDirection(PirateGame game, List<Location> MoveHistory)
        {
            // IF WE HAVE ENOUGH INFO
            if (MoveHistory.Count > 1)
            {
                // GETTING THE LAST MOVE OF THE PIRATE
                int lCol = MoveHistory.Last().Col;
                int lRow = MoveHistory.Last().Row;

                // GETTING THE SECOND LAST MOVE OF THE PIRATE
                int slCol = MoveHistory[MoveHistory.Count - 2].Col;
                int slRow = MoveHistory[MoveHistory.Count - 2].Row;

                // NOW WE CHECK FOR EACH COL, ROW IF IT WHETER DECREASED OR INCREASED
                //
                if (lRow - slRow > 0 && lCol == slCol) return new Tuple<Position, Location>(Position.DOWN, new Location(lRow - slRow, 0));
                //
                if (lRow - slRow < 0 && lCol == slCol) return new Tuple<Position, Location>(Position.UP, new Location(lRow - slRow, 0));
                //
                if (lCol - slCol < 0 && lRow == slRow) return new Tuple<Position, Location>(Position.LEFT, new Location(0, lCol - slCol));
                //
                if (lCol - slCol > 0 && lRow == slRow) return new Tuple<Position, Location>(Position.RIGHT, new Location(0, lCol - slCol));
                //
                if (lRow - slRow > 0 && lCol - slCol > 0) return new Tuple<Position, Location>(Position.DOWN_RIGHT, new Location(lRow - slRow, lCol - slCol));
                //
                if (lRow - slRow < 0 && lCol - slCol > 0) return new Tuple<Position, Location>(Position.UP_RIGHT, new Location(lRow - slRow, lCol - slCol));
                //
                if (lRow - slRow > 0 && lCol - slCol < 0) return new Tuple<Position, Location>(Position.DOWN_LEFT, new Location(lRow - slRow, lCol - slCol));
                //
                if (lRow - slRow < 0 && lCol - slCol < 0) return new Tuple<Position, Location>(Position.UP_LEFT, new Location(lRow - slRow, lCol - slCol));
                // PIRATE DIDNT MOVE
                if (lRow == slRow && lCol == slCol) return new Tuple<Position, Location>(Position.SAME_SPOT, new Location(0, 0));
            }
            // WE DIDNT FIND ANYTHING / LIST DOESNT HAVE ENOUGH INFO
            return null;
        }

        // HISTORY HANDLERS (DELETE UNNECESSERY DATA)
        public void clearHistoryIfNeeded()
        {
            foreach (List<Location> item in ehDict.Values)
            {
                if (item.Count > 3)
                    item.RemoveRange(0, 2);
            }
            foreach (List<Location> item in ahDict.Values)
            {
                if (item.Count > 3)
                    item.RemoveRange(0, 2);
            }
        }

        // ADDING HISTORT EACH TURN FOR EACH PIRATE IN THE GAME (ENEMY AND ALLY)
        public void HandleHistory(PirateGame game)
        {
            foreach (Pirate ep in game.GetEnemyLivingPirates())
            {
                if (ehDict.Keys.Contains(ep.Id))
                    ehDict[ep.Id].Add(ep.Location);
            }
            foreach (Pirate p in game.GetMyLivingPirates())
            {
                if (ahDict.Keys.Contains(p.Id))
                    ahDict[p.Id].Add(p.Location);
            }
            clearHistoryIfNeeded();
        }
        #endregion

        #region UTILS

        // FINDS THE CLOSEST ENEMY DEFENDER ON OUR MS WITH PUSH
        public Dictionary<Pirate, int> distsDistribute(PirateGame game, List<Pirate> list, int needed)
        {
            Dictionary<Pirate, int> dic = new Dictionary<Pirate, int>();
            int sum = 0;
            foreach (Pirate pirate in list)
            {
                if (sum == needed) return dic;
                if (sum + pirate.PushDistance <= needed)
                {
                    sum += pirate.PushDistance;
                    System.Console.WriteLine("TAHAT - " + sum);
                    dic[pirate] = pirate.PushDistance;
                    if (sum == needed) return dic;
                }
                else
                {
                    int total = needed - sum;
                    sum += needed - sum;
                    System.Console.WriteLine("total push - " + total);
                    dic[pirate] = System.Math.Abs(total);
                    if (sum == needed) return dic;
                }
            }
            if (sum == needed) return dic;
            return null;
        }
        // CHECKS IF IT IS WORTH TO PUSH AN ASTRO
        public bool beneficialAstroPush(PirateGame game, Asteroid ast, int pushSum, Location dest)
        {
            int enemys = 0;
            int allys = 0;
            foreach (Pirate enemy in game.GetEnemyLivingPirates())
            {
                if (ast.Location.Towards(dest, pushSum).InRange(enemy, ast.Size))
                {
                    System.Console.WriteLine("enemy +");
                    if (enemy.HasCapsule()) enemys += 2;
                    else enemys += 1;
                }
            }
            foreach (Pirate ally in game.GetMyLivingPirates())
            {
                if (ast.Location.Towards(dest, pushSum).InRange(ally, ast.Size))
                {
                    System.Console.WriteLine("ally +");
                    if (ally.HasCapsule()) allys += 2;
                    else allys += 1;
                }
            }
            if (enemys - allys > 0) return true;
            return false;
        }
        // AIMS THE TARGETS LOCATION BY ITS HISTORY FOR MORE ACCURATE RESULT
        public Location Aim(PirateGame game, List<Location> MoveHistory, Location Target)
        {
            // ITEM1 = POSITION
            // ITEM2 = LOCATION TO ADD
            Tuple<Position, Location> degree = ObjectDirection(game, MoveHistory) != null ? ObjectDirection(game, MoveHistory) : null;
            if (degree == null) return Target;
            Position dir = degree.Item1;
            Location loc = degree.Item2;
            if (dir == 0) return Target;
            else
                return Target.Add(loc);
        }
        // CHECKS WHERE WE NEED TO SAIL WHILE WE HAVE BOMB ON A PIRATE
        public void BeneficialSail(PirateGame game, Pirate pirate)
        {
            int countAllies = 0;
            int countEnemies = 0;

            Pirate max = null;
            int maxDiff = 0;
            foreach (Pirate enemy1 in game.GetEnemyLivingPirates())
            {
                countAllies = 0;
                countEnemies = 0;
                Location dest = enemy1.Location;
                foreach (Pirate enemy in game.GetEnemyLivingPirates())
                {
                    if (pirate.Location.Towards(dest, pirate.MaxSpeed).InRange(enemy, game.StickyBombExplosionRange))
                    {
                        if (enemy.HasCapsule()) countEnemies += 2;
                        else countEnemies += 1;
                    }
                }
                foreach (Pirate ally in game.GetMyLivingPirates())
                {
                    if (pirate.Location.Towards(dest, pirate.MaxSpeed).InRange(ally, game.StickyBombExplosionRange) && ally != pirate)
                    {
                        if (ally.HasCapsule()) countAllies += 2;
                        else countAllies += 1;
                    }
                }
                if (max == null)
                {
                    if (countEnemies - countAllies > 0)
                    {
                        max = enemy1;
                        maxDiff = countEnemies - countAllies;
                    }
                }
                else
                {
                    if (countEnemies - countAllies > maxDiff)
                    {
                        max = enemy1;
                        maxDiff = countEnemies - countAllies;
                    }
                }
            }
            if (max != null)
            {
                if (pirate.StickyBombs.OrderBy(x => x.Countdown).First().Countdown == 0)
                {
                    System.Console.WriteLine("LU");
                    pirate.SailMezik(max);
                }
                else if (max.Distance(pirate) < max.MaxSpeed)
                {
                    System.Console.WriteLine("LULU");
                    pirate.SailMezik(max.Location.Towards(pirate, max.MaxSpeed - 1));
                }
                else
                {
                    System.Console.WriteLine("LULU");
                    pirate.SailMezik(max);
                }
            }
        }
        // CHECKS FOR BEST PLACE TO PUSH BOMBER
        public Location AwAwAw(PirateGame game, StickyBomb bomb, Pirate pirate)
        {
            Tuple<Location, int> best = null;
            foreach (Pirate enemy in game.GetEnemyLivingPirates())
            {
                Location loc = bomb.Location.Towards(enemy, pirate.PushDistance);
                int allys = 0;
                int enemys = 0;
                foreach (Pirate enemy1 in game.GetEnemyLivingPirates())
                {
                    if (loc.InRange(enemy1, game.StickyBombExplosionRange) && enemy1 != bomb.Carrier)
                    {
                        if (enemy1.HasCapsule()) enemys += 2;
                        else enemys++;
                    }
                }

                foreach (Pirate ally in game.GetMyLivingPirates())
                {
                    if (loc.InRange(ally, game.StickyBombExplosionRange) && ally != bomb.Carrier)
                    {
                        if (ally.HasCapsule()) allys += 2;
                        else allys++;
                    }
                }
                if (best == null) best = new Tuple<Location, int>(loc, enemys - allys);
                else if (best.Item2 < enemys - allys)
                {
                    best = new Tuple<Location, int>(loc, enemys - allys);
                }
            }
            if (best == null) return null;
            if (best != null && best.Item1.Equals(bomb.Location))
                return bomb.Location.Towards(pirate, -pirate.PushDistance);
            return best.Item1;
        }
        // SUMS THE AMOUNT OF PUSH FROM A LIST
        public int PushSumList(List<Pirate> list)
        {
            return list.Sum(x => x.PushDistance);
        }
        // CALCULATE WHICH WORMHOLE WE SHOULD FOCUS ON
        public Wormhole WHToPlace(PirateGame game)
        {
            if (!game.GetAllWormholes().Any() || !game.GetMyMotherships().Any()) return null;
            Mothership ms = game.GetMyMotherships().First();
            return game.GetAllWormholes().ClosestObject(ms) as Wormhole;
        }

        #endregion

        #region LISTS
        // CHECKS HOW MANY PIRATES CAN PUSH A CERTAIN OBJECT
        public List<Pirate> PiratesCanPushObject(PirateGame game, SpaceObject obj)
        {
            List<Pirate> list = new List<Pirate>();
            foreach (Pirate pir in game.GetMyLivingPirates())
            {
                if (pir.CanPush(obj))
                {
                    list.Add(pir);
                }
            }
            return list;
        }
        // CHECKS HOW MANY EPIRATES CAN PUSH A CERTAIN OBJECT
        public List<Pirate> EPiratesCanPushObject(PirateGame game, SpaceObject obj)
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
        // CHECKS HOW MANY ENEMYS IS NEAR A PIRATE (PUSH + SPEED + 1)
        public List<Pirate> enemysNearPirate(PirateGame game, Pirate pirate)
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
        // CHECKS HOW MANY ENEMYS IS NEAR A PIRATE (PUSH + SPEED + 1)
        public List<Pirate> piratesNearEnemy(PirateGame game, Pirate enemy)
        {
            List<Pirate> l = new List<Pirate>();
            Mothership closestMS = game.GetEnemyMotherships().Any() ? game.GetEnemyMotherships().ClosestObject(enemy) as Mothership : null;
            foreach (Pirate pirate in game.GetMyLivingPirates())
            {
                if (pirate.InRange(enemy, game.PushRange + pirate.MaxSpeed + 1) && pirate.PushReloadTurns < 2 && closestMS != null && System.Math.Abs(pirate.Distance(closestMS) - enemy.Distance(closestMS)) <= game.PushRange)
                {
                    l.Add(pirate);
                }
            }
            return l;
        }
        // CHECKS HOW MANY ENEMYS IS NEAR A PIRATE (PUSH + SPEED*2)
        public List<Pirate> enemysAroundPirate(PirateGame game, Pirate pirate)
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

        #endregion

        #region FOR THE MEMES
        // TUTORIAL ON HOW TO SailMezik NOT OUT OF MAP https://www.youtube.com/watch?v=6KKKGpAZHAA
        public void HowToSailNotOutOfMap(PirateGame game, Pirate pirate)
        {
            Pirate cEnemy = game.GetEnemyLivingPirates().Any() ? game.GetEnemyLivingPirates().ClosestObject(pirate) as Pirate : null;
            if (cEnemy == null) return;
            if (pirate.Location.Towards(cEnemy, -pirate.MaxSpeed).GetLocation().Col > game.Cols ||
                pirate.Location.Towards(cEnemy, -pirate.MaxSpeed).GetLocation().Col < 0)
            {
                pirate.SailMezik(new Location(pirate.Location.Row + pirate.MaxSpeed, pirate.Location.Col));
            }
            else
            {
                pirate.SailMezik(new Location(pirate.Location.Row, pirate.Location.Col + pirate.MaxSpeed));
            }
        }
        public Wormhole TheWHNeedToUse(PirateGame game, Pirate pirate)
        {
            if (game.GetAllWormholes().Any())
            {
                Mothership closestMS = game.GetMyMotherships().Any() ? game.GetMyMotherships().ClosestObject(pirate) as Mothership : null;
                if (closestMS == null) return null;
                return game.GetAllWormholes().ClosestObject(closestMS) as Wormhole;
            }
            return null;
        }
        #endregion

    }
}