using Pirates;
using System.Collections.Generic;

public class Assassinate
{
    public Location location { get; set; }
    public List<Pirate> pirates { get; set; }
    public Asteroid ast { get; set; }
    public Assassinate(Location location, List<Pirate> pirates)
    {
        this.pirates = pirates;
        this.location = location;
    }
    public Assassinate(Location location, List<Pirate> pirates, Asteroid ast)
    {
        this.pirates = pirates;
        this.location = location;
        this.ast = ast;
    }
}
