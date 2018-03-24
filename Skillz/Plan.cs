using Pirates;
using System.Collections.Generic;

public class Plan
{
    public Location destination { get; set; }
    public List<Pirate> pirates { get; set; }
    public SpaceObject target { get; set; }
    public Dictionary<Pirate, int> pushDict { get; set; }

    public Plan (Location destination, List<Pirate> pirates, SpaceObject target, Dictionary<Pirate,int> pushDict)
    {
        this.destination = destination;
        this.pirates = pirates;
        this.target = target;
        this.pushDict = pushDict;
    }
    public void ShowDetails()
    {
        string ids = "";
        pirates.ForEach(x => ids += $"{x.Id} ");
       System.Console.WriteLine($"TARGET: {target.Id} PUSHERS ID: {ids} DESTINATION: {destination}");
    }
}
