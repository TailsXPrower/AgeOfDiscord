using System;

public class EasingFunc {
    public static Func<double, double> Linear = x => x;
    public static Func<double, double> EaseInSinE = (x) => 1.0 - Math.Cos(x * Math.PI / 2.0);
    public static Func<double, double> EaseOutSinE = (x) => Math.Sin(x * Math.PI / 2.0);
    public static Func<double, double> EaseInOutSinE = (x) => -(Math.Cos(Math.PI * x) - 1.0) / 2.0;
    public static Func<double, double> EaseInQuad = (x) => x * x;
    public static Func<double, double> EaseOutQuad = (x) => 1.0 - (1.0 - x) * (1.0 - x);
    public static Func<double, double> EaseInOutQuad = (x) => x < 0.5 ? 2.0 * x * x : 1.0 - Math.Pow(-2.0 * x + 2.0, 2.0) / 2.0;
    public static Func<double, double> EaseInCubic = (x) => x * x * x;
    public static Func<double, double> EaseOutCubic = (x) => 1.0 - Math.Pow(1.0 - x, 3.0);
    public static Func<double, double> EaseInOutCubic = (x) => x < 0.5 ? 4.0 * x * x * x : 1.0 - Math.Pow(-2.0 * x + 2.0, 3.0) / 2.0;
    public static Func<double, double> EaseInQuart = (x) => x * x * x * x;
    public static Func<double, double> EaseOutQuart = (x) => 1.0 - Math.Pow(1.0 - x, 4.0);
    public static Func<double, double> EaseInOutQuart = (x) => x < 0.5 ? 8.0 * x * x * x * x : 1.0 - Math.Pow(-2.0 * x + 2.0, 4.0) / 2.0;
    public static Func<double, double> EaseInQuint = (x) => x * x * x * x * x;
    public static Func<double, double> EaseOutQuint = (x) => 1.0 - Math.Pow(1.0 - x, 5.0);
    public static Func<double, double> EaseInOutQuint = (x) => x < 0.5 ? 16.0 * x * x * x * x * x : 1.0 - Math.Pow(-2.0 * x + 2.0, 5.0) / 2.0;
    public static Func<double, double> EaseInExpo = (x) => x == 0.0 ? 0.0 : Math.Pow(2.0, 10.0 * x - 10.0);
    public static Func<double, double> EaseOutExpo = (x) => x == 1.0 ? 1.0 : 1.0 - Math.Pow(2.0, -10.0 * x);
    public static Func<double, double> EaseInOutExpo = (x) => x == 0.0 ? 0.0 : (x == 1.0 ? 1.0 : (x < 0.5 ? Math.Pow(2.0, 20.0 * x - 10.0) / 2.0 : (2.0 - Math.Pow(2.0, -20.0 * x + 10.0)) / 2.0));
    public static Func<double, double> EaseInCirc = (x) => 1.0 - Math.Sqrt(1.0 - Math.Pow(x, 2.0));
    public static Func<double, double> EaseOutCirc = (x) => Math.Sqrt(1.0 - Math.Pow(x - 1.0, 2.0));
    public static Func<double, double> EaseInOutCirc = (x) => x < 0.5 ? (1.0 - Math.Sqrt(1.0 - Math.Pow(2.0 * x, 2.0))) / 2.0 : (Math.Sqrt(1.0 - Math.Pow(-2.0 * x + 2.0, 2.0)) + 1.0) / 2.0;
    public static Func<double, double> EaseInBack = (x) => {
	    const double c1 = 1.70158;
	    const double c3 = c1 + 1.0;
        return c3 * x * x * x - c1 * x * x;
    };
    public static Func<double, double> EaseOutBack = (x) => {
	    const double c1 = 1.70158;
        const double c3 = c1 + 1.0;
        return 1.0 + c3 * Math.Pow(x - 1.0, 3.0) + c1 * Math.Pow(x - 1.0, 2.0);
    };
    public static Func<double, double> EaseInOutBack = (x) => {
	    const double c1 = 1.70158;
	    const double c2 = c1 * 1.525;
        return x < 0.5 ? Math.Pow(2.0 * x, 2.0) * ((c2 + 1.0) * 2.0 * x - c2) / 2.0 : (Math.Pow(2.0 * x - 2.0, 2.0) * ((c2 + 1.0) * (x * 2.0 - 2.0) + c2) + 2.0) / 2.0;
    };
    public static Func<double, double> EaseInElastic = (x) => {
	    const double c4 = 2.0943951023931953;
        return x == 0.0 ? 0.0 : (x == 1.0 ? 1.0 : -Math.Pow(2.0, 10.0 * x - 10.0) * Math.Sin((x * 10.0 - 10.75) * c4));
    };
    public static Func<double, double> EaseOutElastic = (x) => {
	    const double c4 = 2.0943951023931953;
        return x == 0.0 ? 0.0 : (x == 1.0 ? 1.0 : Math.Pow(2.0, -10.0 * x) * Math.Sin((x * 10.0 - 0.75) * c4) + 1.0);
    };
    public static Func<double, double> EaseInOutElastic = (x) => {
	    const double c5 = 1.3962634015954636;
        return x == 0.0 ? 0.0 : (x == 1.0 ? 1.0 : (x < 0.5 ? -(Math.Pow(2.0, 20.0 * x - 10.0) * Math.Sin((20.0 * x - 11.125) * c5)) / 2.0 : Math.Pow(2.0, -20.0 * x + 10.0) * Math.Sin((20.0 * x - 11.125) * c5) / 2.0 + 1.0));
    };
    public static Func<double, double> EaseOutBounce = (x) => {
	    const double n1 = 7.5625;
	    const double d1 = 2.75;
	    var d = n1 * x;
	    return x switch
	    {
		    < 1.0 / d1 => d * x,
		    < 2.0 / d1 => d = x - 1.5 / d1 * x + 0.75,
		    _ => x < 2.5 / d1 ? d = x - 2.25 / d1 * x + 0.9375 : d = x - 2.625 / d1 * x + 0.984375
	    };
    };
    public static Func<double, double> EaseInBounce = (x) => {
        return 1.0 - EaseOutBounce.Invoke(x - 1.0);
    };
    public static Func<double, double> EaseInOutBounce = (x) => {
        return x < 0.5 ? (1.0 - EaseOutBounce.Invoke(1.0 - 2.0 * x)) / 2.0 : (1.0 + EaseOutBounce.Invoke(2.0 * x - 1.0)) / 2.0;
    };
}
