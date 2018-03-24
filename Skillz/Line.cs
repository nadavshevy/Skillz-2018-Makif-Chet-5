using Pirates;
public class Line // y = ax + b
{
    public double a { get; set; }
    public double b { get; set; }

    public Line(double a, Location loc)
    {
        this.a = a;
        double y = loc.Col;
        double x = loc.Row;
        this.b = y - a * x;
    }
    public Line(Location loc1, Location loc2)
    {
        double r = (loc1.Col - loc2.Col);
        a = r / (loc1.Row - loc2.Row);
        double y = loc1.Col; double x = loc1.Row;
        b = y - a * x;
    }
    public double Turtle()
    {
        return (-1) / a;
    }
    public double getYOfX(double x)
    {
        return a * x + b;
    }
    public double getXOfY(double y)
    {
        return (y - b) / a;
    }
    public Location Tariehu(Location loc, Pirate pirate)
    {
        int y = (int)(loc.Col + System.Math.Sqrt(System.Math.Pow(pirate.MaxSpeed, 2) / (1 + System.Math.Pow(a, 2))));
        return new Location((int)getXOfY(y), y);
    }
    public Location Hitoch(Line line)
    {
        if (line.a == a && line.b == b) return null;
        int x = (int)((line.b - b) / (a - line.a));
        int y = (int)(a * x + b);
        return new Location(x, y);
    }
    public int LineDistance(Location loc)
    {
        int x0 = loc.Row; int y0 = loc.Col;
        return (int)((this.a * -1 * x0 + y0 - this.b) / (System.Math.Sqrt(System.Math.Pow(this.a, 2) + 1)));
    }

    public void Tostring(PirateGame game)
    {
        game.Debug($"y = {this.a}x +- {this.b}");
    }
}

public class Lines // y = ax + b | b = y - ax | x = num  Y: ROWS , X: COLS
{
    public double a { get; set; } = 0;
    public double b { get; set; } = 0;

    public Lines(Location p1, Location p2)
    {
        double m = 0;
        if (p1.Col != p2.Col)
        {
            m = (p1.Row - p2.Row) / (p1.Col - p2.Col);
            this.a = m;
            this.b = p1.Row - (m * p1.Col);
        }
    }
    public Lines(double m, Location p1)
    {
        this.a = m;
        this.b = p1.Row - (m * p1.Col);
    }

    public Lines anach(Location p1)
    {
        double m = -1 / (a);
        return new Lines(m, p1);
    }
    public int getY(double x)
    {
        return (int)(a * x + b);
    }
    public Location moveOnLine(Location from, double distance)
    {
        int x = (int)(from.Col + System.Math.Sqrt(System.Math.Pow(distance, 2) / (1 + System.Math.Pow(a, 2))));
        return new Location(getY(x), x);
    }
    public int LineDistance(Location loc)
    {
        int x0 = loc.Row; int y0 = loc.Col;
        return (int)((this.a * -1 * x0 + y0 - this.b) / (System.Math.Sqrt(System.Math.Pow(this.a, 2) + 1)));
    }
}