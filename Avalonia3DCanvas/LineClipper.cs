using Avalonia;

namespace Avalonia3DCanvas;

public static class LineClipper
{
    private const int INSIDE = 0;
    private const int LEFT = 1;
    private const int RIGHT = 2;
    private const int BOTTOM = 4;
    private const int TOP = 8;

    private static int ComputeOutCode(double x, double y, double xMin, double yMin, double xMax, double yMax)
    {
        int code = INSIDE;

        if (x < xMin)
            code |= LEFT;
        else if (x > xMax)
            code |= RIGHT;

        if (y < yMin)
            code |= TOP;
        else if (y > yMax)
            code |= BOTTOM;

        return code;
    }

    public static bool ClipLine(ref Point p1, ref Point p2, Rect bounds)
    {
        double xMin = bounds.Left;
        double yMin = bounds.Top;
        double xMax = bounds.Right;
        double yMax = bounds.Bottom;

        double x0 = p1.X;
        double y0 = p1.Y;
        double x1 = p2.X;
        double y1 = p2.Y;

        int outCode0 = ComputeOutCode(x0, y0, xMin, yMin, xMax, yMax);
        int outCode1 = ComputeOutCode(x1, y1, xMin, yMin, xMax, yMax);

        while (true)
        {
            if ((outCode0 | outCode1) == 0)
            {
                p1 = new Point(x0, y0);
                p2 = new Point(x1, y1);
                return true;
            }
            else if ((outCode0 & outCode1) != 0)
            {
                return false;
            }
            else
            {
                int outCodeOut = outCode0 != 0 ? outCode0 : outCode1;

                double x = 0, y = 0;

                if ((outCodeOut & TOP) != 0)
                {
                    x = x0 + (x1 - x0) * (yMin - y0) / (y1 - y0);
                    y = yMin;
                }
                else if ((outCodeOut & BOTTOM) != 0)
                {
                    x = x0 + (x1 - x0) * (yMax - y0) / (y1 - y0);
                    y = yMax;
                }
                else if ((outCodeOut & RIGHT) != 0)
                {
                    y = y0 + (y1 - y0) * (xMax - x0) / (x1 - x0);
                    x = xMax;
                }
                else if ((outCodeOut & LEFT) != 0)
                {
                    y = y0 + (y1 - y0) * (xMin - x0) / (x1 - x0);
                    x = xMin;
                }

                if (outCodeOut == outCode0)
                {
                    x0 = x;
                    y0 = y;
                    outCode0 = ComputeOutCode(x0, y0, xMin, yMin, xMax, yMax);
                }
                else
                {
                    x1 = x;
                    y1 = y;
                    outCode1 = ComputeOutCode(x1, y1, xMin, yMin, xMax, yMax);
                }
            }
        }
    }
}
