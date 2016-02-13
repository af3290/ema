namespace MarketModels

module Simulations =
    open System
   
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

    open Types
    open MathFunctions
    open StochasticProcesses
    open Operations
    open Optimization
    open Forecast

    //TODO: add the rest of the models! YES!

    let MinimumSimulationsCountForNormalConfidenceOf (mu : float) (sigma : float) (alpha : float) : int =
        let z = - Normal.InvCDF(0.0, 1.0, (1.0 - alpha) / 2.0)
        let value = ((2.0 * z * sigma) / mu ) ** 2.0
        (int)(Math.Floor value)
        
    //TODO: fix it, it's wrong...
    let dailyLongTermMean (daysInEachMonthArray : int[]) (priceCurve : float[]) = 
        let numOfMonths = priceCurve.Length
        let totalNumOfDays = daysInEachMonthArray |> Array.sum

        let cumulativeDaysArray = daysInEachMonthArray |> Array.scan (fun state value -> state + value) 0 //|> Array.toSeq |> Seq.skip 1 |> Array.ofSeq
        
        let daysWithPriceCurveVals = daysInEachMonthArray|> Array.mapi (fun i daysInMonth -> (float)(daysInMonth/2+cumulativeDaysArray.[i]))

        let daysArray = Array.init totalNumOfDays (fun i -> i + 1)
        
        let spline = Interpolate.CubicSpline(daysWithPriceCurveVals, priceCurve)
       
        daysArray |> Array.map(fun day -> spline.Interpolate((float) day))

        ///Matches given data such that for each interval avg(data)==priceCurve
    let liftSeriesToPriceCurve (priceSeries : float[,]) (periods : int[]) (priceCurve : float[]) : float[,] =
        let nSeries = priceSeries.GetLength 0
        let horzion = priceSeries.GetLength 1

        if horzion <> Array.sum periods then
            failwith "Can't do"
                    
        let means = dailyLongTermMean periods priceCurve
        let meansMean = (mean means)
        let centeredMeans = means

        let lifted = Array.init nSeries (fun i -> 
            let series = priceSeries.[i, *]  
            let seriesMean = (mean series)
            let centeredSeries = series .-. seriesMean

            centeredSeries .+ means
        )

        array2D lifted

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

    //COPY-PASTED...
    //TODO: rewrite...
    let curveSimulationsConfidence (priceCurve : float[]) (covarianceMatrix : float[,]) (alpha : float) (deltaT : float) : float[][] =  
       
        // The correlationMatrix, if it's covariance, it's divided so it's alright
        let corrMatrix = covarianceMatrix |> divideByDiagonal //should already provide correlationMatrix... 
        
        // Simulating correlated normals
        let correlatedSimulations = multiNormalZeroMeanConfidences covarianceMatrix alpha 

        let longTermMeans = priceCurve |> Array.map(fun value -> [|value|])
        let numOfMonths = priceCurve.Length
        let sigmas = Array.init numOfMonths (fun i -> sqrt(covarianceMatrix.[i,i]))
        let kappas = Array.init numOfMonths (fun i -> 0.) 

        let corrSims = correlatedSimulations.Transpose().ToRowArrays()

        let corrSimsMapGBM = fun mValues -> generateMultipleMeanRevertingGBMPaths priceCurve (mValues |> Array.map (fun value -> [|value|])) deltaT kappas longTermMeans sigmas
        let corrSimsMap = fun mValues -> corrSimsMapGBM mValues |> Array.map (fun value -> value.[1])
        
        //TODO: rewrite all this shit...!
        //TODO: figure it out properly... yeah...
        let sims = corrSims |> Array.map corrSimsMap

        let toTransposeSims = DenseMatrix.init sims.Length sims.[0].Length (fun i j ->  sims.[i].[j])
        
        toTransposeSims.Transpose().ToRowArrays()

    ///Calculates the confidence values for a price curves based on its historical values...
    let curveSimulationsHistoricalConfidence (priceCurve : float[]) (data : float[][]) (alpha : float) (timeHorizonDeltaT : float) : float[][] =
        //TODO: finish here...
        //prepare prices
        let logdata = data |> Array.map (fun row -> row |> Array.map log)
        
        //let difflogdata = logdata |> Array.map(fun row -> row |> DiffSeries)
        let difflogdata = data

        //TODO: replace with PCA...          
        let corrs = Array2D.init (data.GetLength 0) (data.GetLength 0) (fun i j -> 
            let minLen = min difflogdata.[i].Length difflogdata.[j].Length
            if j >= i then 
                Correlation.Pearson(difflogdata.[i] |> Seq.take minLen, difflogdata.[j] |> Seq.take minLen) 
            else 
                0.0
        )

        //TODO: revise..
        JaggedArray.transpose (curveSimulationsConfidence priceCurve corrs alpha timeHorizonDeltaT)






