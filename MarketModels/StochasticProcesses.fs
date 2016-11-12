namespace MarketModels

module StochasticProcesses = 
    open System

    open MathNet.Numerics.Distributions
    open MathNet.Numerics.LinearAlgebra
    open MathNet.Numerics.LinearAlgebra.Double
    open MathNet.Numerics.LinearAlgebra.Matrix
    open MathNet.Numerics.LinearAlgebra.MatrixExtensions
    open MathNet.Numerics.LinearAlgebra.DenseMatrix
    open MathNet.Numerics.LinearAlgebra.Factorization
    
    open MathFunctions
    open Types

    //TODO: all should use MathNet.Numerics.Matrix and related types... yes...

    let getStandardNormalSampleMatrixSimple (rows : int) (cols : int) = 
        let dist = new Normal()        
        dist.RandomSource.Next() |> ignore
        let matarr = Array.zeroCreate<float> (rows*cols)
        dist.Samples(matarr)
        let resultMatrix = Array.init rows (fun i -> Array.init cols (fun j -> matarr.[i*(cols+1)+j]))
        resultMatrix

    let getStandardNormalSampleMatrix (rows : int) (cols : int) = 
        let dist = new Normal()        
        dist.RandomSource.Next() |> ignore
        let samples = dist.Samples()
        let resultMatrix = Array.init rows (fun i -> Array.init cols (fun j -> samples |> Seq.nth (i*(cols+1)+j)))
        resultMatrix
    
    /// Takes a positive definite matrix, makes it lower triangular
    /// and returns the corresponding lower triangular factor.
    let choleskyFactorization (positiveDefiniteMatrix: float [,]) =
        let len = Array2D.length1 positiveDefiniteMatrix
        let array = Array.init (len*len) (fun i -> positiveDefiniteMatrix.[i/len, i%len])
        let matrix = DenseMatrix.init len len (fun i j ->  if j >= i then positiveDefiniteMatrix.[i, j] else 0.0)
        let cholFac = matrix.Cholesky()
        cholFac.Factor
       
    // Function that simulates and correlates the simulations by use of the cholesky functions
    let multiNormalZeroMeanSimulations (covarianceMatrix : float[,]) (numOfSimulations) =
        
        // Creating a jagged array of independent standard normal simulations. The array has the same length as the mean vector. Each entrance of the array constains an array of length numOfSimulations. 
        let simulations = getStandardNormalSampleMatrix (covarianceMatrix.GetLength 1) numOfSimulations  

        let simulationMatrix = DenseMatrix.init (covarianceMatrix.GetLength 1) numOfSimulations (fun i j -> simulations.[i].[j])
        
        // Creating the lower triangular cholesky decomposition of the covariance matrix
        let choleskyMatrix = covarianceMatrix |> choleskyFactorization 

        // Multiplying the matrices choleskyMatrix and simulationsMatrix to get a Matrix of correlated standard normals. 
        let correlatedSimulations = choleskyMatrix.Multiply simulationMatrix

        correlatedSimulations

    // Function that simulates and correlates the simulations by use of the cholesky functions
    let multiNormalZeroMeanConfidences (covarianceMatrix : float[,]) (alpha : float) =
        //TODO: refactor...
        let dist = new Normal()

        // Generating the standard normal samples
        let upper =  Array.create (covarianceMatrix.GetLength 1) (dist.InverseCumulativeDistribution alpha) 
        let lower = upper |> Array.map (fun x -> - x) 
        let simulations =  JaggedArray.transpose [|upper;lower|]
        
        let simulationMatrix = DenseMatrix.init (covarianceMatrix.GetLength 1) 2 (fun i j -> simulations.[i].[j])
        
        // Creating the lower triangular cholesky decomposition of the covariance matrix
        let choleskyMatrix = covarianceMatrix |> choleskyFactorization 

        // Multiplying the matrices choleskyMatrix and simulationsMatrix to get a Matrix of correlated standard normals. 
        let correlatedSimulations = choleskyMatrix.Multiply simulationMatrix

        correlatedSimulations

//    type Poisson() = class
//        member val Lambda = 0.0 with get, set
//    end
        
    ///An OU Step 
    type OU = {
        lambda: float; 
        mu: float; 
        sigma :float
    }

    let singleOUStep currentValue wienerTerm (deltaT : float) kappa mu sigma =
        let sigmaAdjustment = if kappa = 0. then sigma*sqrt(deltaT) else sigma*sqrt((1.-exp(-2.*kappa*deltaT))/(2.*kappa))
        let meanValue = currentValue*exp(-kappa*deltaT) + mu*(1.-exp(-kappa*deltaT))
        let varianceValue = sigmaAdjustment*wienerTerm
        let res = meanValue + varianceValue
        res

    let singleMeanRevertingGBMStep currentValue wienerTerm deltaT kappa mu sigma =
        let sigmaAdjustment = if kappa = 0. then sigma*sigma*deltaT/2. else sigma*sigma/4./kappa*(1.-exp(-2.*kappa*deltaT))
        exp(-sigmaAdjustment)*exp(singleOUStep (log (currentValue) + sigmaAdjustment) wienerTerm deltaT kappa ((log mu) + sigmaAdjustment) sigma)   
        
    let generateMeanRevertingPath (currentPrice : float) (wienerProcess : float[]) (deltaT : float) (kappa : float) (mu : float) (sigma : float) = 
        wienerProcess |> Array.scan(fun prevVal w -> singleMeanRevertingGBMStep prevVal w deltaT kappa mu sigma) currentPrice 

    let generateMeanRevertingPathWithSigmas (currentPrice : float) (wienerProcess : float[]) (deltaT : float) (kappa : float) (mu : float) (sigmas : float[]) = 
        (wienerProcess, sigmas) ||> Array.zip |> Array.scan(fun prevVal (w, sigma) -> singleMeanRevertingGBMStep prevVal w deltaT kappa mu sigma) currentPrice 

    let generateMeanRevertingGMBPath (currentPrice : float)(wienerProcess : float[]) (deltaT : float) (kappa : float) (longTermMeans : float[]) (sigma : float) = 
        (wienerProcess, longTermMeans) ||> Array.zip |> Array.scan(fun prevVal (w, mu) -> singleMeanRevertingGBMStep prevVal w deltaT kappa mu sigma) currentPrice 

    let generateIIDMeanRevertingGBMPaths (startValue : float) (wienerProcesses : float[][]) (deltaT : float) (kappa : float) (longTermMeans : float[]) (sigma : float) =
        wienerProcesses |> Array.map(fun wArr -> generateMeanRevertingGMBPath startValue wArr deltaT kappa longTermMeans sigma)

    let generateMultipleMeanRevertingGBMPaths (currentPrices : float[]) (wienerProcesses : float[][]) (deltaT : float) (kappas : float[]) (longTermMeans : float[][]) (sigmas : float[]) =
        currentPrices |> Array.mapi(fun i currentPrice -> generateMeanRevertingGMBPath currentPrice wienerProcesses.[i] deltaT kappas.[i] longTermMeans.[i] sigmas.[i]) 
     
