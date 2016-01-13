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