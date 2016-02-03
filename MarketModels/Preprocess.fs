namespace MarketModels

module Preprocess =
    open System
    open System.Collections.Generic
    open MathNet.Numerics

    //clearning and preprocessing data here...
    
    type StrictlyMonotonousDirection = Increasing | Decreasing

    let IsStrictlyMonotonous (data : float[,]) (direction : StrictlyMonotonousDirection) : bool =
        false

    let InterploateStrictly (data : float[,]) (direction : StrictlyMonotonousDirection) : float[,] =
        data

    let InterpolateMissingLinear (series : float[]) : float[] =
        series

    let InterpolateMissingLag (series : float[]) (lag : int) : float[] =
        series

    let InterpolateOutliers (series : float[]) : float[] =
        series

    let CapSeries(series : float[], ub : float, lb: float) =
        series |> Array.iteri (fun i x -> 
            if x > ub then series.[i] <- ub 
            if x < lb then series.[i] <- lb 
        )

    let CapMultipleSeries(series : float[,], ub : float, lb: float) =
        series |> Array2D.iteri (fun i j x -> 
            if x > ub then series.[i, j] <- ub 
            if x < lb then series.[i, j] <- lb 
        )