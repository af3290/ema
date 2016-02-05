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

    let IsStrictlyIncreasing (series : int[]) : bool =
        //If all numbers are bigger their previouses, without equals
        series |> Seq.skip 1 |> Seq.mapi(fun i x -> if x > series.[i] then 1 else 0) |> Seq.sum = series.Length - 1

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
        if spikeIndices.Length < 2 then
            Array.empty
        else
        if not (IsStrictlyIncreasing spikeIndices) then
            failwith "Spike indices not in order"

        //shortHands
        let len = (spikeIndices.Length - 2)
        let sIs = spikeIndices

        //find indices with no immediate neighbours
        let singularSpikes = Array.init len (fun i -> 
            //first case
            if i = 0 then 
                if sIs.[i] < sIs.[i+1] - 1 then sIs.[i] else -1 
            else
            //last case
            if i = len - 1 then 
                if sIs.[i - 1] < sIs.[i] - 1 then sIs.[i] else - 1
            else
            //middle elements
            if sIs.[i - 1] < sIs.[i] - 1 && sIs.[i] + 1 < sIs.[i + 1] then 
                sIs.[i]
            else 
                -1)
        let singularSpikesIndices = singularSpikes |> Array.filter (fun i -> i > 0)
        
        //find spike clusters, starting indices for each
        let mutable spikeCluters = List.empty<int>
        let mutable currentHour = spikeIndices.[0]
        let mutable currentIndex = 0

        //find every contiguous cluter
        while currentIndex < spikeIndices.Length - 1 do
            let mutable i = currentIndex + 1
            let mutable nextHour = spikeIndices.[i]            
            
            //find first that breaks continuity
            while currentHour + (i-currentIndex) = nextHour && i < spikeIndices.Length - 1 do
                i <- i + 1
                nextHour <- spikeIndices.[i]

            //fall back to previous to be within contiguous
            nextHour <- spikeIndices.[i - 1]
            
            //append result to list
            if nextHour - currentHour > 0 then spikeCluters <- currentHour :: spikeCluters
            
            //case i = spikeIndices.Length - 1 not handled!!!... in TODO...

            //advance to next contiguous hour cluster
            currentHour <- spikeIndices.[i]
            currentIndex <- i

        series

    let ReplaceSingularSpikes (series : float[]) (spikeIndices : int[]) (sp : SpikePreprocess) (alpha : float) : float[] =        
        if spikeIndices.Length < 2 then
            Array.empty
        else
        if not (IsStrictlyIncreasing spikeIndices) then
            failwith "Spike indices not in order"

        //shortHands
        let len = (spikeIndices.Length - 2)
        let sIs = spikeIndices

        //find indices with no neighbours within same day
        let singularSpikesInDay = Array.init len (fun i -> 
            let todaysStart = 24 * (sIs.[i] / 24)
            let todaysEnd = 24 * (sIs.[i] / 24) + 23
            //first case
            if i = 0 then 
                if todaysEnd < sIs.[i+1] then sIs.[i] else -1 
            else
            //last case
            if i = len - 1 then 
                if sIs.[i - 1] < todaysStart then sIs.[i] else - 1
            else
            //middle elements
            if sIs.[i - 1] < todaysStart && todaysEnd < sIs.[i + 1] then 
                sIs.[i]
            else 
                -1)

        //eliminate previous week too... since that's needed for similar day method
        let singularSpikesInDayIndices = singularSpikesInDay |> Array.filter (fun i -> i > 168)

        match sp with
        | SpikePreprocess.SimilarDay -> 
            series |> Array.mapi (fun i x -> 
                if Array.BinarySearch(singularSpikesInDayIndices, i) > 0
                //last week's similar day
                then series.[i-168]
                else series.[i]
            )
        | SpikePreprocess.None -> series
        | SpikePreprocess.Limited -> series   