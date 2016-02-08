namespace MarketModels

module Simulations =
    open System
    open Types
    open MathFunctions
    open StochasticProcesses
    open Operations
    open Optimization
    open Forecast
    open MathNet
    open MathNet.Numerics.Statistics
    open MathNet.Numerics
    open MathNet.Numerics.Interpolation
    open MathNet.Numerics.Distributions
    open MathNet.Numerics.LinearAlgebra
    open MathNet.Numerics.LinearAlgebra.Double
    open MathNet.Numerics.LinearAlgebra.Matrix
    open MathNet.Numerics.LinearAlgebra.MatrixExtensions
    open MathNet.Numerics.LinearAlgebra.DenseMatrix
    //TODO: add the rest of the models! YES!

    let MinimumSimulationsCountForNormalConfidenceOf (mu : float) (sigma : float) (alpha : float) : int =
        let z = - Normal.InvCDF(0.0, 1.0, (1.0 - alpha) / 2.0)
        let value = ((2.0 * z * sigma) / mu ) ** 2.0
        (int)(Math.Floor value)

    let dailyLongTermMean (daysInEachMonthArray : int[]) (priceCurve : float[]) = 
        let numOfMonths = priceCurve.Length
        let totalNumOfDays = daysInEachMonthArray |> Array.sum

        let cumulativeDaysArray = daysInEachMonthArray |> Array.scan (fun state value -> state + value) 0 //|> Array.toSeq |> Seq.skip 1 |> Array.ofSeq
        
        let daysWithPriceCurveVals = daysInEachMonthArray|> Array.mapi (fun i daysInMonth -> (float)(daysInMonth/2+cumulativeDaysArray.[i]))

        let daysArray = Array.init totalNumOfDays (fun i -> i + 1)
        
        let spline = Interpolate.CubicSpline(daysWithPriceCurveVals, priceCurve)
       
        daysArray |> Array.map(fun day -> spline.Interpolate((float) day))

    //TODO: change to daysInEachMonthArray to timeSteps (e.g. days in months or hours in months... etc...)
    let spotPriceSimulations (daysInEachMonthArray : int[]) (numOfSimulations : int) (priceCurve : float[]) (deltaT : float) (reversionRate : float) (sigma : float) = 
        
        // The number of days to be simulated
        let numOfDays = daysInEachMonthArray |> Array.sum

        let dailyMeans = dailyLongTermMean daysInEachMonthArray priceCurve
        //let volFromSeasonality = (dailyMeans |> calculateLogReturns |> stdDev)/sqrt(deltaT)

        // Generating the standard normal samples
        let simulations = StochasticProcesses.getStandardNormalSampleMatrix numOfSimulations numOfDays 

        //Generating the paths
        StochasticProcesses.generateIIDMeanRevertingGBMPaths dailyMeans.[0] simulations deltaT reversionRate dailyMeans sigma

    let spotPriceSimulationsConfidence (daysInEachMonthArray : int[]) (priceCurve : float[]) (deltaT : float) (reversionRate : float) (sigma : float) (alpha : float) = 
        
        // The number of days to be simulated
        let numOfDays = daysInEachMonthArray |> Array.sum

        let dailyMeans = dailyLongTermMean daysInEachMonthArray priceCurve

        let dist = new Normal()
        
        // Generating the standard normal samples
        let upper =  Array.create numOfDays (dist.InverseCumulativeDistribution alpha) 
        let lower = upper |> Array.map (fun x -> - x) 
        let simulations =  [|upper;lower|]

        //Generating the paths
        StochasticProcesses.generateIIDMeanRevertingGBMPaths dailyMeans.[0] simulations deltaT reversionRate dailyMeans sigma

    /// Gets the correlation matrix from a covariance matrix
    let divideByDiagonal (arr2D : float[,]) =
        let numOfRows = arr2D |> Array2D.length1
        let numOfCols = arr2D |> Array2D.length2

        if numOfRows = numOfCols then
            arr2D |> Array2D.mapi(fun i j value -> value/sqrt(arr2D.[i,i]*arr2D.[j,j]))
        else
            arr2D
   
    let curveSimulations (priceCurve : float[]) (covarianceMatrix : float[,]) (numOfCurvesToSimulate : int) (deltaT : float) : float[][] =  
       
        // The correlationMatrix, if it's covariance, it's divided so it's alright
        let corrMatrix = covarianceMatrix |> divideByDiagonal //should already provide correlationMatrix... 
        
        // Simulating correlated normals
        let correlatedSimulations = multiNormalZeroMeanSimulations covarianceMatrix numOfCurvesToSimulate 

        let longTermMeans = priceCurve |> Array.map(fun value -> [|value|])
        let numOfMonths = priceCurve.Length
        let sigmas = Array.init numOfMonths (fun i -> sqrt(covarianceMatrix.[i,i]))
        let kappas = Array.init numOfMonths (fun i -> 0.) 

        let corrSims = correlatedSimulations.Transpose().ToRowArrays()

        let corrSimsMapGBM = fun mValues -> generateMultipleMeanRevertingGBMPaths priceCurve (mValues |> Array.map (fun value -> [|value|])) deltaT kappas longTermMeans sigmas
        let corrSimsMap = fun mValues -> corrSimsMapGBM mValues |> Array.map (fun value -> value.[1])
        
        //TODO: rewrite all this shit...!
        let sims = corrSims |> Array.map corrSimsMap

        let toTransposeSims = DenseMatrix.init sims.Length sims.[0].Length (fun i j ->  sims.[i].[j])
        
        toTransposeSims.Transpose().ToRowArrays()


    //TODO: do this algorithm from matlab... for now assume a much simpler approach...
    ///Reorders data in structure
//    let concatenateHistoricalForwardPrices (data : float[][]) (length : int) : float[][] =
//        data

    ///calculates the covariance from historical data, then calls below
    ///data is in format of [historicalContract, historicalPrice]
    let curveSimulationsHistorical (priceCurve : float[]) (data : float[][]) (numOfCurvesToSimulate : int) (deltaT : float) : float[][] =
        //prepare prices
        let logdata = data |> Array.map (fun row -> row |> Array.map log)
        
        let difflogdata = logdata |> Array.map(fun row -> row |> DiffSeries)
        
        //TODO: replace with PCA...          
        let corrs = Array2D.init (data.GetLength 0) (data.GetLength 0) (fun i j -> 
            let minLen = min difflogdata.[i].Length difflogdata.[j].Length
            if j >= i then 
                Correlation.Pearson(difflogdata.[i] |> Seq.take minLen, difflogdata.[j] |> Seq.take minLen) 
            else 
                0.0
        )

        curveSimulations priceCurve corrs numOfCurvesToSimulate deltaT





