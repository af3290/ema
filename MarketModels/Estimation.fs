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

    (* 
        Methods coming from matlab... 
        we disregard censData and some other values... just use for MLE of a PDF on given data case...
    *)

    ///PDF function, returns the negative log likelihood... no CDF...
    let llf_pdfcdf (paramsValues : float[]) (uncensData : float[]) (pdfFun : float[] -> float[]) : float =
        
        let pdfVals = pdfFun paramsValues

        let abnormalVals = pdfVals |> Array.filter (fun x -> Double.IsInfinity(x) || Double.IsNaN(x))

        if abnormalVals.Length > 0 then 
            failwith "Improper PDF output"

        let loggedVals = pdfVals |> Array.map (fun x -> log x)
        
        let negativeLogLikelihood = - sum loggedVals

        negativeLogLikelihood


    ///Returns the value and gradient, calls above...
    ///(float, float[])
    ///PDF takes params array and returns PDF values calculated on data local the he caller...
    let llf_diff (paramsValues : float[]) (uncensData : float[]) (pdfFun : float[] -> float[]) (delta : float) : (float * float[]) = 
        
        let nll = llf_pdfcdf paramsValues uncensData pdfFun
        
        //gradient calculation function
        let gradientCalc = fun gPlusParamsValues gMinusParamsValues ->
            let plusVal = llf_pdfcdf gPlusParamsValues uncensData pdfFun
            let minusVal = llf_pdfcdf gMinusParamsValues uncensData pdfFun
            plusVal - minusVal

        let maxOne = fun x -> 
            let absx = abs(x)
            max absx 1.0

        let deltaParams = paramsValues |> Array.map(fun x-> delta * maxOne x)

        let e = Array.zeroCreate<float> (paramsValues.Length)
        let ngrad = Array.zeroCreate<float> (paramsValues.Length)
            
        //TODO: rewrite...
        //actual central difference calculation
        for j in 0 .. (paramsValues.Length - 1) do
            e.[j] <- deltaParams.[j]
            ngrad.[j] <- gradientCalc (paramsValues .+ e) (paramsValues .- e)
            e.[j] <- 0.0

        //central difference scaling to divided deltas
        ngrad |> Array.iteri (fun i x -> ngrad.[i] <- x / (2.0 * deltaParams.[i]))

        (nll, ngrad)

    ///Maximum Likelihood estimation of a generic PDF function based on data sample... akin to matlab's mlecustom.m function
    let MLE (data: float[]) (x0: float[]) (lb: float[]) (ub: float[]) (pdf : float[] -> float[]) : float[] =
        
        //for llf_diff
        let delta = 6.0555e-06

        //linear bounds
        let bounds = array2D [|lb; ub|]

        //gradient wrapper function
        let optimFunc : alglib.ndimensional_grad = new alglib.ndimensional_grad(fun parameters funcRes grad obj -> 
            let valAndGrad = llf_diff parameters data pdf delta 
            funcRes <- fst valAndGrad
            let gradientValue = snd valAndGrad
            gradientValue |> Array.iteri(fun i x -> grad.[i] <- gradientValue.[i])
        )

        let result = ConstrainedMultivariateWithGradient x0 bounds optimFunc

        result

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

