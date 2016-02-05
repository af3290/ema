namespace MarketModels

module Preprocess =
    open System
    open System.Collections.Generic
    open Types
    open Forecast
    open TimeSeries
    open MathFunctions
    open Operations
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

    let Desezonalize (series : float[]) (lag : int) : float[] =
        //Simple moving average case
        let b = Array.init lag (fun i -> 1.0 / (float)lag)        
        
        let movavg = MovAvg series lag

        (series -- movavg) .+. mean series

    ///Returns the indices where spikes are determined based on moving standard deviation at given lags and confidence level
    let EstimateSpikesOnMovSDs (series : float[]) (longLag : int) (shortLag : int) (alpha : float) : int[] = 
        
        let shortSDs = MovingStandardDeviation series MovingAverageType.Simple shortLag 1

        let longSDs = MovingStandardDeviation series MovingAverageType.Simple longLag 1

        let isSpikeIndices = Array.init series.Length (fun i -> if shortSDs.[i] > alpha * longSDs.[i] then i - shortLag/2 else -1) 
                
        isSpikeIndices |> Array.filter (fun x -> x > 0)

    ///IN TODO...
    let ReplaceSpikes (series : float[]) (spikeIndices : int[]) (sp : SpikePreprocess) (alpha : float) : float[] = 
        //find unique ocurring spike
        let singularSpikes = Array.init (spikeIndices.Length - 2) (fun i -> if spikeIndices.[i] < spikeIndices.[i+1] && spikeIndices.[i+1] < spikeIndices.[i+2] then i else -1)
        
        //also need to find spike cluters and the appropriate period's replacement
        let spikeCluters = Array.init (spikeIndices.Length - 2) (fun i -> if spikeIndices.[i] < spikeIndices.[i+1] && spikeIndices.[i+1] < spikeIndices.[i+2] then i else -1)

        //etc...

        series