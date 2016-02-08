namespace MarketModels

///Contains most common estimation techniques: OLS and MLE
module Estimation = 
    open System
    open Types
    open MathFunctions
    open StochasticProcesses
    open Operations
    open Optimization
    open Forecast

    let OLS (x: float) : float =
        x

    ///Maximum Likelihood estimation of a generic function...
    let MLE (x: float) : float =
        x

    ///Maximum Likelihood estimation of an OU process.. returns the parameters
    let OU_MLE (S : float[]) (delta : float): OU =
        let n = (float)(S.Length - 1)
 
        let x = before S (S.Length - 1)
        let y = after S 1

        let Sx  = sum x
        let Sy  = sum y
        let Sxx = sum (x ^^ 2.0)
        let Sxy = sum (x.* y)
        let Syy = sum (y ^^2.0)
 
        let a  = ( n*Sxy - Sx*Sy ) / ( n*Sxx - Sx ** 2.0 );
        let b  = ( Sy - a*Sx ) / n;
        let sd = sqrt( (n*Syy - Sy ** 2.0 - a*(n*Sxy - Sx*Sy) )/n/(n-2.0) );
 
        {
            lambda = -log(a)/delta;
            mu     = b/(1.0-a);
            sigma  =  sd * sqrt( -2.0*log(a)/delta/(1.0 - a ** 2.0) );
        }

